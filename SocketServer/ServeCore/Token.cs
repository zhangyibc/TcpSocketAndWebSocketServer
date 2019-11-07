using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServeCore
{
    public abstract class Token
    {
        public SocketTypes socketType;

        /// <summary>
        /// 发送网络消息
        /// </summary>
        /// <param name="value"></param>
        public abstract void write(SocketModel mode);

        /// <summary>
        /// 网络消息到达
        /// </summary>
        /// <param name="buff"></param>
        public abstract void receive(byte[] buff);
    }
}
