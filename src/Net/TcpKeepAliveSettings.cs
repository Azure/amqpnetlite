namespace Amqp
{
    /// <summary>
    /// TCP Keep-Alive settings.
    /// </summary>
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
