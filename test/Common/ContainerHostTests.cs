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
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Amqp;
using Amqp.Framing;
using Amqp.Handler;
using Amqp.Listener;
using Amqp.Sasl;
using Amqp.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Sockets;
using System.Text;

namespace Test.Amqp
{
    [TestClass]
    public class ContainerHostTests
    {
        TimeSpan Timeout = TimeSpan.FromMilliseconds(5000);
        ContainerHost host;
        ILinkProcessor linkProcessor;

        public Address Address
        {
            get;
            set;
        }

        static ContainerHostTests()
        {
            //Trace.TraceLevel = TraceLevel.Frame;
            //Trace.TraceListener = (l, f, a) => System.Diagnostics.Trace.WriteLine(DateTime.Now.ToString("[hh:mm:ss.fff]") + " " + string.Format(f, a));
        }

        public void ClassInitialize()
        {
            // pick a port other than 5762 so that it doesn't conflict with the test broker
            this.Address = new Address("amqp://guest:guest@localhost:15672");

            this.host = new ContainerHost(this.Address);
            this.host.Listeners[0].SASL.EnableExternalMechanism = true;
            this.host.Listeners[0].SASL.EnableAnonymousMechanism = true;
            this.host.Open();
        }

        public void ClassCleanup()
        {
            if (this.host != null)
            {
                this.host.Close();
            }
        }

        public void TestCleanup()
        {
            if (this.linkProcessor != null)
            {
                this.host.UnregisterLinkProcessor(this.linkProcessor);
                this.linkProcessor = null;
            }
        }

        [TestInitialize]
        public void MyTestInitialize()
        {
            this.ClassInitialize();
        }

        [TestCleanup]
        public void MyTestCleanup()
        {
            this.linkProcessor = null;
            this.ClassCleanup();
        }

        [TestMethod]
        public void ContainerHostMessageProcessorTest()
        {
            string name = "ContainerHostMessageProcessorTest";
            var processor = new TestMessageProcessor();
            this.host.RegisterMessageProcessor(name, processor);

            int count = 500;
            var connection = new Connection(Address);
            var session = new Session(connection);
            var sender = new SenderLink(session, "send-link", name);

            for (int i = 0; i < count; i++)
            {
                var message = new Message("msg" + i);
                message.Properties = new Properties() { GroupId = name };
                sender.Send(message, Timeout);
            }

            sender.Close();
            session.Close();
            connection.Close();

            Assert.AreEqual(count, processor.Messages.Count);
            for (int i = 0; i < count; i++)
            {
                var message = processor.Messages[i];
                Assert.AreEqual("msg" + i, message.GetBody<string>());
            }
        }

        [TestMethod]
        public void ContainerHostMessageSourceTest()
        {
            string name = "ContainerHostMessageSourceTest";
            int count = 100;
            Queue<Message> messages = new Queue<Message>();
            for (int i = 0; i < count; i++)
            {
                messages.Enqueue(new Message("test") { Properties = new Properties() { MessageId = name + i } });
            }

            var source = new TestMessageSource(messages);
            this.host.RegisterMessageSource(name, source);

            var connection = new Connection(Address);
            var session = new Session(connection);
            var receiver = new ReceiverLink(session, "receiver0", name);
            int released = 0;
            int rejected = 0;
            int ignored = 0;
            for (int i = 1; i <= count; i++)
            {
                Message message = receiver.Receive();
                if (i % 5 == 0)
                {
                    receiver.Reject(message);
                    rejected++;
                }
                else if (i % 17 == 0)
                {
                    receiver.Release(message);
                    released++;
                }
                else if (i % 36 == 0)
                {
                    ignored++;
                }
                else
                {
                    receiver.Accept(message);
                }
            }

            receiver.Close();
            session.Close();
            connection.Close();

            Thread.Sleep(500);
            Assert.AreEqual(released + ignored, messages.Count, string.Join(",", messages.Select(m => m.Properties.MessageId)));
            Assert.AreEqual(rejected, source.DeadletterMessage.Count, string.Join(",", source.DeadletterMessage.Select(m => m.Properties.MessageId)));
        }

        [TestMethod]
        public void ContainerHostRequestProcessorTest()
        {
            string name = "ContainerHostRequestProcessorTest";
            var processor = new TestRequestProcessor();
            this.host.RegisterRequestProcessor(name, processor);

            int count = 500;
            var connection = new Connection(Address);
            var session = new Session(connection);

            string replyTo = "client-reply-to";
            Attach recvAttach = new Attach()
            {
                Source = new Source() { Address = name },
                Target = new Target() { Address = replyTo }
            };

            var doneEvent = new ManualResetEvent(false);
            List<string> responses = new List<string>();
            ReceiverLink receiver = new ReceiverLink(session, "request-client-receiver", recvAttach, null);
            receiver.Start(
                20,
                (link, message) =>
                {
                    responses.Add(message.GetBody<string>());
                    link.Accept(message);
                    if (responses.Count == count)
                    {
                        doneEvent.Set();
                    }
                });

            SenderLink sender = new SenderLink(session, "request-client-sender", name);
            for (int i = 0; i < count; i++)
            {
                Message request = new Message("Hello");
                request.Properties = new Properties() { MessageId = "request" + i, ReplyTo = replyTo };
                sender.Send(request, null, null);
            }

            Assert.IsTrue(doneEvent.WaitOne(10000), "Not completed in time");

            receiver.Close();
            sender.Close();
            session.Close();
            connection.Close();

            Assert.AreEqual(count, processor.TotalCount);
            Assert.AreEqual(count, responses.Count);
            for (int i = 1; i <= count; i++)
            {
                Assert.AreEqual("OK" + i, responses[i - 1]);
            }
        }

        [TestMethod]
        public void ContainerHostLinkProcessorTest()
        {
            string name = "ContainerHostLinkProcessorTest";
            this.host.RegisterLinkProcessor(this.linkProcessor = new TestLinkProcessor());

            int count = 80;
            var connection = new Connection(Address);
            var session = new Session(connection);
            var sender = new SenderLink(session, "send-link", "any");

            for (int i = 0; i < count; i++)
            {
                var message = new Message("msg" + i);
                message.Properties = new Properties() { GroupId = name };
                sender.Send(message, Timeout);
            }

            sender.Close();
            session.Close();
            connection.Close();
        }

        [TestMethod]
        public void ContainerHostTargetLinkEndpointTest()
        {
            string name = "ContainerHostTargetLinkEndpointTest";
            List<Message> messages = new List<Message>();
            this.linkProcessor = new TestLinkProcessor(
                link => new TargetLinkEndpoint(new TestMessageProcessor(50, messages), link));
            this.host.RegisterLinkProcessor(this.linkProcessor);

            int count = 190;
            var connection = new Connection(Address);
            var session = new Session(connection);
            var sender = new SenderLink(session, "send-link", "any");

            for (int i = 0; i < count; i++)
            {
                var message = new Message("msg" + i);
                message.Properties = new Properties() { GroupId = name };
                sender.Send(message, Timeout);
            }

            sender.Close();
            session.Close();
            connection.Close();

            Assert.AreEqual(count, messages.Count);
        }

        [TestMethod]
        public void ContainerHostSourceLinkEndpointTest()
        {
            string name = "ContainerHostSourceLinkEndpointTest";
            int count = 100;
            Queue<Message> messages = new Queue<Message>();
            for (int i = 0; i < count; i++)
            {
                messages.Enqueue(new Message("test") { Properties = new Properties() { MessageId = name + i } });
            }

            var source = new TestMessageSource(messages);
            this.linkProcessor = new TestLinkProcessor(link => new SourceLinkEndpoint(source, link));
            this.host.RegisterLinkProcessor(this.linkProcessor);

            var connection = new Connection(Address);
            var session = new Session(connection);
            var receiver = new ReceiverLink(session, "receiver0", name);
            int released = 0;
            int rejected = 0;
            for (int i = 1; i <= count; i++)
            {
                Message message = receiver.Receive();
                if (i % 5 == 0)
                {
                    receiver.Reject(message);
                    rejected++;
                }
                else if (i % 17 == 0)
                {
                    receiver.Release(message);
                    released++;
                }
                else
                {
                    receiver.Accept(message);
                }
            }

            receiver.Close();
            session.Close();
            connection.Close();

            Thread.Sleep(200);
            Assert.AreEqual(released, messages.Count);
            Assert.AreEqual(rejected, source.DeadletterMessage.Count);
        }

        [TestMethod]
        public void ContainerHostMultipleClientsTest()
        {
            string name = "ContainerHostMultipleClientsTest";
            var processor = new TestMessageProcessor();
            this.host.RegisterMessageProcessor(name, processor);

            var connection = new Connection(Address);

            // client 1
            {
                var session = new Session(connection);
                var sender = new SenderLink(session, "send-link-1", name);
                sender.Send(new Message("msg1"), Timeout);
            }

            // client 2
            {
                var session = new Session(connection);
                var sender = new SenderLink(session, "send-link-2", name);
                sender.Send(new Message("msg2"), Timeout);
            }

            connection.Close();
        }


        [TestMethod]
        public void ContainerHostProcessorOrderTest()
        {
            string name = "ContainerHostProcessorOrderTest";
            List<Message> messages = new List<Message>();
            this.host.RegisterMessageProcessor(name, new TestMessageProcessor(50, messages));
            this.linkProcessor = new TestLinkProcessor();
            this.host.RegisterLinkProcessor(this.linkProcessor);

            int count = 80;
            var connection = new Connection(Address);
            var session = new Session(connection);
            var sender = new SenderLink(session, "send-link", name);

            for (int i = 0; i < count; i++)
            {
                var message = new Message("msg" + i);
                message.Properties = new Properties() { GroupId = name };
                sender.Send(message, Timeout);
            }

            sender.Close();

            this.host.RegisterMessageSource(name, new TestMessageSource(new Queue<Message>(messages)));
            var receiver = new ReceiverLink(session, "recv-link", name);
            for (int i = 0; i < count; i++)
            {
                var message = receiver.Receive();
                receiver.Accept(message);
            }

            receiver.Close();

            sender = new SenderLink(session, "send-link", "any");
            for (int i = 0; i < count; i++)
            {
                var message = new Message("msg" + i);
                message.Properties = new Properties() { GroupId = name };
                sender.Send(message, Timeout);
            }

            sender.Close();
            session.Close();
            connection.Close();
        }

        [TestMethod]
        public void ContainerHostUnknownProcessorTest()
        {
            string name = "ContainerHostUnknownProcessorTest";
            this.host.RegisterMessageProcessor("message" + name, new TestMessageProcessor());
            this.host.RegisterRequestProcessor("request" + name, new TestRequestProcessor());

            var connection = new Connection(Address);
            var session = new Session(connection);
            var sender = new SenderLink(session, "send-link", name);

            try
            {
                sender.Send(new Message("test"));
                Assert.IsTrue(false, "exception not thrown");
            }
            catch(AmqpException exception)
            {
                Assert.IsTrue(exception.Error != null, "Error is null");
                Assert.AreEqual((Symbol)ErrorCode.NotFound, exception.Error.Condition, "Wrong error code");
            }

            sender.Close();
            session.Close();
            connection.Close();
        }

        [TestMethod]
        public void ContainerHostConnectionIdleTimeoutTest()
        {
            string name = "ContainerHostConnectionIdleTimeoutTest";
            this.host.RegisterMessageProcessor(name, new TestMessageProcessor());
            this.host.Listeners[0].AMQP.IdleTimeout = 1000;

            var connection = new Connection(Address, null, null, (c, o) => o.IdleTimeOut = 0);
            var session = new Session(connection);
            var sender = new SenderLink(session, "send-link", name);
            sender.Send(new Message("test") { Properties = new Properties() { MessageId = name } });
            Thread.Sleep(1100);
            Assert.IsTrue(connection.Error != null, "error should be set");
            Assert.AreEqual((Symbol)ErrorCode.ConnectionForced, connection.Error.Condition);
        }

        [TestMethod]
        public void ContainerHostIncorrectProcessorTest()
        {
            this.host.RegisterMessageProcessor("message-processor", new TestMessageProcessor());
            this.host.RegisterMessageSource("message-source", new TestMessageSource(new Queue<Message>()));
            Error error;

            var connection = new Connection(Address);
            var session = new Session(connection);
            var sender = new SenderLink(session, "send-link", "message-source");

            try
            {
                sender.Send(new Message("test"));
                Assert.IsTrue(false, "sender exception not thrown");
            }
            catch (AmqpException exception)
            {
                error = exception.Error;
                Assert.IsTrue(error != null, "Error is null");
                Assert.IsTrue(error.Condition.Equals((Symbol)ErrorCode.NotFound) ||
                    error.Condition.Equals((Symbol)ErrorCode.IllegalState), "Wrong error code for sender " + error);
            }

            var receiver = new ReceiverLink(session, "recv-link", "message-processor");
            try
            {
                var message = receiver.Receive(Timeout);
                Thread.Sleep(100);
                error = receiver.Error;
            }
            catch (AmqpException exception)
            {
                error = exception.Error;
            }

            Assert.IsTrue(error != null, "Error is null");
            Assert.AreEqual((Symbol)ErrorCode.NotFound, error.Condition, "Wrong error code for receiver");

            session.Close();
            connection.Close();
        }

        [TestMethod]
        public void ContainerHostDefaultValueTest()
        {
            string name = "ContainerHostDefaultValueTest";
            this.host.RegisterMessageProcessor(name, new TestMessageProcessor());

            Open remoteOpen = null;
            Begin remoteBegin = null;
            Attach remoteAttach = null;

            var connection = new Connection(Address, null, new Open() { ContainerId = "c" }, (c, o) => remoteOpen = o);
            var session = new Session(connection, new Begin() { NextOutgoingId = 3 }, (s, b) => remoteBegin = b);
            var sender1 = new SenderLink(session, "send-link1", new Attach() { Role = false, Target = new Target() { Address = name }, Source = new Source() }, (l, a) => remoteAttach = a);
            var sender2 = new SenderLink(session, "send-link2", new Attach() { Role = false, Target = new Target() { Address = name }, Source = new Source() }, (l, a) => remoteAttach = a);

            sender1.Send(new Message("m1"));
            sender2.Send(new Message("m2"));

            session.Close();
            connection.Close();

            Assert.IsTrue(remoteOpen != null, "remote open not received");
            Assert.IsTrue(remoteOpen.MaxFrameSize < uint.MaxValue, "max frame size not set");
            Assert.IsTrue(remoteOpen.ChannelMax < ushort.MaxValue, "channel max not set");

            Assert.IsTrue(remoteBegin != null, "remote begin not received");
            Assert.IsTrue(remoteBegin.IncomingWindow < uint.MaxValue, "incoming window not set");
            Assert.IsTrue(remoteBegin.HandleMax < uint.MaxValue, "handle max not set");

            Assert.IsTrue(remoteAttach != null, "remote attach not received");
        }

        [TestMethod]
        public void ContainerHostMultiplexingTest()
        {
            string name = "ContainerHostMultiplexingTest";
            this.host.RegisterMessageProcessor(name, new TestMessageProcessor());

            int completed = 0;
            ManualResetEvent doneEvent = new ManualResetEvent(false);
            var connection = new Connection(Address, null, new Open() { ContainerId = name }, null);
            for (int i = 0; i < 10; i++)
            {
                var session = new Session(connection, new Begin() { NextOutgoingId = (uint)i }, null);
                for (int j = 0; j < 20; j++)
                {
                    var link = new SenderLink(session, string.Join("-", name, i, j), name);
                    for (int k = 0; k < 30; k++)
                    {
                        link.Send(
                            new Message() { Properties = new Properties() { MessageId = string.Join("-", "msg", i, j, k) } },
                            (l, m, o, s) => { if (Interlocked.Increment(ref completed) >= 10 * 20 * 30) doneEvent.Set(); },
                            null);
                    }
                }
            }

            Assert.IsTrue(doneEvent.WaitOne(10000), "send not completed in time");
            connection.Close();
        }

        [TestMethod]
        public void ContainerHostCloseTest()
        {
            string name = "ContainerHostCloseTest";
            var address = new Address("amqp://guest:guest@localhost:15673");

            ContainerHost h = new ContainerHost(address);
            h.Open();
            h.RegisterMessageProcessor(name, new TestMessageProcessor());

            //Create a client to send data to the host message processor
            var closedEvent = new ManualResetEvent(false);
            var connection = new Connection(address);
            connection.Closed += (IAmqpObject obj, Error error) =>
            {
                closedEvent.Set();
            };

            var session = new Session(connection);
            var sender = new SenderLink(session, "sender-link", name);

            //Send one message while the host is open
            sender.Send(new Message("Hello"), Timeout);

            //Close the host. this should close existing connections
            h.Close();

            Assert.IsTrue(closedEvent.WaitOne(10000), "connection is not closed after host is closed.");

            try
            {
                sender.Send(new Message("test"));
                Assert.IsTrue(false, "exception not thrown");
            }
            catch (AmqpException exception)
            {
                Assert.IsTrue(exception.Error != null, "Error is null");
                Assert.AreEqual((Symbol)ErrorCode.ConnectionForced, exception.Error.Condition, "Wrong error code");
            }

            connection.Close();

            // Reopen the host and send again
            // Use a different port as on some system the port is not released immediately
            address = new Address("amqp://guest:guest@localhost:15674");
            h = new ContainerHost(address);
            h.RegisterMessageProcessor(name, new TestMessageProcessor());
            h.Open();

            connection = new Connection(address);
            session = new Session(connection);
            sender = new SenderLink(session, "sender-link", name);
            sender.Send(new Message("Hello"), Timeout);
            connection.Close();

            h.Close();
        }

        [TestMethod]
        public void ContainerHostSessionFlowControlTest()
        {
            string name = "ContainerHostSessionFlowControlTest";
            this.host.RegisterMessageProcessor(name, new TestMessageProcessor(500000, null));

            // this test assumes that nMsgs is greater than session's window size
            int nMsgs = 10000;
            var connection = new Connection(Address);
            var session = new Session(connection);
            var sender = new SenderLink(session, "send-link", name);
            for (int i = 0; i < nMsgs; i++)
            {
                var message = new Message("msg" + i);
                message.Properties = new Properties() { GroupId = name };
                sender.Send(message, Timeout);
            }

            sender.Close();
            session.Close();
            connection.Close();
        }

        [TestMethod]
        public void ContainerHostDeliveryHandlerTest()
        {
            string name = "ContainerHostSendDeliveryHandlerTest";

            IDelivery sendDelivery = null;
            IDelivery receiveDelivery = null;
            IDelivery listenerDelivery = null;
            var clientHandler = new TestHandler(e =>
            {
                if (e.Id == EventId.SendDelivery)
                {
                    sendDelivery = (IDelivery)e.Context;
                    sendDelivery.Batchable = true;
                    sendDelivery.Tag = new byte[] { 8, 79 };
                }
                else if (e.Id == EventId.ReceiveDelivery)
                {
                    receiveDelivery = (IDelivery)e.Context;
                }
            });
            var listenerHandler = new TestHandler(e =>
            {
                if (e.Id == EventId.SendDelivery)
                {
                    ((IDelivery)e.Context).Tag = Guid.NewGuid().ToByteArray();
                }
                else if (e.Id == EventId.ReceiveDelivery)
                {
                    listenerDelivery = (IDelivery)e.Context;
                }
            });

            List<Message> messages = new List<Message>();
            this.host.RegisterMessageProcessor(name, new TestMessageProcessor(10, messages));
            this.host.Listeners[0].HandlerFactory = a => listenerHandler;

            var connection = new Connection(Address, clientHandler);
            var session = new Session(connection);
            var sender = new SenderLink(session, "send-link", name);
            sender.Send(new Message(name));
            sender.Close();

            this.host.RegisterMessageSource(name, new TestMessageSource(new Queue<Message>(messages)));
            var receiver = new ReceiverLink(session, "recv-link", name);
            var message = receiver.Receive();
            receiver.Accept(message);
            session.Close();
            connection.Close();

            Assert.IsTrue(sendDelivery != null);
            Assert.AreEqual(sendDelivery.Batchable, true);
            Assert.AreEqual((byte)8, sendDelivery.Tag[0]);
            Assert.AreEqual((byte)79, sendDelivery.Tag[1]);

            Assert.IsTrue(listenerDelivery != null);
            Assert.AreEqual(listenerDelivery.Batchable, true);
            Assert.AreEqual((byte)8, listenerDelivery.Tag[0]);
            Assert.AreEqual((byte)79, listenerDelivery.Tag[1]);

            Assert.IsTrue(receiveDelivery != null);
            Assert.AreEqual(16, receiveDelivery.Tag.Length);
        }

        [TestMethod]
        public void DuplicateLinkNameSameSessionTest()
        {
            string name = "DuplicateLinkNameSameSessionTest";
            this.host.RegisterMessageProcessor(name, new TestMessageProcessor(500000, null));

            string linkName = "same-send-link";
            var connection = new Connection(Address);
            var session = new Session(connection);
            var sender1 = new SenderLink(session, linkName, name);
            sender1.Send(new Message("msg1"), Timeout);

            try
            {
                var sender2 = new SenderLink(session, linkName, name);
                Assert.IsTrue(false, "Excpected exception not thrown");
            }
            catch(AmqpException ae)
            {
                Assert.AreEqual((Symbol)ErrorCode.NotAllowed, ae.Error.Condition);
            }

            sender1.Close();
            session.Close();
            connection.Close();
        }

        [TestMethod]
        public void DuplicateLinkNameSameConnectionTest()
        {
            string name = "DuplicateLinkNameSameConnectionTest";
            this.host.RegisterMessageProcessor(name, new TestMessageProcessor(500000, null));

            string linkName = "same-send-link";
            var connection = new Connection(Address);

            var session1 = new Session(connection);
            var sender1 = new SenderLink(session1, linkName, name);

            var session2 = new Session(connection);
            var sender2 = new SenderLink(session2, linkName, name);

            sender1.Send(new Message("msg1"), Timeout);

            try
            {
                sender2.Send(new Message("msg1"), Timeout);
                Assert.IsTrue(false, "Excpected exception not thrown");
            }
            catch (AmqpException ae)
            {
                Assert.AreEqual((Symbol)ErrorCode.Stolen, ae.Error.Condition);
            }

            connection.Close();
        }

        [TestMethod]
        public void DuplicateLinkNameDifferentContainerTest()
        {
            string name = "DuplicateLinkNameDifferentContainerTest";
            this.host.RegisterMessageProcessor(name, new TestMessageProcessor(500000, null));

            string linkName = "same-send-link";
            var connection1 = new Connection(Address);
            var session1 = new Session(connection1);
            var sender1 = new SenderLink(session1, linkName, name);

            var connection2 = new Connection(Address);
            var session2 = new Session(connection2);
            var sender2 = new SenderLink(session2, linkName, name);

            sender1.Send(new Message("msg1"), Timeout);
            sender1.Send(new Message("msg1"), Timeout);

            connection1.Close();
            connection2.Close();
        }

        [TestMethod]
        public void DuplicateLinkNameDifferentRoleTest()
        {
            string name = "DuplicateLinkNameDifferentRoleTest";
            this.linkProcessor = new TestLinkProcessor();
            this.host.RegisterLinkProcessor(this.linkProcessor);

            string linkName = "same-link-for-different-role";
            var connection = new Connection(Address);
            var session1 = new Session(connection);
            var sender = new SenderLink(session1, linkName, name);
            sender.Send(new Message("msg1"), Timeout);

            var session2 = new Session(connection);
            var receiver = new ReceiverLink(session2, linkName, name);
            receiver.SetCredit(2, false);
            var message = receiver.Receive();
            Assert.IsTrue(message != null, "No message was received");
            receiver.Accept(message);

            connection.Close();
        }

        [TestMethod]
        public void ContainerHostSaslAnonymousTest()
        {
            string name = "ContainerHostSaslAnonymousTest";
            ListenerLink link = null;
            var linkProcessor = new TestLinkProcessor();
            linkProcessor.SetHandler(a => { link = a.Link; return false; });
            this.host.RegisterLinkProcessor(this.linkProcessor = linkProcessor);

            var factory = new ConnectionFactory();
            factory.SASL.Profile = SaslProfile.Anonymous;
            var connection = factory.CreateAsync(new Address(Address.Host, Address.Port, null, null, "/", Address.Scheme)).Result;
            var session = new Session(connection);
            var sender = new SenderLink(session, name, name);
            sender.Send(new Message("msg1"), Timeout);
            connection.Close();

            Assert.IsTrue(link != null, "link is null");
            var listenerConnection = (ListenerConnection)link.Session.Connection;
            Assert.IsTrue(listenerConnection.Principal == null, "principal should be null");
        }

        [TestMethod]
        public void ContainerHostSaslUnknownTest()
        {
            Address a = new Address(Address.Host, Address.Port, null, null, "/", Address.Scheme);
            SaslProfile u = new CustomSaslProfile("UNKNOWN");
            try
            {
                var c = new Connection(a, u, null, null);
                Assert.IsTrue(false, "connection should fail");
            }
            catch(AmqpException exception)
            {
                Assert.AreEqual((Symbol)ErrorCode.NotImplemented, exception.Error.Condition);
            }

            var factory = new ConnectionFactory();
            factory.SASL.Profile = u;
            try
            {
                var c = factory.CreateAsync(a).GetAwaiter().GetResult();
                Assert.IsTrue(false, "connection should fail");
            }
            catch (AmqpException exception)
            {
                Assert.AreEqual((Symbol)ErrorCode.NotImplemented, exception.Error.Condition);
            }
        }

        [TestMethod]
        public void ContainerHostPlainPrincipalTest()
        {
            string name = "ContainerHostPlainPrincipalTest";
            ListenerLink link = null;
            var linkProcessor = new TestLinkProcessor();
            linkProcessor.SetHandler(a => { link = a.Link; return false; });
            this.host.RegisterLinkProcessor(this.linkProcessor = linkProcessor);

            var connection = new Connection(Address);
            var session = new Session(connection);
            var sender = new SenderLink(session, name, name);
            sender.Send(new Message("msg1"), Timeout);
            connection.Close();

            Assert.IsTrue(link != null, "link is null");
            var listenerConnection = (ListenerConnection)link.Session.Connection;
            Assert.IsTrue(listenerConnection.Principal != null, "principal is null");
            Assert.IsTrue(listenerConnection.Principal.Identity.AuthenticationType == "PLAIN", "wrong auth type");
        }

        [TestMethod]
        public void ContainerHostCustomSaslMechanismTest()
        {
            string name = "ContainerHostCustomSaslMechanismTest";
            this.host.Listeners[0].SASL.EnableMechanism(name, SaslProfile.Anonymous);
            this.host.RegisterMessageProcessor(name, new TestMessageProcessor());

            var factory = new ConnectionFactory();
            factory.SASL.Profile = new CustomSaslProfile(name);
            var connection = factory.CreateAsync(new Address(Address.Host, Address.Port, null, null, "/", Address.Scheme)).Result;
            var session = new Session(connection);
            var sender = new SenderLink(session, name, name);
            sender.Send(new Message("msg1"), Timeout);
            connection.Close();
        }

        [TestMethod]
        public void ContainerHostSaslPlainNegativeTest()
        {
            var addressWithInvalidPassword = new Address("amqp://guest:invalid@localhost:15672");
            Trace.WriteLine(TraceLevel.Information, "sync test");
            {
                try
                {
                    var connection = new Connection(addressWithInvalidPassword);
                    Assert.IsTrue(false, "Exception not thrown");
                }
                catch (AmqpException ae)
                {
                    Assert.AreEqual(ErrorCode.UnauthorizedAccess, ae.Error.Condition.ToString());
                }
            }

            Trace.WriteLine(TraceLevel.Information, "async test");
            Task.Factory.StartNew(async () =>
            {
                try
                {
                    Connection connection = await Connection.Factory.CreateAsync(addressWithInvalidPassword);
                    Assert.IsTrue(false, "Exception not thrown");
                }
                catch (AmqpException ae)
                {
                    Assert.AreEqual(ErrorCode.UnauthorizedAccess, ae.Error.Condition.ToString());
                }
            }).Unwrap().GetAwaiter().GetResult();
        }

        [TestMethod]
        public void ContainerHostListenerSaslPlainNegativeTest()
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(this.Address.Host, this.Address.Port);
            var stream = new NetworkStream(socket);

            stream.Write(new byte[] { (byte)'A', (byte)'M', (byte)'Q', (byte)'P', 3, 1, 0, 0 }, 0, 8);
            TestListener.FRM(stream, 0x41, 3, 0, new Symbol("PLAIN"), Encoding.ASCII.GetBytes("guest\0invalid"));

            byte[] buffer = new byte[1024];
            int total = 0;
            int readSize = 0;
            for (int i = 0; i < 1000; i++)
            {
                readSize = socket.Receive(buffer, 0, buffer.Length, SocketFlags.None);
                if (readSize == 0)
                {
                    break;
                }

                total += readSize;
            }

            Assert.IsTrue(total > 0, "No response received from listener");
            Assert.AreEqual(0, readSize, "last read should be 0 as socket should be closed");
        }

        [TestMethod]
        public void ContainerHostX509PrincipalTest()
        {
            string name = "ContainerHostX509PrincipalTest";
            string address = "amqps://localhost:5676";
            X509Certificate2 cert = null;
            
            try
            {
                cert = GetCertificate(StoreLocation.LocalMachine, StoreName.My, "localhost");
            }
            catch (PlatformNotSupportedException)
            {
                // Unix machine, ignored
                return;
            }

            ContainerHost sslHost = new ContainerHost(new Address(address));
            sslHost.Listeners[0].SSL.Certificate = cert;
            sslHost.Listeners[0].SSL.ClientCertificateRequired = true;
            sslHost.Listeners[0].SSL.RemoteCertificateValidationCallback = (a, b, c, d) => true;
            sslHost.Listeners[0].SASL.EnableExternalMechanism = true;
            ListenerLink link = null;
            var linkProcessor = new TestLinkProcessor();
            linkProcessor.SetHandler(a => { link = a.Link; return false; });
            sslHost.RegisterLinkProcessor(linkProcessor);
            sslHost.Open();

            try
            {
                var factory = new ConnectionFactory();
                factory.SSL.RemoteCertificateValidationCallback = (a, b, c, d) => true;
                factory.SSL.ClientCertificates.Add(cert);
                factory.SASL.Profile = SaslProfile.External;
                var connection = factory.CreateAsync(new Address(address)).Result;
                var session = new Session(connection);
                var sender = new SenderLink(session, name, name);
                sender.Send(new Message("msg1"), Timeout);
                connection.Close();

                Assert.IsTrue(link != null, "link is null");
                var listenerConnection = (ListenerConnection)link.Session.Connection;
                Assert.IsTrue(listenerConnection.Principal != null, "principal is null");
                Assert.IsTrue(listenerConnection.Principal.Identity.AuthenticationType == "X509", "wrong auth type");

                X509Identity identity = (X509Identity)listenerConnection.Principal.Identity;
                Assert.IsTrue(identity.Certificate != null, "certificate is null");
            }
            finally
            {
                sslHost.Close();
            }
        }

        [TestMethod]
        public void InvalidAddressesTest()
        {
            var connection = new Connection(Address);
            var session = new Session(connection);

            try
            {
                var invalidAddresses = new string[] { null, "", "   " };
                for (int i = 0; i < invalidAddresses.Length; i++)
                {
                    var threw = false;
                    try
                    {
                        var sender = new SenderLink(session, "invalid-address-" + i, invalidAddresses[i]);
                        sender.Send(new Message("1"));
                    }
                    catch (AmqpException e)
                    {
                        Assert.AreEqual(ErrorCode.InvalidField, e.Error.Condition.ToString(),
                            string.Format("Address '{0}' did not cause an amqp exception with the expected error condition", invalidAddresses[i] ?? "null"));
                        threw = true;
                    }

                    Assert.IsTrue(threw, string.Format("Address '{0}' did not throw an amqp exception", invalidAddresses[i] ?? "null"));
                };
            }
            finally
            {
                session.Close();
                connection.Close();
            }
        }

        [TestMethod]
        public void ConnectionListenerCloseWithoutOpenTest()
        {
            ConnectionListener listener = new ConnectionListener("amqp://localhost:12345", null);
            listener.Close();
        }

        [TestMethod]
        public void ContainerHostCustomTransportTest()
        {
            string name = "ContainerHostCustomTransportTest";
            string address = "pipe://./" + name;
            ContainerHost host = new ContainerHost(new Address(address));
            host.CustomTransports.Add("pipe", NamedPipeTransport.Listener);
            host.RegisterMessageProcessor(name, new TestMessageProcessor());
            host.Open();

            try
            {
                var factory = new ConnectionFactory(new TransportProvider[] { NamedPipeTransport.Factory });
                var connection = factory.CreateAsync(new Address(address)).GetAwaiter().GetResult();
                var session = new Session(connection);
                var sender = new SenderLink(session, name, name);
                sender.Send(new Message("msg1"), Timeout);
                connection.Close();
            }
            finally
            {
                host.Close();
            }
        }

        [TestMethod]
        public void ContainerHostMessageProcessorUnregisterTest()
        {
            string name = "ContainerHostMessageProcessorUnregisterTest";
            var processor = new TestMessageProcessor();
            this.host.RegisterMessageProcessor(name, processor);

            int count = 5;
            var connection = new Connection(Address);
            var session = new Session(connection);
            var sender = new SenderLink(session, "send-link", name);

            for (int i = 0; i < count; i++)
            {
                var message = new Message("msg" + i);
                message.Properties = new Properties() { MessageId = name + i };
                sender.Send(message, Timeout);
            }

            this.host.UnregisterMessageProcessor(name);

            connection.Close();
        }

#if !NETFX40
        [TestMethod]
        public void LinkProcessorAsyncTest()
        {
            string name = "LinkProcessorAsyncTest";
            var processor = new TestLinkProcessor();
            processor.SetHandler(
                a =>
                {
                    Task.Delay(100).ContinueWith(_ => a.Complete(new TestLinkEndpoint(), 0));
                    return true;
                }
            );
            this.host.RegisterLinkProcessor(processor);

            int count = 5;
            var connection = new Connection(Address);
            var session = new Session(connection);
            var receiver = new ReceiverLink(session, "recv-link", name);
            for (int i = 0; i < count; i++)
            {
                var message = receiver.Receive(TimeSpan.FromSeconds(4));
                Assert.IsTrue(message != null);
                receiver.Accept(message);
            }

            connection.Close();
        }
#endif

#if !(DOTNET || NETFX40)
        [TestMethod]
        public void ContainerHostWebSocketWildCardAddressTest()
        {
            var host = new ContainerHost(new string[] { "ws://+:28080/test/" });
            host.Listeners[0].SASL.EnablePlainMechanism("guest", "guest");
            host.RegisterMessageProcessor("q1", new TestMessageProcessor());
            host.Open();

            try
            {
                var connection = Connection.Factory.CreateAsync(new Address("ws://guest:guest@localhost:28080/test/")).Result;
                var session = new Session(connection);
                var sender = new SenderLink(session, "ContainerHostWebSocketWildCardAddressTest", "q1");
                sender.Send(new Message("msg1"), Timeout);
                connection.Close();
            }
            catch
            {
                System.Diagnostics.Trace.WriteLine("If the test fails with System.Net.HttpListenerException (0x80004005): Access is denied");
                System.Diagnostics.Trace.WriteLine("Run the following command with admin privilege:");
                System.Diagnostics.Trace.WriteLine("netsh http add urlacl url=http://+:28080/test/ user=domain\\user");
                throw;
            }
            finally
            {
                host.Close();
            }
        }
#endif

        [TestMethod]
        public void EncodeDecodeMessageWithAmqpValueTest()
        {
            string name = "EncodeDecodeMessageWithAmqpValueTest";
            Queue<Message> messages = new Queue<Message>();
            messages.Enqueue(new Message("test") { Properties = new Properties() { MessageId = name } });

            var source = new TestMessageSource(messages);
            this.host.RegisterMessageSource(name, source);

            var connection = new Connection(Address);
            var session = new Session(connection);
            var receiver = new ReceiverLink(session, "receiver0", name);

            Message message = receiver.Receive();
            Message copy = Message.Decode(message.Encode());

            Assert.AreEqual((message.BodySection as AmqpValue).Value, (copy.BodySection as AmqpValue).Value);
        }

        public static X509Certificate2 GetCertificate(StoreLocation storeLocation, StoreName storeName, string certFindValue)
        {
            X509Store store = new X509Store(storeName, storeLocation);
            store.Open(OpenFlags.OpenExistingOnly);
            X509Certificate2Collection collection = store.Certificates.Find(
                X509FindType.FindBySubjectName,
                certFindValue,
                false);
            if (collection.Count == 0)
            {
                throw new ArgumentException("No certificate can be found using the find value " + certFindValue);
            }

#if DOTNET
            store.Dispose();
#else
            store.Close();
#endif
            return collection[0];
        }
    }

    class TestMessageProcessor : IMessageProcessor
    {
        public TestMessageProcessor()
            : this(20, new List<Message>())
        {
        }

        public TestMessageProcessor(int credit, List<Message> messages)
        {
            this.Credit = credit;
            this.Messages = messages;
        }

        public List<Message> Messages
        {
            get;
            private set;
        }

        public int Credit
        {
            get;
            private set;
        }

        public void Process(MessageContext messageContext)
        {
            if (this.Messages != null)
            {
                this.Messages.Add(messageContext.Message);
            }

            messageContext.Complete();
        }
    }

    class TestRequestProcessor : IRequestProcessor
    {
        int totalCount;

        public TestRequestProcessor()
        {
        }

        public int Credit
        {
            get { return 100; }
        }

        public int TotalCount
        {
            get { return this.totalCount; }
        }

        public void Process(RequestContext requestContext)
        {
            int id = Interlocked.Increment(ref this.totalCount);
            requestContext.Complete(new Message("OK" + id));
        }
    }

    class TestMessageSource : IMessageSource
    {
        readonly Queue<Message> messages;
        readonly List<Message> deadletterMessage;

        public TestMessageSource(Queue<Message> messages)
        {
            this.messages = messages;
            this.deadletterMessage = new List<Message>();
        }

        public IList<Message> DeadletterMessage
        {
            get { return this.deadletterMessage; }
        }

        public Task<ReceiveContext> GetMessageAsync(ListenerLink link)
        {
            lock (this.messages)
            {
                ReceiveContext context = null;
                if (this.messages.Count > 0)
                {
                    context = new ReceiveContext(link, this.messages.Dequeue());
                }

                var tcs = new TaskCompletionSource<ReceiveContext>();
                tcs.SetResult(context);
                return tcs.Task;
            }
        }

        public void DisposeMessage(ReceiveContext receiveContext, DispositionContext dispositionContext)
        {
            if (dispositionContext.DeliveryState is Rejected)
            {
                this.deadletterMessage.Add(receiveContext.Message);
            }
            else if (dispositionContext.DeliveryState is Released)
            {
                lock (this.messages)
                {
                    this.messages.Enqueue(receiveContext.Message);
                }
            }

            dispositionContext.Complete();
        }
    }

    class TestLinkProcessor : ILinkProcessor
    {
        Func<AttachContext, bool> attachHandler;
        readonly Func<ListenerLink, LinkEndpoint> factory;

        public TestLinkProcessor()
        {
        }

        public TestLinkProcessor(Func<ListenerLink, LinkEndpoint> factory)
        {
            this.factory = factory;
        }

        public void SetHandler(Func<AttachContext, bool> attachHandler)
        {
            this.attachHandler = attachHandler;
        }

        public void Process(AttachContext attachContext)
        {
            if (this.attachHandler != null)
            {
                if (this.attachHandler(attachContext))
                {
                    return;
                }
            }

            attachContext.Complete(
                this.factory != null ? this.factory(attachContext.Link) : new TestLinkEndpoint(),
                attachContext.Attach.Role ? 0 : 30);
        }
    }

    class TestLinkEndpoint : LinkEndpoint
    {
        public override void OnMessage(MessageContext messageContext)
        {
            messageContext.Complete();
        }

        public override void OnFlow(FlowContext flowContext)
        {
            for (int i = 0; i < flowContext.Messages; i++)
            {
                var message = new Message("test message");
                flowContext.Link.SendMessage(message, message.Encode());
            }
        }

        public override void OnDisposition(DispositionContext dispositionContext)
        {
            dispositionContext.Complete();
        }
    }

    sealed class CustomSaslProfile : SaslProfile
    {
        public CustomSaslProfile(string name)
            : base(name)
        {
        }

        protected override ITransport UpgradeTransport(ITransport transport)
        {
            return transport;
        }

        protected override DescribedList GetStartCommand(string hostname)
        {
            return new SaslInit()
            {
                Mechanism = this.Mechanism,
                InitialResponse = Encoding.UTF8.GetBytes($"{this.Mechanism}@contoso.com")
            };
        }

        protected override DescribedList OnCommand(DescribedList command)
        {
            return null;
        }
    }
}
