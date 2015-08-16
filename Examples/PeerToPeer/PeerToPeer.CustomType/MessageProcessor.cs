using System;
using Amqp;
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
            var person = messageContext.Message.GetBody<Person>();
            Console.WriteLine(person.ToString());

            messageContext.Complete();
        }
    }
}
