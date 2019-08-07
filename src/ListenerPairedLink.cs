using System;
using Amqp.Framing;
using Amqp.Listener;

namespace Amqp
{
    /// <summary>
    /// Instances of this class hold listener-side paired links after
    /// they have been accepted 
    /// </summary>
    public class ListenerPairedLink : IDisposable
    {
        /// <summary>
        /// Creates a new paired link   
        /// </summary>
        /// <param name="linkName">Name of the link</param>
        public ListenerPairedLink(string linkName)
        {
            LinkName = linkName;
        }

        /// <summary>
        /// Gets the name of the link
        /// </summary>
        public string LinkName { get; }

        /// <summary>
        /// Receiving link that transfers messages into this container
        /// </summary>
        public ListenerLink Receiver { get; protected internal set; }

        /// <summary>
        /// Sending link that transfers messages out of this container
        /// </summary>
        public ListenerLink Sender { get; protected internal set; }

        /// <summary>
        /// Disposes the links (force detach)
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