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
using Amqp.Sasl;
using Amqp.Types;
using System;
using System.Text;
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

#if !COMPACT_FRAMEWORK
        bool waitExitContext = true;
#else
        bool waitExitContext = false;
#endif

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
            string testName = "BasicSendReceive";
            const int nMsgs = 200;
            Connection connection = new Connection(address);
            Session session = new Session(connection);
            SenderLink sender = new SenderLink(session, "sender-" + testName, "q1");

            for (int i = 0; i < nMsgs; ++i)
            {
                Message message = new Message("msg" + i);
                message.Properties = new Properties() { GroupId = "abcdefg" };
                message.ApplicationProperties = new ApplicationProperties();
                message.ApplicationProperties["sn"] = i;
                sender.Send(message, null, null);
            }

            ReceiverLink receiver = new ReceiverLink(session, "receiver-" + testName, "q1");
            for (int i = 0; i < nMsgs; ++i)
            {
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
        public void TestMethod_ConnectionFrameSize()
        {
            string testName = "ConnectionFrameSize";
            const int nMsgs = 200;
            Connection connection = new Connection(address);
            Session session = new Session(connection);
            SenderLink sender = new SenderLink(session, "sender-" + testName, "q1");

            int frameSize = 16 * 1024;
            for (int i = 0; i < nMsgs; ++i)
            {
                Message message = new Message(new string('A', frameSize + (i - nMsgs / 2)));
                sender.Send(message, null, null);
            }

            ReceiverLink receiver = new ReceiverLink(session, "receiver-" + testName, "q1");
            for (int i = 0; i < nMsgs; ++i)
            {
                Message message = receiver.Receive();
                string value = (string)message.Body;
                Trace.WriteLine(TraceLevel.Information, "receive: {0}x{1}", value[0], value.Length);
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
        public void TestMethod_ConnectionChannelMax()
        {
            ushort channelMax = 5;
            Connection connection = new Connection(
                address,
                null,
                new Open() { ContainerId = "ConnectionChannelMax", HostName = address.Host, ChannelMax = channelMax },
                (c, o) => Trace.WriteLine(TraceLevel.Information, "{0}", o));

            for (int i = 0; i <= channelMax; i++)
            {
                Session session = new Session(connection);
            }

            try
            {
                Session session = new Session(connection);
                Fx.Assert(false, "Created more sessions than allowed.");
            }
            catch (AmqpException exception)
            {
                Fx.Assert(exception.Error.Condition.Equals((Symbol)ErrorCode.NotAllowed), "Wrong error code");
            }

            connection.Close();
        }

#if !(NETMF || COMPACT_FRAMEWORK)
        [TestMethod]
#endif
        public void TestMethod_ConnectionRemoteProperties()
        {
            string testName = "ConnectionRemoteProperties";            
            ManualResetEvent opened = new ManualResetEvent(false);
            Open remoteOpen = null;
            OnOpened onOpen = (c, o) =>
            {
                remoteOpen = o;
                opened.Set();
            };

            Open open = new Open()
            {
                ContainerId = testName,
                Properties = new Fields() { { new Symbol("p1"), "abcd" } },
                DesiredCapabilities = new Symbol[] { new Symbol("dc1"), new Symbol("dc2") },
                OfferedCapabilities = new Symbol[] { new Symbol("oc") }
            };

            Connection connection = new Connection(address, null, open, onOpen);

            opened.WaitOne(10000, waitExitContext);

            connection.Close();

            Assert.IsTrue(remoteOpen != null, "remote open not set");
        }

#if !(NETMF || COMPACT_FRAMEWORK)
        [TestMethod]
#endif
        public void TestMethod_OnMessage()
        {
            string testName = "OnMessage";
            const int nMsgs = 200;
            Connection connection = new Connection(address);
            Session session = new Session(connection);

            ReceiverLink receiver = new ReceiverLink(session, "receiver-" + testName, "q1");
            ManualResetEvent done = new ManualResetEvent(false);
            int received = 0;
            receiver.Start(10, (link, m) =>
                {
                    Trace.WriteLine(TraceLevel.Information, "receive: {0}", m.ApplicationProperties["sn"]);
                    link.Accept(m);
                    received++;
                    if (received == nMsgs)
                    {
                        done.Set();
                    }
                });

            SenderLink sender = new SenderLink(session, "sender-" + testName, "q1");
            for (int i = 0; i < nMsgs; ++i)
            {
                Message message = new Message()
                {
                    BodySection = new Data() { Binary = Encoding.UTF8.GetBytes("msg" + i) }
                };
                message.Properties = new Properties() { MessageId = "msg" + i, GroupId = testName };
                message.ApplicationProperties = new ApplicationProperties();
                message.ApplicationProperties["sn"] = i;
                sender.Send(message, null, null);
            }

            int last = -1;
            while (!done.WaitOne(10000, waitExitContext) && received > last)
            {
                last = received;
            }

            sender.Close();
            receiver.Close();
            session.Close();
            connection.Close();

            Assert.AreEqual(nMsgs, received, "not all messages are received.");
        }

#if !(NETMF || COMPACT_FRAMEWORK)
        [TestMethod]
#endif
        public void TestMethod_CloseBusyReceiver()
        {
            string testName = "CloseBusyReceiver";
            const int nMsgs = 20;
            Connection connection = new Connection(address);
            Session session = new Session(connection);

            SenderLink sender = new SenderLink(session, "sender-" + testName, "q1");
            for (int i = 0; i < nMsgs; ++i)
            {
                Message message = new Message();
                message.Properties = new Properties() { MessageId = "msg" + i };
                message.ApplicationProperties = new ApplicationProperties();
                message.ApplicationProperties["sn"] = i;
                sender.Send(message, null, null);
            }

            ReceiverLink receiver = new ReceiverLink(session, "receiver-" + testName, "q1");
            ManualResetEvent closed = new ManualResetEvent(false);
            receiver.Closed += (o, e) => closed.Set();
            receiver.Start(
                nMsgs,
                (r, m) =>
                {
                    if (m.Properties.MessageId == "msg0") r.Close(0);
                });
            Assert.IsTrue(closed.WaitOne(10000, waitExitContext));

            ReceiverLink receiver2 = new ReceiverLink(session, "receiver2-" + testName, "q1");
            for (int i = 0; i < nMsgs; ++i)
            {
                Message message = receiver2.Receive();
                Trace.WriteLine(TraceLevel.Information, "receive: {0}", message.Properties.MessageId);
                receiver2.Accept(message);
            }

            receiver2.Close();
            sender.Close();
            session.Close();
            connection.Close();
        }

#if !(NETMF || COMPACT_FRAMEWORK)
        [TestMethod]
#endif
        public void TestMethod_ReleaseMessage()
        {
            string testName = "ReleaseMessage";
            const int nMsgs = 20;
            Connection connection = new Connection(address);
            Session session = new Session(connection);

            SenderLink sender = new SenderLink(session, "sender-" + testName, "q1");
            for (int i = 0; i < nMsgs; ++i)
            {
                Message message = new Message();
                message.Properties = new Properties() { MessageId = "msg" + i };
                message.ApplicationProperties = new ApplicationProperties();
                message.ApplicationProperties["sn"] = i;
                sender.Send(message, null, null);
            }

            ReceiverLink receiver = new ReceiverLink(session, "receiver-" + testName, "q1");
            for (int i = 0; i < nMsgs; ++i)
            {
                Message message = receiver.Receive();
                Trace.WriteLine(TraceLevel.Information, "receive: {0}", message.Properties.MessageId);
                if (i % 2 == 0)
                {
                    receiver.Accept(message);
                }
                else
                {
                    receiver.Release(message);
                }
            }
            receiver.Close();

            ReceiverLink receiver2 = new ReceiverLink(session, "receiver2-" + testName, "q1");
            for (int i = 0; i < nMsgs / 2; ++i)
            {
                Message message = receiver2.Receive();
                Trace.WriteLine(TraceLevel.Information, "receive: {0}", message.Properties.MessageId);
                receiver2.Accept(message);
            }

            receiver2.Close();
            sender.Close();
            session.Close();
            connection.Close();
        }

#if !(NETMF || COMPACT_FRAMEWORK)
        [TestMethod]
#endif
        public void TestMethod_SendAck()
        {
            string testName = "SendAck";
            const int nMsgs = 20;
            Connection connection = new Connection(address);
            Session session = new Session(connection);

            SenderLink sender = new SenderLink(session, "sender-" + testName, "q1");
            ManualResetEvent done = new ManualResetEvent(false);
            OutcomeCallback callback = (m, o, s) =>
            {
                Trace.WriteLine(TraceLevel.Information, "send complete: sn {0} outcome {1}", m.ApplicationProperties["sn"], o.Descriptor.Name);
                if ((int)m.ApplicationProperties["sn"] == (nMsgs - 1))
                {
                    done.Set();
                }
            };

            for (int i = 0; i < nMsgs; ++i)
            {
                Message message = new Message();
                message.Properties = new Properties() { MessageId = "msg" + i, GroupId = testName };
                message.ApplicationProperties = new ApplicationProperties();
                message.ApplicationProperties["sn"] = i;
                sender.Send(message, callback, null);
            }

            done.WaitOne(10000, waitExitContext);

            ReceiverLink receiver = new ReceiverLink(session, "receiver-" + testName, "q1");
            for (int i = 0; i < nMsgs; ++i)
            {
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
            string testName = "ReceiveWaiter";
            Connection connection = new Connection(address);
            Session session = new Session(connection);

            ReceiverLink receiver = new ReceiverLink(session, "receiver-" + testName, "q1");
            Thread t = new Thread(() =>
            {
                Message message = receiver.Receive();
                Trace.WriteLine(TraceLevel.Information, "receive: {0}", message.Properties.MessageId);
                receiver.Accept(message);
            });

            t.Start();

            SenderLink sender = new SenderLink(session, "sender-" + testName, "q1");
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
        public void TestMethod_ReceiveWithFilter()
        {
            string testName = "ReceiveWithFilter";
            Connection connection = new Connection(address);
            Session session = new Session(connection);

            Message message = new Message("I can match a filter");
            message.Properties = new Properties() { GroupId = "abcdefg" };
            message.ApplicationProperties = new ApplicationProperties();
            message.ApplicationProperties["sn"] = 100;

            SenderLink sender = new SenderLink(session, "sender-" + testName, "q1");
            sender.Send(message, null, null);

            // update the filter descriptor and expression according to the broker
            Map filters = new Map();
            // JMS selector filter: code = 0x0000468C00000004L, symbol="apache.org:selector-filter:string"
            filters.Add(new Symbol("f1"), new DescribedValue(new Symbol("apache.org:selector-filter:string"), "sn = 100"));
            ReceiverLink receiver = new ReceiverLink(session, "receiver-" + testName, new Source() { Address = "q1", FilterSet = filters }, null);
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
        public void TestMethod_LinkCloseWithPendingSend()
        {
            string testName = "LinkCloseWithPendingSend";
            Connection connection = new Connection(address);
            Session session = new Session(connection);
            SenderLink sender = new SenderLink(session, "sender-" + testName, "q1");

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
        public void TestMethod_SynchronousSend()
        {
            string testName = "SynchronousSend";
            Connection connection = new Connection(address);
            Session session = new Session(connection);
            SenderLink sender = new SenderLink(session, "sender-" + testName, "q1");
            Message message = new Message("hello");
            sender.Send(message, 60000);

            ReceiverLink receiver = new ReceiverLink(session, "receiver-" + testName, "q1");
            message = receiver.Receive();
            Assert.IsTrue(message != null, "no message was received.");
            receiver.Accept(message);

            sender.Close();
            receiver.Close();
            session.Close();
            connection.Close();
        }

#if !(NETMF || COMPACT_FRAMEWORK)
        [TestMethod]
#endif
        public void TestMethod_DynamicSenderLink()
        {
            string testName = "DynamicSenderLink";
            Connection connection = new Connection(address);
            Session session = new Session(connection);

            string targetAddress = null;
            OnAttached onAttached = (link, attach) =>
            {
                targetAddress = ((Target)attach.Target).Address;
            };

            SenderLink sender = new SenderLink(session, "sender-" + testName, new Target() { Dynamic = true }, onAttached);
            Message message = new Message("hello");
            sender.Send(message, 60000);

            Assert.IsTrue(targetAddress != null, "dynamic target not attached");
            ReceiverLink receiver = new ReceiverLink(session, "receiver-" + testName, targetAddress);
            message = receiver.Receive();
            Assert.IsTrue(message != null, "no message was received.");
            receiver.Accept(message);

            sender.Close();
            receiver.Close();
            session.Close();
            connection.Close();
        }

#if !(NETMF || COMPACT_FRAMEWORK)
        [TestMethod]
#endif
        public void TestMethod_DynamicReceiverLink()
        {
            string testName = "DynamicReceiverLink";
            Connection connection = new Connection(address);
            Session session = new Session(connection);

            string remoteSource = null;
            ManualResetEvent attached = new ManualResetEvent(false);
            OnAttached onAttached = (link, attach) => { remoteSource = ((Source)attach.Source).Address; attached.Set(); };
            ReceiverLink receiver = new ReceiverLink(session, "receiver-" + testName, new Source() { Dynamic = true }, onAttached);

            attached.WaitOne(10000, waitExitContext);

            Assert.IsTrue(remoteSource != null, "dynamic source not attached");

            SenderLink sender = new SenderLink(session, "sender-" + testName, remoteSource);
            Message message = new Message("hello");
            sender.Send(message, 60000);

            message = receiver.Receive();
            Assert.IsTrue(message != null, "no message was received.");
            receiver.Accept(message);

            sender.Close();
            receiver.Close();
            session.Close();
            connection.Close();
        }

#if !(NETMF || COMPACT_FRAMEWORK)
        [TestMethod]
#endif
        public void TestMethod_RequestResponse()
        {
            string testName = "RequestResponse";
            Connection connection = new Connection(address);
            Session session = new Session(connection);

            // server app: the request handler
            ReceiverLink requestLink = new ReceiverLink(session, "srv.requester-" + testName, "q1");
            requestLink.Start(10, (l, m) =>
                {
                    l.Accept(m);

                    // got a request, send back a reply
                    SenderLink sender = new SenderLink(session, "srv.replier-" + testName, m.Properties.ReplyTo);
                    Message reply = new Message("received");
                    reply.Properties = new Properties() { CorrelationId = m.Properties.MessageId };
                    sender.Send(reply, (a, b, c) => ((Link)c).Close(0), sender);
                });

            // client: setup a temp queue and waits for responses
            OnAttached onAttached = (l, at) =>
                {
                    // client: sends a request to the request queue, specifies the temp queue as the reply queue
                    SenderLink sender = new SenderLink(session, "cli.requester-" + testName, "q1");
                    Message request = new Message("hello");
                    request.Properties = new Properties() { MessageId = "request1", ReplyTo = ((Source)at.Source).Address };
                    sender.Send(request, (a, b, c) => ((Link)c).Close(0), sender);
                };
            ReceiverLink responseLink = new ReceiverLink(session, "cli.responder-" + testName, new Source() { Dynamic = true }, onAttached);
            Message response = responseLink.Receive();
            Assert.IsTrue(response != null, "no response was received");
            responseLink.Accept(response);

            requestLink.Close();
            responseLink.Close();
            session.Close();
            connection.Close();
        }

#if !(NETMF || COMPACT_FRAMEWORK)
        [TestMethod]
#endif
        public void TestMethod_AdvancedLinkFlowControl()
        {
            string testName = "AdvancedLinkFlowControl";
            int nMsgs = 20;
            Connection connection = new Connection(address);
            Session session = new Session(connection);

            SenderLink sender = new SenderLink(session, "sender-" + testName, "q1");
            for (int i = 0; i < nMsgs; ++i)
            {
                Message message = new Message();
                message.Properties = new Properties() { MessageId = "msg" + i, GroupId = testName };
                sender.Send(message, null, null);
            }

            ReceiverLink receiver = new ReceiverLink(session, "receiver-" + testName, "q1");
            receiver.SetCredit(2, false);
            Message m1 = receiver.Receive();
            Message m2 = receiver.Receive();
            Assert.AreEqual("msg0", m1.Properties.MessageId);
            Assert.AreEqual("msg1", m2.Properties.MessageId);
            receiver.Accept(m1);
            receiver.Accept(m2);

            ReceiverLink receiver2 = new ReceiverLink(session, "receiver2-" + testName, "q1");
            receiver2.SetCredit(2, false);
            Message m3 = receiver2.Receive();
            Message m4 = receiver2.Receive();
            Assert.AreEqual("msg2", m3.Properties.MessageId);
            Assert.AreEqual("msg3", m4.Properties.MessageId);
            receiver2.Accept(m3);
            receiver2.Accept(m4);

            receiver.SetCredit(4);
            for (int i = 4; i < nMsgs; i++)
            {
                Message m = receiver.Receive();
                Assert.AreEqual("msg" + i, m.Properties.MessageId);
                receiver.Accept(m);
            }

            sender.Close();
            receiver.Close();
            receiver2.Close();
            session.Close();
            connection.Close();
        }

        /// <summary>
        /// This test proves that issue #14 is fixed.
        /// https://github.com/Azure/amqpnetlite/issues/14
        /// </summary>
 #if !(NETMF || COMPACT_FRAMEWORK)
        [TestMethod, ExpectedException(typeof(ArgumentException))]
#endif
        public void TestMethod_SendEmptyMessage()
        {
            string testName = "SendEmptyMessage";

            Connection connection = new Connection(address);
            Session session = new Session(connection);
            SenderLink sender = new SenderLink(session, "sender-" + testName, "q1");

            try
            {
                sender.Send(new Message());
            }
            finally
            {
                sender.Close();
                session.Close();
                connection.Close();
            }
        }
    }
}
