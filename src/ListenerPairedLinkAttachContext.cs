using System;
using Amqp.Framing;
using Amqp.Listener;

namespace Amqp
{
    /// <summary>
    /// 
    /// </summary>
    public class ListenerPairedLinkAttachContext : ListenerPairedLink
    {
        /// <summary>
        /// 
        /// </summary>
        public Attach ReceiverAttach { get; protected internal set; }

        /// <summary>
        /// 
        /// </summary>
        public Attach SenderAttach { get; protected internal set; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="linkName"></param>
        public ListenerPairedLinkAttachContext(string linkName) : base(linkName)
        {
        }
    }
}