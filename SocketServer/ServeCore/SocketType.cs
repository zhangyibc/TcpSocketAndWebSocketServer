using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServeCore
{
    // Server类型，是HTTP还是HTTPS
    public enum SocketTypes
    {
        TcpSocket = 0,
        WebSocket = 1
    }
}
