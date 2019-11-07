using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServeCore
{
    public class SocketModel
    {
        /// <summary>
        /// 一级协议 用于区分所属模块
        /// </summary>
        public int type { get; set; }
        /// <summary>
        /// 二级协议 用于区分 模块下所属子模块
        /// </summary>
        public int area { get; set; }
        /// <summary>
        /// 三级协议  用于区分当前处理逻辑功能
        /// </summary>
        public int command { get; set; }
        /// <summary>
        /// 消息体 当前需要处理的主体数据
        /// </summary>
        public byte[] message { get; set; }

        public SocketModel() { }
        public SocketModel(byte t, int a, int c, byte[] o)
        {
            this.type = t;
            this.area = a;
            this.command = c;
            this.message = o;
        }

        public T GetMessage<T>()
        {
            object obj = SerializeUtil.decode(this.message);
            return (T)obj;
        }
    }
}
