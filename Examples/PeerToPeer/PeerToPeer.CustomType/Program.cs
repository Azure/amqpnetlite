using System;
using System.Collections.Generic;
using Amqp;
using Amqp.Listener;

namespace PeerToPeer.CustomType
{
    class Program
    {
        private const string Address = "amqp://guest:guest@127.0.0.1:5672";
        private const string MsgProcName = "messages";

        static void Main(string[] args)
        {
            //Create host and register message processor
            var uri = new Uri(Address);
            var host = new ContainerHost(new List<Uri>() { uri }, null, uri.UserInfo);
            host.RegisterMessageProcessor(MsgProcName, new MessageProcessor());
            host.Open();

            //Create client
            var connection = new Connection(new Address(Address));
            var session = new Session(connection);
            var sender = new SenderLink(session, "message-client", MsgProcName);

            //Send message with an object of custom type as the body
            var person = new Person() { EyeColor = "brown", Height = 175, Weight = 75 };
            sender.Send(new Message(person));

            sender.Close();
            session.Close();
            connection.Close();

            host.Close();
        }
    }
}
