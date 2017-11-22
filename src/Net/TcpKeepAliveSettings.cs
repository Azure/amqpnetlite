namespace Amqp
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
