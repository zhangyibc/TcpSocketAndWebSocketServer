using ServeCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketNetFramework
{
    public class WebSocketServer
    {
        public TcpListener server;
        public int maxClient;
        public UserTokenPool pool;

        // 信号量
        Semaphore acceptClients;

        // 消息处理
        public LengthEncode LE = LengthEncoding.encode;
        public LengthDecode LD = LengthEncoding.decode;

        public MessageEncode encode = MessageEncoding.encode;
        public MessageDecode decode = MessageEncoding.decode;

        /// <summary>
        /// 消息处理中心，由外部应用传入
        /// </summary>
        public IHandlerCenter center;

        /// <summary>
        /// 日志处理中心
        /// </summary>
        public ILogger logger;

        public WebSocketServer(int max)
        {
            maxClient = max;
        }

        public void Start(int port)
        {
            // 初始化对象池
            pool = new UserTokenPool(maxClient);
            //连接信号量
            acceptClients = new Semaphore(maxClient, maxClient);
            for (int i = 0; i < maxClient; i++)
            {
                UserToken token = new UserToken();
                token.LD = LD;
                token.LE = LE;
                token.encode = encode;
                token.decode = decode;
                token.closeProcess = ClientClose;
                token.center = center;
                token.logger = logger;
                token.tokenID = i;

                pool.push(token);
            }
            //监听当前服务器网卡所有可用IP地址的port端口
            // 外网IP  内网IP192.168.x.x 本机IP一个127.0.0.1
            try
            {
                server = new TcpListener(IPAddress.Any, port);
                server.Start();
                StartAccept();
            }
            catch (Exception e)
            {
                logger.Log(e.Message);
            }
        }

        public async void StartAccept()
        {
            //信号量-1
            acceptClients.WaitOne();
            TcpClient client = await server.AcceptTcpClientAsync();
            UserToken token = pool.pop();
            token.client = client;
            ProcessAccept(token);
            // 再次监听新的连接对象
            StartAccept();
        }

        public void ProcessAccept(UserToken token)
        {
            NetworkStream clientStream = token.client.GetStream();
            if (clientStream.DataAvailable)
            {
                if (token.client.Available < 3)
                {
                }
                else
                {
                    byte[] message = new byte[token.client.Available];
                    clientStream.Read(message, 0, message.Length);
                    string data = Encoding.UTF8.GetString(message);
                    if (new Regex("^GET").IsMatch(data))
                    {
                        const string eol = "\r\n"; // HTTP/1.1 defines the sequence CR LF as the end-of-line marker

                        byte[] response = Encoding.UTF8.GetBytes("HTTP/1.1 101 Switching Protocols" + eol
                            + "Connection: Upgrade" + eol
                            + "Upgrade: websocket" + eol
                            + "Sec-WebSocket-Accept: " + Convert.ToBase64String(
                                System.Security.Cryptography.SHA1.Create().ComputeHash(
                                    Encoding.UTF8.GetBytes(
                                        new Regex("Sec-WebSocket-Key: (.*)").Match(data).Groups[1].Value.Trim() + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"
                                    )
                                )
                            ) + eol
                            + eol);

                        clientStream.Write(response, 0, response.Length);
                        // TODO 通知应用层 有客户端连接
                        center.ClientConnect(token);
                        // 开启消息到达监听
                        token.startReceive();
                    }
                }
            }
        }

        public void ClientClose(UserToken token, string error)
        {
            if (token.client != null)
            {
                lock (token)
                {
                    //通知应用层面 客户端断开连接了
                    center.ClientClose(token, error);
                    token.Close();
                    //加回一个信号量，供其它用户使用
                    pool.push(token);
                    acceptClients.Release();
                }
            }
        }
    }
}
