using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebSocketNetFramework
{
    public delegate byte[] LengthEncode(byte[] value);
    public delegate byte[] LengthDecode(ref List<byte> value);

    public delegate byte[] MessageEncode(object value);
    public delegate object MessageDecode(byte[] value);

    public delegate void CloseProcess(UserToken uws, string error);
}
