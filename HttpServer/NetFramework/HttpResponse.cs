using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NetFramework
{
    public class HttpResponse : BaseHeader
    {
        public string StatusCode { get; set; }

        public string Protocols { get; set; }

        public string ProtocolsVersion { get; set; }

        public byte[] Content { get; private set; }

        private Stream handler;

        public ILogger Logger { get; set; }

        public HttpResponse(TcpClient client, Stream stream, ILogger Logger)
        {
            this.handler = stream;
            this.Headers = new Dictionary<string, string>();
            this.Logger = Logger;
        }

        public HttpResponse SetContent(byte[] content, Encoding encoding = null)
        {
            this.Content = content;
            this.Encoding = encoding != null ? encoding : Encoding.UTF8;
            this.Content_Length = content.Length.ToString();
            return this;
        }

        public HttpResponse SetContent(string content, Encoding encoding = null)
        {
            //初始化内容
            encoding = encoding != null ? encoding : Encoding.UTF8;
            return SetContent(encoding.GetBytes(content), encoding);
        }

        public Stream GetResponseStream()
        {
            return this.handler;
        }

        public string GetHeader(ResponseHeaders header)
        {
            return this.GetHeader(header.GetDescription());
        }

        public string GetHeader(string fieldName)
        {
            return GetHeaderByKey(fieldName);
        }

        public void SetHeader(ResponseHeaders header, string value)
        {
            this.SetHeader(header.GetDescription(), value);
        }

        public void SetHeader(string fieldName, string value)
        {
            SetHeaderByKey(fieldName, value);
        }

        public HttpResponse FromFile(string fileName)
        {
            if (!File.Exists(fileName))
            {
                this.SetContent("<html><body><h1>404-not found</h1></body></html>");
                this.Content_Type = "text/html; charset=UTF-8";
                this.StatusCode = "404";
                return this;
            }

            byte[] content = File.ReadAllBytes(fileName);
            string contentType = GetMimeFromFile(fileName);
            this.SetContent(content);
            this.Content_Type = contentType;
            this.StatusCode = "200";
            return this;
        }

        public HttpResponse FromXML(string xmlText)
        {
            this.SetContent(xmlText);
            this.Content_Type = "text/xml";
            this.StatusCode = "200";
            return this;
        }

        public HttpResponse FromJSON(string jsonText)
        {
            this.SetContent(jsonText);
            this.Content_Type = "text/json";
            this.StatusCode = "200";
            return this;
        }

        public HttpResponse FromText(string text)
        {
            this.SetContent(text);
            this.Content_Type = "text/plain";
            this.StatusCode = "200";
            return this;
        }

        /// <summary>
        /// 构建响应头部
        /// </summary>
        /// <returns></returns>
        public string BuildHeader()
        {
            StringBuilder builder = new StringBuilder();

            if (!string.IsNullOrEmpty(StatusCode))
            {
                builder.Append("HTTP/1.1 " + StatusCode + "\r\n");
            }

            if (!string.IsNullOrEmpty(this.Content_Type))
            {
                builder.AppendLine("Content-Type:" + this.Content_Type);
            }
            return builder.ToString();
        }

        /// <summary>
        /// 发送数据
        /// </summary>
        public void Send()
        {
            if (!handler.CanWrite) return;

            try
            {
                //发送响应头
                string header = BuildHeader();
                byte[] headerBytes = this.Encoding.GetBytes(header);
                handler.Write(headerBytes, 0, headerBytes.Length);

                //发送空行
                byte[] lineBytes = this.Encoding.GetBytes(System.Environment.NewLine);
                handler.Write(lineBytes, 0, lineBytes.Length);

                //发送内容
                handler.Write(Content, 0, Content.Length);
            }
            catch (Exception e)
            {
                Log(e.Message);
            }
            finally
            {
                handler.Close();
            }
        }

        /// <summary>
        /// 记录日志
        /// </summary>
        /// <param name="message">日志消息</param>
        private void Log(object message)
        {
            if (Logger != null)
            {
                Logger.Log(message);
            }
        }

        private static string GetMimeFromFile(string filePath)
        {
            IntPtr mimeout;
            if (!File.Exists(filePath))
                throw new FileNotFoundException(string.Format("File {0} can't be found at server.", filePath));

            int MaxContent = (int)new FileInfo(filePath).Length;
            if (MaxContent > 4096) MaxContent = 4096;
            byte[] buf = new byte[MaxContent];

            using (FileStream fs = File.OpenRead(filePath))
            {
                fs.Read(buf, 0, MaxContent);
                fs.Close();
            }

            int result = FindMimeFromData(IntPtr.Zero, filePath, buf, MaxContent, null, 0, out mimeout, 0);
            if (result != 0)
                throw Marshal.GetExceptionForHR(result);

            string mime = Marshal.PtrToStringUni(mimeout);
            Marshal.FreeCoTaskMem(mimeout);

            return mime;
        }

        [DllImport("urlmon.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = false)]
        static extern int FindMimeFromData(IntPtr pBC,
              [MarshalAs(UnmanagedType.LPWStr)] string pwzUrl,
              [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.I1, SizeParamIndex = 3)]
              byte[] pBuffer,
              int cbSize,
              [MarshalAs(UnmanagedType.LPWStr)]
              string pwzMimeProposed,
              int dwMimeFlags,
              out IntPtr ppwzMimeOut,
              int dwReserved);
    }
}
