using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServeCore
{
    public interface IHandlerCenter
    {
        /// <summary>
        /// 客户端连接
        /// </summary>
        /// <param name="token">连接的客户端对象</param>
        void ClientConnect(Token token);
        /// <summary>
        /// 收到客户端消息
        /// </summary>
        /// <param name="token">发送消息的客户端对象</param>
        /// <param name="message">消息内容</param>
        void MessageReceive(Token token, object message);
        /// <summary>
        /// 客户端断开连接
        /// </summary>
        /// <param name="token">断开的客户端对象</param>
        /// <param name="error">断开的错误信息</param>
        void ClientClose(Token token, string error);
    }
}
