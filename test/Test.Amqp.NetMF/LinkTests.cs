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
using Amqp;
using Amqp.Framing;
using Amqp.Types;
using System;
using System.Threading;
#if !(NETMF || COMPACT_FRAMEWORK)
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif

namespace Test.Amqp
{
#if !(NETMF || COMPACT_FRAMEWORK)
    [TestClass]
#endif
    public class LinkTests
    {
        public static Address address = new Address("amqp://guest:guest@localhost:5672");

#if !(NETMF || COMPACT_FRAMEWORK)
        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            Connection.DisableServerCertValidation = true;
            // uncomment the following to write frame traces
            //Trace.TraceLevel = TraceLevel.Frame;
            //Trace.TraceListener = (f, a) => System.Diagnostics.Trace.WriteLine(DateTime.Now.ToString("[hh:ss.fff]") + " " + string.Format(f, a));
        }

        [TestMethod]
#endif
        public void TestMethod_BasicSendReceive()
        {
            Connection connection = new Connection(address);
            Session session = new Session(connection);
            SenderLink sender = new SenderLink(session, "send-link", "q1");

            for (int i = 0; i < 200; ++i)
            {
                Message message = new Message();
                message.Properties = new Properties() { GroupId = "abcdefg" };
                message.ApplicationProperties = new ApplicationProperties();
                message.ApplicationProperties["sn"] = i;
                sender.Send(message, null, null);
            }

            ReceiverLink receiver = new ReceiverLink(session, "receive-link", "q1");
            for (int i = 0; i < 200; ++i)
            {
                if (i % 50 == 0) receiver.SetCredit(50);
                Message message = receiver.Receive();
                Trace.WriteLine(TraceLevel.Information, "receive: {0}", message.ApplicationProperties["sn"]);
                receiver.Accept(message);
            }

            sender.Close();
            receiver.Close();
            session.Close();
            connection.Close();
        }

#if !(NETMF || COMPACT_FRAMEWORK)
        [TestMethod]
#endif
        public void TestMethod_OnMessage()
        {
            Connection connection = new Connection(address);
            Session session = new Session(connection);

            ReceiverLink receiver = new ReceiverLink(session, "receive-link", "q1");
            ManualResetEvent done = new ManualResetEvent(false);
            receiver.Start(200, (link, m) =>
                {
                    Trace.WriteLine(TraceLevel.Information, "receive: {0}", m.ApplicationProperties["sn"]);
                    link.Accept(m);
                    if ((int)m.ApplicationProperties["sn"] == 199)
                    {
                        done.Set();
                    }
                });

            SenderLink sender = new SenderLink(session, "send-link", "q1");
            for (int i = 0; i < 200; ++i)
            {
                Message message = new Message();
                message.Properties = new Properties() { GroupId = "abcdefg" };
                message.ApplicationProperties = new ApplicationProperties();
                message.ApplicationProperties["sn"] = i;
                sender.Send(message, null, null);
            }

#if !COMPACT_FRAMEWORK
            done.WaitOne(10000, true);
#else
            done.WaitOne(10000, false);
#endif

            sender.Close();
            receiver.Close();
            session.Close();
            connection.Close();
        }

#if !(NETMF || COMPACT_FRAMEWORK)
        [TestMethod]
#endif
        public void TestMethod_SendAck()
        {
            Connection connection = new Connection(address);
            Session session = new Session(connection);

            SenderLink sender = new SenderLink(session, "send-link", "q1");
            ManualResetEvent done = new ManualResetEvent(false);
            OutcomeCallback callback = (m, o, s) =>
            {
                Trace.WriteLine(TraceLevel.Information, "send complete: sn {0} outcome {1}", m.ApplicationProperties["sn"], o.Descriptor.Name);
                if ((int)m.ApplicationProperties["sn"] == 199)
                {
                    done.Set();
                }
            };

            for (int i = 0; i < 200; ++i)
            {
                Message message = new Message();
                message.Properties = new Properties() { GroupId = "abcdefg" };
                message.ApplicationProperties = new ApplicationProperties();
                message.ApplicationProperties["sn"] = i;
                sender.Send(message, callback, null);
            }

#if !COMPACT_FRAMEWORK
            done.WaitOne(10000, true);
#else
            done.WaitOne(10000, false);
#endif

            ReceiverLink receiver = new ReceiverLink(session, "receive-link", "q1");
            for (int i = 0; i < 200; ++i)
            {
                if (i % 100 == 0) receiver.SetCredit(100);
                Message message = receiver.Receive();
                Trace.WriteLine(TraceLevel.Information, "receive: {0}", message.ApplicationProperties["sn"]);
                receiver.Accept(message);
            }
            
            sender.Close();
            receiver.Close();
            session.Close();
            connection.Close();
        }

#if !(NETMF || COMPACT_FRAMEWORK)
        [TestMethod]
#endif
        public void TestMethod_ReceiveWaiter()
        {
            Connection connection = new Connection(address);
            Session session = new Session(connection);

            ReceiverLink receiver = new ReceiverLink(session, "receive-link", "q1");
            Thread t = new Thread(() =>
            {
                receiver.SetCredit(1);
                Message message = receiver.Receive();
                Trace.WriteLine(TraceLevel.Information, "receive: {0}", message.Properties.MessageId);
                receiver.Accept(message);
            });

            t.Start();

            SenderLink sender = new SenderLink(session, "send-link", "q1");
            Message msg = new Message() { Properties = new Properties() { MessageId = "123456" } };
            sender.Send(msg, null, null);

            t.Join(10000);

            sender.Close();
            receiver.Close();
            session.Close();
            connection.Close();
        }

#if !(NETMF || COMPACT_FRAMEWORK)
        [TestMethod]
#endif
        public void TestMethod_ReceiveWithFilterTest()
        {
            Connection connection = new Connection(address);
            Session session = new Session(connection);

            Message message = new Message("I can match a filter");
            message.Properties = new Properties() { GroupId = "abcdefg" };
            message.ApplicationProperties = new ApplicationProperties();
            message.ApplicationProperties["sn"] = 100;

            SenderLink sender = new SenderLink(session, "send-link", "q1");
            sender.Send(message, null, null);

            // update the filter descriptor and expression according to the broker
            Map filters = new Map();
            // JMS selector filter: code = 0x0000468C00000004L, symbol="apache.org:selector-filter:string"
            filters.Add(new Symbol("f1"), new DescribedValue(new Symbol("apache.org:selector-filter:string"), "sn = 100"));
            ReceiverLink receiver = new ReceiverLink(session, "receive-link", new Source() { Address = "q1", FilterSet = filters });
            receiver.SetCredit(10);
            Message message2 = receiver.Receive();
            receiver.Accept(message2);

            sender.Close();
            receiver.Close();
            session.Close();
            connection.Close();
        }

#if !(NETMF || COMPACT_FRAMEWORK)
        [TestMethod]
#endif
        public void TestMethod_LinkCloseWithPendingSendTest()
        {
            Connection connection = new Connection(address);
            Session session = new Session(connection);
            SenderLink sender = new SenderLink(session, "send-link", "q1");

            bool cancelled = false;
            Message message = new Message("released");
            sender.Send(message, (m, o, s) => cancelled = true, null);
            sender.Close(0);

            // assume that Close is called before connection/link is open so message is still queued in link
            // but this is not very reliable, so just do a best effort check
            if (cancelled)
            {
                Trace.WriteLine(TraceLevel.Information, "The send was cancelled as expected");
            }
            else
            {
                Trace.WriteLine(TraceLevel.Information, "The send was not cancelled as expected. This can happen if close call loses the race");
            }

            try
            {
                message = new Message("failed");
                sender.Send(message, (m, o, s) => cancelled = true, null);
                Assert.IsTrue(false, "Send should fail after link is closed");
            }
            catch (AmqpException exception)
            {
                Trace.WriteLine(TraceLevel.Information, "Caught exception: ", exception.Error);
            }

            session.Close();
            connection.Close();
        }

#if !(NETMF || COMPACT_FRAMEWORK)
        [TestMethod]
#endif
        public void TestMethod_SynchronousSendTest()
        {
            Connection connection = new Connection(address);
            Session session = new Session(connection);
            SenderLink sender = new SenderLink(session, "sync-send-link", "q1");
            Message message = new Message("hello");
            sender.Send(message, 60000);

            ReceiverLink receiver = new ReceiverLink(session, "receive-link", "q1");
            receiver.SetCredit(10);
            message = receiver.Receive();
            Assert.IsTrue(message != null, "no message was received.");

            sender.Close();
            receiver.Close();
            session.Close();
            connection.Close();
        }

#if !(NETMF || COMPACT_FRAMEWORK)
        [TestMethod]
#endif
        public void TestMethod_DynamicSenderLinkTest()
        {
            Connection connection = new Connection(address);
            Session session = new Session(connection);

            Target remoteTarget = null;
            SenderLink sender = new SenderLink(session, "sync-send-link", new Target() { Dynamic = true }, (l, t, s) => remoteTarget = t);
            Message message = new Message("hello");
            sender.Send(message, 60000);

            Assert.IsTrue(remoteTarget != null, "dynamic target not attached");
            ReceiverLink receiver = new ReceiverLink(session, "receive-link", remoteTarget.Address);
            receiver.SetCredit(10);
            message = receiver.Receive();
            Assert.IsTrue(message != null, "no message was received.");

            sender.Close();
            receiver.Close();
            session.Close();
            connection.Close();
        }

#if !(NETMF || COMPACT_FRAMEWORK)
        [TestMethod]
#endif
        public void TestMethod_DynamicReceiverLinkTest()
        {
            Connection connection = new Connection(address);
            Session session = new Session(connection);

            Source remoteSource = null;
            ManualResetEvent attached = new ManualResetEvent(false);
            OnAttached onAttached = (l, t, s) => { remoteSource = s; attached.Set(); };
            ReceiverLink receiver = new ReceiverLink(session, "dynamic-receive-link", new Source() { Dynamic = true }, onAttached);
#if !COMPACT_FRAMEWORK
            attached.WaitOne(10000, true);
#else
            attached.WaitOne(10000, false);
#endif

            Assert.IsTrue(remoteSource != null, "dynamic source not attached");

            SenderLink sender = new SenderLink(session, "send-link", remoteSource.Address);
            Message message = new Message("hello");
            sender.Send(message, 60000);

            receiver.SetCredit(10);
            message = receiver.Receive();
            Assert.IsTrue(message != null, "no message was received.");

            sender.Close();
            receiver.Close();
            session.Close();
            connection.Close();
        }

#if !(NETMF || COMPACT_FRAMEWORK)
        [TestMethod]
#endif
        public void TestMethod_RequestResponseTest()
        {
            Connection connection = new Connection(address);
            Session session = new Session(connection);

            // server app: the request handler
            ReceiverLink requestLink = new ReceiverLink(session, "server-request-link", "q1");
            requestLink.Start(10, (l, m) =>
                {
                    l.Accept(m);

                    // got a request, send back a reply
                    SenderLink sender = new SenderLink(session, "server-reply-link", m.Properties.ReplyTo);
                    Message reply = new Message("received");
                    reply.Properties = new Properties() { CorrelationId = m.Properties.MessageId };
                    sender.Send(reply, (a, b, c) => ((Link)c).Close(0), sender);
                });

            // client: setup a temp queue and waits for responses
            OnAttached onAttached = (l, t, s) =>
                {
                    // client: sends a request to the request queue, specifies the temp queue as the reply queue
                    SenderLink sender = new SenderLink(session, "client-request-link", "q1");
                    Message request = new Message("hello");
                    request.Properties = new Properties() { MessageId = "request1", ReplyTo = s.Address };
                    sender.Send(request, (a, b, c) => ((Link)c).Close(0), sender);
                };
            ReceiverLink responseLink = new ReceiverLink(session, "dynamic-response-link", new Source() { Dynamic = true }, onAttached);
            responseLink.SetCredit(10);
            Message response = responseLink.Receive();
            Assert.IsTrue(response != null, "no response was received");

            requestLink.Close();
            responseLink.Close();
            session.Close();
            connection.Close();
        }
    }
}
