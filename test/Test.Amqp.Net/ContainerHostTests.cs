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
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Amqp;
using Amqp.Framing;
using Amqp.Listener;
using Amqp.Sasl;
using Amqp.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test.Amqp
{
    [TestClass]
    public class ContainerHostTests
    {
        const int SendTimeout = 5000;
        ContainerHost host;

        public Uri Uri
        {
            get;
            set;
        }

        Address Address
        {
            get { return new Address(this.Uri.AbsoluteUri); }
        }

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            //Trace.TraceLevel = TraceLevel.Frame;
            //Trace.TraceListener = (f, a) => System.Diagnostics.Trace.WriteLine(DateTime.Now.ToString("[hh:ss.fff]") + " " + string.Format(f, a));
        }

        [TestInitialize]
        public void Initialize()
        {
            // pick a port other than 5762 so that it doesn't conflict with the test broker
            this.Uri = new Uri("amqp://guest:guest@localhost:5765");

            this.host = new ContainerHost(new List<Uri>() { this.Uri }, null, this.Uri.UserInfo);
            this.host.Listeners[0].SASL.EnableExternalMechanism = true;
            this.host.Open();
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (this.host != null)
            {
                this.host.Close();
            }
        }

        [TestMethod]
        public void ContainerHostMessageProcessorTest()
        {
            string name = MethodInfo.GetCurrentMethod().Name;
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
                sender.Send(message, SendTimeout);
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
        public void ContainerHostRequestProcessorTest()
        {
            string name = MethodInfo.GetCurrentMethod().Name;
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
                sender.Send(request, SendTimeout);
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
            string name = MethodInfo.GetCurrentMethod().Name;
            this.host.RegisterLinkProcessor(new TestLinkProcessor());

            int count = 80;
            var connection = new Connection(Address);
            var session = new Session(connection);
            var sender = new SenderLink(session, "send-link", "any");

            for (int i = 0; i < count; i++)
            {
                var message = new Message("msg" + i);
                message.Properties = new Properties() { GroupId = name };
                sender.Send(message, SendTimeout);
            }

            sender.Close();
            session.Close();
            connection.Close();
        }

        [TestMethod]
        public void ContainerHostProcessorOrderTest()
        {
            string name = MethodInfo.GetCurrentMethod().Name;
            this.host.RegisterMessageProcessor(name, new TestMessageProcessor());
            this.host.RegisterLinkProcessor(new TestLinkProcessor());

            int count = 80;
            var connection = new Connection(Address);
            var session = new Session(connection);
            var sender = new SenderLink(session, "send-link", name);

            for (int i = 0; i < count; i++)
            {
                var message = new Message("msg" + i);
                message.Properties = new Properties() { GroupId = name };
                sender.Send(message, SendTimeout);
            }

            sender.Close();

            sender = new SenderLink(session, "send-link", "any");
            for (int i = 0; i < count; i++)
            {
                var message = new Message("msg" + i);
                message.Properties = new Properties() { GroupId = name };
                sender.Send(message, SendTimeout);
            }

            sender.Close();
            session.Close();
            connection.Close();
        }

        [TestMethod]
        public void ContainerHostUnknownProcessorTest()
        {
            string name = MethodInfo.GetCurrentMethod().Name;
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
        public void ContainerHostDefaultValueTest()
        {
            string name = MethodInfo.GetCurrentMethod().Name;
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
            string name = MethodInfo.GetCurrentMethod().Name;
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
                            (m, o, s) => { if (Interlocked.Increment(ref completed) >= 10 * 20 * 30) doneEvent.Set(); },
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
            string name = MethodInfo.GetCurrentMethod().Name;
            this.host.RegisterMessageProcessor(name, new TestMessageProcessor());

            //Create a client to send data to the host message processor
            var closedEvent = new ManualResetEvent(false);
            var connection = new Connection(Address);
            connection.Closed += (AmqpObject obj, Error error) =>
            {
                closedEvent.Set();
            };

            var session = new Session(connection);
            var sender = new SenderLink(session, "sender-link", name);

            //Send one message while the host is open
            sender.Send(new Message("Hello"), SendTimeout);

            //Close the host. this should close existing connections
            this.host.Close();

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
            this.host = new ContainerHost(new List<Uri>() { Uri }, null, Uri.UserInfo);
            this.host.RegisterMessageProcessor(name, new TestMessageProcessor());
            this.host.Open();

            connection = new Connection(Address);
            session = new Session(connection);
            sender = new SenderLink(session, "sender-link", name);
            sender.Send(new Message("Hello"), SendTimeout);
            connection.Close();
        }

        [TestMethod]
        public void ContainerHostSessionFlowControlTest()
        {
            string name = MethodInfo.GetCurrentMethod().Name;
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
                sender.Send(message, SendTimeout);
            }

            sender.Close();
            session.Close();
            connection.Close();
        }

        [TestMethod]
        public void DuplicateLinkNameSameSessionTest()
        {
            string name = MethodInfo.GetCurrentMethod().Name;
            this.host.RegisterMessageProcessor(name, new TestMessageProcessor(500000, null));

            string linkName = "same-send-link";
            var connection = new Connection(Address);
            var session = new Session(connection);
            var sender1 = new SenderLink(session, linkName, name);
            sender1.Send(new Message("msg1"), SendTimeout);

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
            string name = MethodInfo.GetCurrentMethod().Name;
            this.host.RegisterMessageProcessor(name, new TestMessageProcessor(500000, null));

            string linkName = "same-send-link";
            var connection = new Connection(Address);

            var session1 = new Session(connection);
            var sender1 = new SenderLink(session1, linkName, name);

            var session2 = new Session(connection);
            var sender2 = new SenderLink(session2, linkName, name);

            sender1.Send(new Message("msg1"), SendTimeout);

            try
            {
                sender2.Send(new Message("msg1"), SendTimeout);
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
            string name = MethodInfo.GetCurrentMethod().Name;
            this.host.RegisterMessageProcessor(name, new TestMessageProcessor(500000, null));

            string linkName = "same-send-link";
            var connection1 = new Connection(Address);
            var session1 = new Session(connection1);
            var sender1 = new SenderLink(session1, linkName, name);

            var connection2 = new Connection(Address);
            var session2 = new Session(connection2);
            var sender2 = new SenderLink(session2, linkName, name);

            sender1.Send(new Message("msg1"), SendTimeout);
            sender1.Send(new Message("msg1"), SendTimeout);

            connection1.Close();
            connection2.Close();
        }

        [TestMethod]
        public void DuplicateLinkNameDifferentRoleTest()
        {
            string name = MethodInfo.GetCurrentMethod().Name;
            this.host.RegisterLinkProcessor(new TestLinkProcessor());

            string linkName = "same-link-for-different-role";
            var connection = new Connection(Address);
            var session1 = new Session(connection);
            var sender = new SenderLink(session1, linkName, name);
            sender.Send(new Message("msg1"), SendTimeout);

            var session2 = new Session(connection);
            var receiver = new ReceiverLink(session2, linkName, name);
            receiver.SetCredit(2, false);
            var message = receiver.Receive();
            Assert.IsTrue(message != null, "No message was received");
            receiver.Accept(message);

            connection.Close();
        }

        [TestMethod]
        public void ContainerHostPlainPrincipalTest()
        {
            string name = MethodInfo.GetCurrentMethod().Name;
            ListenerLink link = null;
            var linkProcessor = new TestLinkProcessor();
            linkProcessor.OnLinkAttached += a => link = a;
            this.host.RegisterLinkProcessor(linkProcessor);

            var connection = new Connection(Address);
            var session = new Session(connection);
            var sender = new SenderLink(session, name, name);
            sender.Send(new Message("msg1"), SendTimeout);
            connection.Close();

            Assert.IsTrue(link != null, "link is null");
            var listenerConnection = (ListenerConnection)link.Session.Connection;
            Assert.IsTrue(listenerConnection.Principal != null, "principal is null");
            Assert.IsTrue(listenerConnection.Principal.Identity.AuthenticationType == "PLAIN", "wrong auth type");
        }

        [TestMethod]
        public void ContainerHostX509PrincipalTest()
        {
            string name = MethodInfo.GetCurrentMethod().Name;
            string address = "amqps://localhost:5676";
            X509Certificate2 cert = GetCertificate(StoreLocation.LocalMachine, StoreName.My, "localhost");
            ContainerHost sslHost = new ContainerHost(new Uri(address));
            sslHost.Listeners[0].SSL.Certificate = cert;
            sslHost.Listeners[0].SSL.ClientCertificateRequired = true;
            sslHost.Listeners[0].SSL.RemoteCertificateValidationCallback = (a, b, c, d) => true;
            sslHost.Listeners[0].SASL.EnableExternalMechanism = true;
            ListenerLink link = null;
            var linkProcessor = new TestLinkProcessor();
            linkProcessor.OnLinkAttached += a => link = a;
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
                sender.Send(new Message("msg1"), SendTimeout);
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
        public void InvalidAddresses()
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

        static X509Certificate2 GetCertificate(StoreLocation storeLocation, StoreName storeName, string certFindValue)
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

            store.Close();
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

    class TestLinkProcessor : ILinkProcessor
    {
        public Action<ListenerLink> OnLinkAttached;

        public void Process(AttachContext attachContext)
        {
            if (this.OnLinkAttached != null)
            {
                this.OnLinkAttached(attachContext.Link);
            }

            attachContext.Complete(new TestLinkEndpoint(), attachContext.Attach.Role ? 0 : 30);
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
    }
}
