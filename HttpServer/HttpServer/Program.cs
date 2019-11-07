using NetFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Server server = new Server(8080);
            server.SetRoot(@"D:\HttpServer");
            server.Logger = new ConsoleLogger();
            server.center = new MessageCenter(server.GetRoot());
            server.Start();

            Console.ReadKey();
        }
    }
}
