using System;
using Amqp.Framing;
using Amqp.Listener;

namespace Amqp
{
    /// <summary>
    /// 
    /// </summary>
    public class ListenerPairedLink : IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="linkName"></param>
        public ListenerPairedLink(string linkName)
        {
            LinkName = linkName;
        }

        /// <summary>
        /// 
        /// </summary>
        public string LinkName { get; }

        /// <summary>
        /// 
        /// </summary>
        public ListenerLink Receiver { get; protected internal set; }

        /// <summary>
        /// 
        /// </summary>
        public ListenerLink Sender { get; protected internal set; }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            Error error = new Error(ErrorCode.DetachForced) { Description = "Processor was unregistered." };
            if (Receiver != null)
            {
                if (!Receiver.IsClosed)
                {
                    Receiver.CloseInternal(0, error);
                }

                Receiver = null;
            }

            if (Sender != null)
            {
                if (!Sender.IsClosed)
                {
                    Sender.CloseInternal(0, error);
                }

                Sender = null;
            }
        }
    }
}