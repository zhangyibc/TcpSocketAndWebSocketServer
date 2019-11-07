using NetFramework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpServer
{
    public class MessageCenter : IMessage
    {
        /// <summary>
        /// 服务器目录
        /// </summary>
        public string ServerRoot { get; private set; }

        public MessageCenter(string root)
        {
            this.ServerRoot = root;
        }

        public void MessageReceive(UserToken token)
        {
            switch (token.GetMethod())
            {
                case "GET":
                    OnGet(token);
                    break;
                case "POST":
                    OnPost(token);
                    break;
                default:
                    OnDefault(token);
                    break;
            }
        }

        public void OnGet(UserToken token)
        {
            ///链接形式1:"http://localhost:4050/assets/styles/style.css"表示访问指定文件资源，
            ///此时读取服务器目录下的/assets/styles/style.css文件。

            ///链接形式1:"http://localhost:4050/assets/styles/"表示访问指定页面资源，
            ///此时读取服务器目录下的/assets/styles/index.html文件。

            //当文件不存在时应返回404状态码
            string requestURL = token.request.URL;
            requestURL = requestURL.Replace("/", @"\").Replace("\\..", "").TrimStart('\\');
            string requestFile = Path.Combine(ServerRoot, requestURL);

            //判断地址中是否存在扩展名
            string extension = Path.GetExtension(requestFile);

            //根据有无扩展名按照两种不同链接进行处
            if (extension != "")
            {
                //从文件中返回HTTP响应
                token.response = token.response.FromFile(requestFile);
            }
            else
            {
                //目录存在且不存在index页面时时列举目录
                if (Directory.Exists(requestFile) && !File.Exists(requestFile + "\\index.html"))
                {
                    requestFile = Path.Combine(ServerRoot, requestFile);
                    string content = ListDirectory(requestFile, requestURL);
                    token.response = token.response.SetContent(content, Encoding.UTF8);
                    token.response.Content_Type = "text/html; charset=UTF-8";
                }
                else
                {
                    //加载静态HTML页面
                    requestFile = Path.Combine(requestFile, "index.html");
                    token.response = token.response.FromFile(requestFile);
                    token.response.Content_Type = "text/html; charset=UTF-8";
                }
            }

            //发送HTTP响应
            token.Send();
        }

        public void OnPost(UserToken token)
        {
            //获取客户端传递的参数
            string data = token.request.Params == null ? "" : string.Join(";", token.request.Params.Select(x => x.Key + "=" + x.Value).ToArray());

            //设置返回信息
            string content = string.Format("这是通过Post方式返回的数据:{0}", data);

            //构造响应报文
            token.response.SetContent(content);
            token.response.Content_Encoding = "utf-8";
            token.response.StatusCode = "200";
            token.response.Content_Type = "text/html; charset=UTF-8";
            token.response.Headers["Server"] = "小敏姐是不是傻";

            //发送响应
            token.Send();
        }

        public void OnDefault(UserToken token)
        {

        }

        private string ConvertPath(string[] urls)
        {
            string html = string.Empty;
            int length = ServerRoot.Length;
            foreach (var url in urls)
            {
                var s = url.StartsWith("..") ? url : url.Substring(length).TrimEnd('\\');
                html += String.Format("<li><a href=\"{0}\">{0}</a></li>", s);
            }

            return html;
        }

        private string ListDirectory(string requestDirectory, string requestURL)
        {
            //列举子目录
            string[] folders = requestURL.Length > 1 ? new string[] { "../" } : new string[] { };
            folders = folders.Concat(Directory.GetDirectories(requestDirectory)).ToArray();
            var foldersList = ConvertPath(folders);

            //列举文件
            string[] files = Directory.GetFiles(requestDirectory);
            string filesList = ConvertPath(files);

            //构造HTML
            StringBuilder builder = new StringBuilder();
            builder.Append(string.Format("<html><head><title>{0}</title></head>", requestDirectory));
            builder.Append(string.Format("<body><h1>{0}</h1><br/><ul>{1}{2}</ul></body></html>",
                 requestURL, filesList, foldersList));

            return builder.ToString();
        }
    }
}
