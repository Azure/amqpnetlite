using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amqp.Net
{
    public class TcpKeepAliveSettings
    {
        public ulong KeepAliveTime
        {
            get;
            set;
        }

        public ulong KeepAliveInterval
        {
            get;
            set;
        }
    }
}
