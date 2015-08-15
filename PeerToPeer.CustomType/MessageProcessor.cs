using System;
using Amqp.Listener;

namespace PeerToPeer.CustomType
{
    public class MessageProcessor : IMessageProcessor
    {
        public int Credit
        {
            get { return 300; }
        }

        public void Process(MessageContext messageContext)
        {
            //TODO - how to deserialize the body to type Person?

            messageContext.Complete();
        }
    }
}
