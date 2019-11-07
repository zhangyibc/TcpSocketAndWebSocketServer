using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NetFramework
{
    public class UserToken : BaseHeader
    {
        public ILogger Logger { get; set; }

        private Stream handler;
        private TcpClient client;

        public HttpRequest request;
        public HttpResponse response;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="stream"></param>
        public UserToken(TcpClient client, Stream stream, ILogger Logger)
        {
            this.client = client;
            this.handler = stream;
            this.Logger = Logger;

            this.request = new HttpRequest(this.client, this.handler, this.Logger);
            this.response = new HttpResponse(this.client, this.handler, this.Logger);
        }

        public string GetMethod()
        {
            return this.request.Method;
        }

        public void Send()
        {
            this.response.Send();
        }
    }
}
