//  ------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation
//  All rights reserved. 
//  
//  Licensed under the Apache License, Version 2.0 (the ""License""); you may not use this 
//  file except in compliance with the License. You may obtain a copy of the License at 
//  http://www.apache.org/licenses/LICENSE-2.0  
//  
//  THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, 
//  EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR 
//  CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR 
//  NON-INFRINGEMENT. 
// 
//  See the Apache Version 2.0 License for specific language governing permissions and 
//  limitations under the License.
//  ------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using Amqp.Framing;
using Amqp.Listener;
using Amqp;
#if !(NETMF || COMPACT_FRAMEWORK)
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif

namespace Test.Amqp.Net
{
#if !(NETMF || COMPACT_FRAMEWORK)
    [TestClass]
#endif
    public class ContainerHostTests
    {
        private const string Endpoint = "amqp://guest:guest@localhost:5765"; //pick a port other than 5762 so that it doesn't conflict with the test broker
        private static readonly Uri Uri = new Uri(Endpoint);
        private static readonly Address Address = new Address(Endpoint);
        private const int SendTimeout = 5000;

        /// <summary>
        /// Verifies that the container host kills existing connections when it closes the listeners.
        /// Also verifies once the host reopens the listeners a new client with the same link name can be reestablished and send data.
        /// </summary>
#if !(NETMF || COMPACT_FRAMEWORK)
        [TestMethod]
#endif
        public void TestMethod_CloseKillsExistingConnection()
        {
            const string MsgProcName = "processor";

            //Create a ContainerHost, add a message processor and start listening for connections
            var host = new ContainerHost(new List<Uri>() { Uri }, null, Uri.UserInfo);
            var processor = new TestMessageProcessor();
            host.RegisterMessageProcessor(MsgProcName, processor);
            host.Open();

            //Create a client to send data to the host message processor
            var connClosed = false;
            var connection = new Connection(Address);
            connection.Closed += (AmqpObject obj, Error error) =>
            {
                connClosed = true;
            };

            var session = new Session(connection);
            var sender = new SenderLink(session, "link", MsgProcName);

            try
            {
                //Send one message while the host is open
                var msg1 = "a";
                sender.Send(new Message(msg1), SendTimeout);

                //Close the host
                //This should close existing connections
                host.Close();

                //Try sending a second message
                //This should fail if the host closed existing connections
                bool threw = false;
                try
                {
                    var msg2 = "b";
                    sender.Send(new Message(msg2), SendTimeout);
                }
                catch (AmqpException)
                {
                    //The connection is closed and this proves we could not continue to send data
                    threw = true;
                }

                //Verify the connection was closed and no data was written
                Assert.IsTrue(threw);
                Assert.IsTrue(connClosed);

                //Only the first message should be written
                Assert.AreEqual(1, processor.Messages.Count);
                Assert.AreEqual(msg1, processor.Messages[0].GetBody<string>());

                //Make sure a connection can be established and messages can be sent again
                host.Open();

                //Create a new client
                connection = new Connection(Address);
                session = new Session(connection);
                sender = new SenderLink(session, "link", MsgProcName);

                //Send a message
                var msg3 = "c";
                sender.Send(new Message(msg3), SendTimeout);

                //Make sure the message processor got the message
                Assert.AreEqual(2, processor.Messages.Count);
                Assert.AreEqual(msg3, processor.Messages[1].GetBody<string>());
            }
            finally
            {
                sender.Close();
                session.Close();
                connection.Close();

                host.Close();
            }
        }
    }

    class TestMessageProcessor : IMessageProcessor
    {
        public TestMessageProcessor()
        {
            this.Messages = new List<Message>();
        }

        public List<Message> Messages
        {
            get;
            private set;
        }

        public int Credit
        {
            get { return 300; }
        }

        public void Process(MessageContext messageContext)
        {
            this.Messages.Add(messageContext.Message);
            messageContext.Complete();
        }
    }
}
