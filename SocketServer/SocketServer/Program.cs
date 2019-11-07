using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TcpSocketNetFramework;
using WebSocketNetFramework;

namespace SocketServer
{
    class Program
    {
        static void Main(string[] args)
        {
            ConsoleLogger logger = new ConsoleLogger();
            HandlerCenter center = new HandlerCenter(logger);

            // 启动TCP服务器
            TcpSocketServer tcpSocketServer = new TcpSocketServer(9000);
            tcpSocketServer.logger = logger;
            tcpSocketServer.center = center;
            tcpSocketServer.Start(10086);
            logger.Log("TcpSocket服务器启动成功");

            WebSocketServer webSocketServer = new WebSocketServer(9000);
            webSocketServer.logger = logger;
            webSocketServer.center = center;
            webSocketServer.Start(10010);
            logger.Log("WebSocket服务器启动成功");

            while (Console.ReadLine() != "exit") { };
        }
    }
}
