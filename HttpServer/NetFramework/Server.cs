using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace NetFramework
{
    public class Server
    {
        /// <summary>
        /// 服务器端口
        /// </summary>
        public int ServerPort { get; private set; }

        /// <summary>
        /// 服务器目录
        /// </summary>
        public string ServerRoot { get; private set; }

        /// <summary>
        /// 是否运行
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// 服务器协议
        /// </summary>
        public Protocols Protocol { get; private set; }

        /// <summary>
        /// 服务端Socet
        /// </summary>
        private TcpListener serverListener;

        /// <summary>
        /// 日志接口
        /// </summary>
        public ILogger Logger { get; set; }

        // 请求处理
        public IMessage center;

        // SSL证书(用于Https)
        private X509Certificate serverCertificate = null;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="port"></param>
        /// <param name="root"></param>
        public Server(int port, string root)
        {
            this.ServerPort = port;

            // 要判断指定目录是否存在，如果指定目录不存在，需要设为程序根目录
            if (Directory.Exists(root))
            {
                this.ServerRoot = root;
            }
            else
            {
                this.ServerRoot = AppDomain.CurrentDomain.BaseDirectory;
            }
        }

        // 构造函数
        public Server(int port) : this(port, AppDomain.CurrentDomain.BaseDirectory)
        {
        }

        // 启动服务器
        public void Start()
        {
            if (this.IsRunning)
            {
                this.Log("服务器已经在运行了");
                return;
            }

            //创建服务器Socket
            this.serverListener = new TcpListener(IPAddress.Any, ServerPort);
            this.Protocol = this.serverCertificate == null ? Protocols.Http : Protocols.Https;
            this.IsRunning = true;
            this.serverListener.Start();

            // 打印服务器状态
            this.Log(string.Format("Sever is running at {0}://{1}:{2}", Protocol.ToString().ToLower(), IPAddress.Any, ServerPort));

            this.StartAccept();
        }

        public async void StartAccept()
        {
            TcpClient client = await this.serverListener.AcceptTcpClientAsync();
            ProcessAccept(client);
            // 再次监听新的连接对象
            StartAccept();
        }

        /// <summary>
        /// 设置SSL
        /// </summary>
        /// <param name="certificate">SSL证书路径</param>
        /// <returns></returns>
        public Server SetSSL(string certificate)
        {
            return SetSSL(X509Certificate.CreateFromCertFile(certificate));
        }

        /// <summary>
        /// 设置SSL
        /// </summary>
        /// <param name="certifiate">SSL证书</param>
        /// <returns></returns>
        public Server SetSSL(X509Certificate certifiate)
        {
            this.serverCertificate = certifiate;
            return this;
        }

        /// <summary>
        /// 设置服务器目录
        /// </summary>
        /// <param name="root">根目录</param>
        public Server SetRoot(string root)
        {
            if (Directory.Exists(root))
            {
                this.ServerRoot = root;
            }
            else
            {
                this.ServerRoot = AppDomain.CurrentDomain.BaseDirectory;
            }

            return this;
        }

        /// <summary>
        /// 获取服务器目录
        /// </summary>
        public string GetRoot()
        {
            return this.ServerRoot;
        }

        /// <summary>
        /// 处理客户端请求
        /// </summary>
        /// <param name="client">客户端Socket</param>
        private void ProcessAccept(TcpClient client)
        {
            // 处理请求
            Stream clientStream = client.GetStream();

            // 首先要处理SSL
            if (this.Protocol == Protocols.Https && this.serverCertificate != null)
            {
                clientStream = ProcessSSL(clientStream);
            }

            // 如果数据流为空，说明是一个无效的网络请求
            if (clientStream == null)
            {
                client.Dispose();
                client.Close();
                return;
            }

            // 需要处理这个请求，判断是Get还是Post还是其他，暂时只处理Get和Post
            UserToken token = new UserToken(client, clientStream, this.Logger);

            center.MessageReceive(token);
        }

        /// <summary>
        /// 处理ssl加密请求
        /// </summary>
        /// <param name="clientStream"></param>
        /// <returns></returns>
        private Stream ProcessSSL(Stream clientStream)
        {
            try
            {
                SslStream sslStream = new SslStream(clientStream);
                sslStream.AuthenticateAsServer(serverCertificate, false, SslProtocols.Tls, true);
                sslStream.ReadTimeout = 10000;
                sslStream.WriteTimeout = 10000;
                return sslStream;
            }
            catch (Exception e)
            {
                Log(e.Message);
                clientStream.Close();
            }

            return null;
        }

        // 打印日志
        private void Log(object message)
        {
            if (this.Logger != null)
            {
                Logger.Log(message);
            }
        }
    }
}
