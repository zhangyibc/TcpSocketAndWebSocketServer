using ServeCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TcpSocketNetFramework;

namespace SocketServer
{
    public class HandlerCenter : IHandlerCenter
    {
        /// <summary>
        /// 日志处理中心
        /// </summary>
        public ILogger logger;

        public HandlerCenter(ILogger logger)
        {
            this.logger = logger;
        }

        public void ClientClose(Token token, string error)
        {
            logger.Log("有客户端断开连接了");
        }

        public void ClientConnect(Token token)
        {
            logger.Log("有客户端连接了,连接方式:" + token.socketType.ToString());
        }

        public void MessageReceive(Token token, object message)
        {
            logger.Log("有消息发过来了,连接方式:" + token.socketType.ToString());
            SocketModel model = message as SocketModel;
            logger.Log(model);
            
            SocketModel msg = new SocketModel(3, 3, 3, null);
            token.write(msg);
        }
    }
}
