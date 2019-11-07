using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetFramework
{
    public interface IMessage
    {
        void MessageReceive(UserToken token);
    }
}
