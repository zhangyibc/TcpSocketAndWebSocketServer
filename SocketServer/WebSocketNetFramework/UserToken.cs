using ServeCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace WebSocketNetFramework
{
    public class UserToken:Token
    {
        public TcpClient client;

        public int tokenID;

        // 接收数据的buffer
        public byte[] buffer;
        // buffer大小
        public int bufferSize = 4 * 1024;

        public LengthEncode LE;
        public LengthDecode LD;

        public MessageEncode encode;
        public MessageDecode decode;

        public CloseProcess closeProcess;

        // 消息数据
        List<byte> cache = new List<byte>();
        private bool isReading = false;
        private bool isWriting = false;

        // 发送消息队列
        Queue<byte[]> writeQueue = new Queue<byte[]>();

        // 消息中心
        public IHandlerCenter center;
        public ILogger logger;

        public UserToken()
        {
            this.socketType = SocketTypes.WebSocket;
        }

        public override void receive(byte[] buff)
        {
            //将消息写入缓存
            byte[] message = AnalyticData(buff, buff.Length);
            cache.AddRange(message);

            if (!isReading)
            {
                isReading = true;
                onData();
            }
        }

        void onData()
        {
            //解码消息存储对象
            byte[] buff = null;
            //当粘包解码器存在的时候 进行粘包处理
            if (LD != null)
            {
                buff = LD(ref cache);
                //消息未接收全 退出数据处理 等待下次消息到达
                if (buff == null) { isReading = false; return; }
            }
            else
            {
                //缓存区中没有数据 直接跳出数据处理 等待下次消息到达
                if (cache.Count == 0) { isReading = false; return; }
                buff = cache.ToArray();
                cache.Clear();
            }
            //反序列化方法是否存在
            if (decode == null) { throw new Exception("message decode process is null"); }
            //进行消息反序列化
            object message = decode(buff);
            //TODO 通知应用层 有消息到达
            center.MessageReceive(this, message);
            //尾递归 防止在消息处理过程中 有其他消息到达而没有经过处理
            onData();
        }

        public override void write(SocketModel model)
        {
            byte[] value = encode(model);
            value = LE(value);

            if (client == null)
            {
                //此连接已经断开了
                closeProcess(this, "客户端链接为空");
                return;
            }
            writeQueue.Enqueue(value);
            if (!isWriting)
            {
                isWriting = true;
                onWrite();
            }
        }

        public void onWrite()
        {
            //判断发送消息队列是否有消息
            if (writeQueue.Count == 0) { isWriting = false; return; }
            //取出第一条待发消息
            byte[] msg = PackData(writeQueue.Dequeue());
            //设置消息发送异步对象的发送数据缓冲区数据
            if (client.GetStream().CanWrite)
            {
                try
                {
                    client.GetStream().BeginWrite(msg, 0, msg.Length, new AsyncCallback(AsyncWriteCallBack), client.GetStream());
                }
                catch (Exception)
                {
                    closeProcess(this, "发送消息失败");
                }
            }
        }

        // 发送结束回调
        public void AsyncWriteCallBack(IAsyncResult iar)
        {
            if (iar.IsCompleted)
            {
                NetworkStream clientStream = (NetworkStream)iar.AsyncState;
                clientStream.EndWrite(iar);
                onWrite();
            }
            else
            {
                //此连接已经断开了
                closeProcess(this, "发送消息失败");
            }
        }

        // 开始消息接收
        public void startReceive()
        {
            NetworkStream clientStream = client.GetStream();
            if (clientStream.CanRead)
            {
                buffer = new byte[client.ReceiveBufferSize];
                clientStream.BeginRead(buffer, 0, 4 * 1024, new AsyncCallback(AsyncReadCallBack), clientStream);
            }
            else
            {
                closeProcess(this, "客户端断开连接");
            }
        }

        public void AsyncReadCallBack(IAsyncResult iar)
        {
            if (iar.IsCompleted)
            {
                NetworkStream clientStream = (NetworkStream)iar.AsyncState;
                try
                {
                    int byteNum = clientStream.EndRead(iar);
                    if (byteNum > 0)
                    {
                        byte[] msg = new byte[byteNum];
                        Array.Copy(buffer, 0, msg, 0, byteNum);
                        receive(msg);
                        // 递归轮询
                        startReceive();
                    }

                    else
                    {
                        closeProcess(this, "客户端主动断开连接");
                    }
                }
                catch (Exception)
                {
                    closeProcess(this, "客户端主动断开连接");
                }
            }
            else
            {
                closeProcess(this, "客户端主动断开连接");
            }
        }

        // 外界调用刷新
        public void writed()
        {
            //与onData尾递归同理
            onWrite();
        }

        public void Close()
        {
            try
            {
                writeQueue.Clear();
                cache.Clear();
                isReading = false;
                isWriting = false;
                client.Close();
                client = null;
            }
            catch (Exception e)
            {
                logger.Log(e.Message);
            }
        }

        // websocket数据解包
        public byte[] AnalyticData(byte[] recBytes, int recByteLength)
        {
            if (recByteLength < 2) { return null; }


            bool fin = (recBytes[0] & 0x80) == 0x80; // 1bit，1表示最后一帧  
            if (!fin)
            {
                return null;// 超过一帧暂不处理 
            }

            bool mask_flag = (recBytes[1] & 0x80) == 0x80; // 是否包含掩码  
            if (!mask_flag)
            {
                return null;// 不包含掩码的暂不处理
            }

            int payload_len = recBytes[1] & 0x7F; // 数据长度  

            byte[] masks = new byte[4];
            byte[] messageData;

            if (payload_len == 126)
            {
                Array.Copy(recBytes, 4, masks, 0, 4);
                payload_len = (ushort)(recBytes[2] << 8 | recBytes[3]);
                messageData = new byte[payload_len];
                Array.Copy(recBytes, 8, messageData, 0, payload_len);

            }
            else if (payload_len == 127)
            {
                Array.Copy(recBytes, 10, masks, 0, 4);
                byte[] uInt64Bytes = new byte[8];
                for (int i = 0; i < 8; i++)
                {
                    uInt64Bytes[i] = recBytes[9 - i];
                }
                ulong len = BitConverter.ToUInt64(uInt64Bytes, 0);

                messageData = new byte[len];
                for (ulong i = 0; i < len; i++)
                {
                    messageData[i] = recBytes[i + 14];
                }
            }
            else
            {
                Array.Copy(recBytes, 2, masks, 0, 4);
                messageData = new byte[payload_len];
                Array.Copy(recBytes, 6, messageData, 0, payload_len);
            }

            for (var i = 0; i < payload_len; i++)
            {
                messageData[i] = (byte)(messageData[i] ^ masks[i % 4]);
            }

            return messageData;
        }

        // websocket数据封装
        public byte[] PackData(byte[] temp)
        {
            byte[] contentBytes = null;

            if (temp.Length < 126)
            {
                contentBytes = new byte[temp.Length + 2];
                contentBytes[0] = 0x82;
                contentBytes[1] = (byte)temp.Length;
                Array.Copy(temp, 0, contentBytes, 2, temp.Length);
            }
            else if (temp.Length < 0xFFFF)
            {
                contentBytes = new byte[temp.Length + 4];
                contentBytes[0] = 0x82;
                contentBytes[1] = 126;
                contentBytes[2] = (byte)(temp.Length & 0xFF);
                contentBytes[3] = (byte)(temp.Length >> 8 & 0xFF);
                Array.Copy(temp, 0, contentBytes, 4, temp.Length);
            }
            else
            {
                contentBytes = new byte[temp.Length + 10];
                contentBytes[0] = 0x82;
                contentBytes[1] = 127;
                byte[] byteLength = BitConverter.GetBytes((ulong)temp.Length);
                Array.Copy(byteLength, 0, contentBytes, 2, temp.Length);
                Array.Copy(temp, 0, contentBytes, 10, temp.Length);
            }

            return contentBytes;
        }
    }
}
