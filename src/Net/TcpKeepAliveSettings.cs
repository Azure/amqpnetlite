namespace Amqp
{
    /// <summary>
    /// TCP Keep-Alive settings.
    /// </summary>
    public class TcpKeepAliveSettings
    {
        /// <summary>
        /// How often a keep-alive transmission is sent to an idle connection.
        /// </summary>
        public uint KeepAliveTime
        {
            get;
            set;
        }

        /// <summary>
        /// How often a keep-alive transmission is sent when no response is received from previous keep-alive transmissions.
        /// </summary>
        public uint KeepAliveInterval
        {
            get;
            set;
        }
    }
}
