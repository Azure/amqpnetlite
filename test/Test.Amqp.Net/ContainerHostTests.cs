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
using System.Threading;
using Amqp;
using Amqp.Framing;
using Amqp.Listener;
using Amqp.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test.Amqp
{
    [TestClass]
    public class ContainerHostTests
    {
        // pick a port other than 5762 so that it doesn't conflict with the test broker
        const string Endpoint = "amqp://guest:guest@localhost:5765";
        const int SendTimeout = 5000;
        static readonly Uri Uri = new Uri(Endpoint);
        static readonly Address Address = new Address(Endpoint);
        ContainerHost host;

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            //Trace.TraceLevel = TraceLevel.Frame;
            //Trace.TraceListener = (f, a) => System.Diagnostics.Trace.WriteLine(DateTime.Now.ToString("[hh:ss.fff]") + " " + string.Format(f, a));
        }

        [TestInitialize]
        public void Initialize()
        {
            this.host = new ContainerHost(new List<Uri>() { Uri }, null, Uri.UserInfo);
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
            var sender2 = new SenderLink(session, linkName, name);

            sender1.Send(new Message("msg1"), SendTimeout);

            try
            {
                sender2.Send(new Message("msg1"), SendTimeout);
                Assert.IsTrue(false, "Excpected exception not thrown");
            }
            catch(AmqpException ae)
            {
                Assert.AreEqual((Symbol)ErrorCode.Stolen, ae.Error.Condition);
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
        public void Process(AttachContext attachContext)
        {
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
