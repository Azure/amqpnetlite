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
using System.Text;
using System.Threading;
using Amqp.Handler;
#if NETFX || NETFX35 || DOTNET
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
#if NETFX_CORE
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#endif

namespace Test.Amqp
{
#if NETFX || NETFX35 || NETFX_CORE || DOTNET
    [TestClass]
#endif
    public class LinkTests
    {
        TestTarget testTarget = new TestTarget();

        static LinkTests()
        {
            Connection.DisableServerCertValidation = true;
            // uncomment the following to write frame traces
            //Trace.TraceLevel = TraceLevel.Frame;
            //Trace.TraceListener = (l, f, a) => System.Diagnostics.Trace.WriteLine(DateTime.Now.ToString("[hh:mm:ss.fff]") + " " + string.Format(f, a));
        }

#if NETFX || NETFX35 || NETFX_CORE || DOTNET
        [TestMethod]
#endif
        public void TestMethod_BasicSendReceive()
        {
            string testName = "BasicSendReceive";
            const int nMsgs = 200;
            Connection connection = new Connection(testTarget.Address);
            Session session = new Session(connection);
            SenderLink sender = new SenderLink(session, "sender-" + testName, testTarget.Path);

            for (int i = 0; i < nMsgs; ++i)
            {
                Message message = new Message("msg" + i);
                message.Properties = new Properties() { GroupId = "abcdefg" };
                message.ApplicationProperties = new ApplicationProperties();
                message.ApplicationProperties["sn"] = i;
                sender.Send(message, null, null);
            }

            ReceiverLink receiver = new ReceiverLink(session, "receiver-" + testName, testTarget.Path);
            for (int i = 0; i < nMsgs; ++i)
            {
                Message message = receiver.Receive();
                Trace.WriteLine(TraceLevel.Verbose, "receive: {0}", message.ApplicationProperties["sn"]);
                receiver.Accept(message);
            }
            
            sender.Close();
            receiver.Close();
            session.Close();
            connection.Close();
        }

#if NETFX || NETFX35 || NETFX_CORE || DOTNET
        [TestMethod]
#endif
        public void TestMethod_InterfaceSendReceive()
        {
            string testName = "InterfaceSendReceive";
            const int nMsgs = 200;
            IConnection connection = new Connection(testTarget.Address);
            ISession session = connection.CreateSession();
            ISenderLink sender = session.CreateSender("sender-" + testName, testTarget.Path);

            for (int i = 0; i < nMsgs; ++i)
            {
                Message message = new Message("msg" + i);
                message.Properties = new Properties() { GroupId = "abcdefg" };
                message.ApplicationProperties = new ApplicationProperties();
                message.ApplicationProperties["sn"] = i;
                sender.Send(message, null, null);
            }

            IReceiverLink receiver = session.CreateReceiver("receiver-" + testName, testTarget.Path);
            for (int i = 0; i < nMsgs; ++i)
            {
                Message message = receiver.Receive();
                Trace.WriteLine(TraceLevel.Verbose, "receive: {0}", message.ApplicationProperties["sn"]);
                receiver.Accept(message);
            }

            connection.Close();
        }

#if NETFX || NETFX35 || NETFX_CORE || DOTNET
        [TestMethod]
#endif
        public void TestMethod_InterfaceSendReceiveOnAttach()
        {
            string testName = "InterfaceSendReceiveOnAttach";
            const int nMsgs = 200;
            int nAttaches = 0;

            OnAttached onAttached = (link, attach) => {
                nAttaches++;
            };

            IConnection connection = new Connection(testTarget.Address);
            ISession session = connection.CreateSession();
            ISenderLink sender = session.CreateSender("sender-" + testName, new Target() { Address = testTarget.Path }, onAttached);

            for (int i = 0; i < nMsgs; ++i)
            {
                Message message = new Message("msg" + i);
                message.Properties = new Properties() { GroupId = "abcdefg" };
                message.ApplicationProperties = new ApplicationProperties();
                message.ApplicationProperties["sn"] = i;
                sender.Send(message, null, null);
            }

            IReceiverLink receiver = session.CreateReceiver("receiver-" + testName, new Source() { Address = testTarget.Path }, onAttached);
            for (int i = 0; i < nMsgs; ++i)
            {
                Message message = receiver.Receive();
                Trace.WriteLine(TraceLevel.Verbose, "receive: {0}", message.ApplicationProperties["sn"]);
                receiver.Accept(message);
            }

            Assert.AreEqual(2, nAttaches);
            connection.Close();
        }

#if NETFX || NETFX35 || NETFX_CORE || DOTNET
        [TestMethod]
#endif
        public void TestMethod_ConnectionFrameSize()
        {
            string testName = "ConnectionFrameSize";
            const int nMsgs = 200;
            int frameSize = 4 * 1024;
            Connection connection = new Connection(testTarget.Address, null, new Open() { ContainerId = "c1", MaxFrameSize = (uint)frameSize }, null);
            Session session = new Session(connection);
            SenderLink sender = new SenderLink(session, "sender-" + testName, testTarget.Path);

            for (int i = 0; i < nMsgs; ++i)
            {
                Message message = new Message(new string('A', frameSize + (i - nMsgs / 2)));
                sender.Send(message, null, null);
            }

            ReceiverLink receiver = new ReceiverLink(session, "receiver-" + testName, testTarget.Path);
            for (int i = 0; i < nMsgs; ++i)
            {
                Message message = receiver.Receive();
                string value = (string)message.Body;
                Trace.WriteLine(TraceLevel.Verbose, "receive: {0}x{1}", value[0], value.Length);
                receiver.Accept(message);
            }

            sender.Close();
            receiver.Close();
            session.Close();
            connection.Close();
        }

#if NETFX || NETFX35 || NETFX_CORE || DOTNET
        [TestMethod]
#endif
        public void TestMethod_ConnectionChannelMax()
        {
            ushort channelMax = 5;
            Connection connection = new Connection(
                testTarget.Address,
                null,
                new Open() { ContainerId = "ConnectionChannelMax", HostName = testTarget.Address.Host, ChannelMax = channelMax },
                (c, o) => Trace.WriteLine(TraceLevel.Verbose, "{0}", o));

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

#if NETFX || NETFX35 || NETFX_CORE || DOTNET
        [TestMethod]
#endif
        public void TestMethod_ConnectionWithIPAddress()
        {
            string testName = "ConnectionWithIPAddress";
            const int nMsgs = 20;
            // If the test target is 'localhost' then verify that '127.0.0.1' also works.
            if (testTarget.Address.Host != "localhost")
            {
                Trace.WriteLine(TraceLevel.Verbose,
                    "Test {0} skipped. Test target '{1}' is not 'localhost'.",
                    testName, testTarget.Address.Host);
                return;
            }
            Address address2 = new Address("127.0.0.1", testTarget.Address.Port,
                testTarget.Address.User, testTarget.Address.Password,
                testTarget.Address.Path, testTarget.Address.Scheme);
            Connection connection = new Connection(address2);
            Session session = new Session(connection);
            SenderLink sender = new SenderLink(session, "sender-" + testName, testTarget.Path);

            for (int i = 0; i < nMsgs; ++i)
            {
                Message message = new Message("msg" + i);
                message.Properties = new Properties() { GroupId = "abcdefg" };
                message.ApplicationProperties = new ApplicationProperties();
                message.ApplicationProperties["sn"] = i;
                sender.Send(message, null, null);
            }

            ReceiverLink receiver = new ReceiverLink(session, "receiver-" + testName, testTarget.Path);
            for (int i = 0; i < nMsgs; ++i)
            {
                Message message = receiver.Receive();
                Trace.WriteLine(TraceLevel.Verbose, "receive: {0}", message.ApplicationProperties["sn"]);
                receiver.Accept(message);
            }

            sender.Close();
            receiver.Close();
            session.Close();
            connection.Close();
        }

#if NETFX || NETFX35 || NETFX_CORE || DOTNET
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

            Connection connection = new Connection(testTarget.Address, null, open, onOpen);

            opened.WaitOne(10000);

            connection.Close();

            Assert.IsTrue(remoteOpen != null, "remote open not set");
        }

#if NETFX || NETFX35 || NETFX_CORE || DOTNET
        [TestMethod]
#endif
        public void TestMethod_OnMessage()
        {
            string testName = "OnMessage";
            const int nMsgs = 200;
            Connection connection = new Connection(testTarget.Address);
            Session session = new Session(connection);

            ReceiverLink receiver = new ReceiverLink(session, "receiver-" + testName, testTarget.Path);
            ManualResetEvent done = new ManualResetEvent(false);
            int received = 0;
            receiver.Start(10, (link, m) =>
                {
                    Trace.WriteLine(TraceLevel.Verbose, "receive: {0}", m.ApplicationProperties["sn"]);
                    link.Accept(m);
                    received++;
                    if (received == nMsgs)
                    {
                        done.Set();
                    }
                });

            SenderLink sender = new SenderLink(session, "sender-" + testName, testTarget.Path);
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
            while (!done.WaitOne(10000) && received > last)
            {
                last = received;
            }

            sender.Close();
            receiver.Close();
            session.Close();
            connection.Close();

            Assert.AreEqual(nMsgs, received, "not all messages are received.");
        }

#if NETFX || NETFX35 || NETFX_CORE || DOTNET
        [TestMethod]
#endif
        public void TestMethod_CloseBusyReceiver()
        {
            string testName = "CloseBusyReceiver";
            const int nMsgs = 20;
            Connection connection = new Connection(testTarget.Address);
            Session session = new Session(connection);

            SenderLink sender = new SenderLink(session, "sender-" + testName, testTarget.Path);
            for (int i = 0; i < nMsgs; ++i)
            {
                Message message = new Message();
                message.Properties = new Properties() { MessageId = "msg" + i };
                message.ApplicationProperties = new ApplicationProperties();
                message.ApplicationProperties["sn"] = i;
                sender.Send(message, null, null);
            }

            ReceiverLink receiver = new ReceiverLink(session, "receiver-" + testName, testTarget.Path);
            ManualResetEvent closed = new ManualResetEvent(false);
            receiver.Closed += (o, e) => closed.Set();
            receiver.Start(
                nMsgs,
                (r, m) =>
                {
                    if (m.Properties.MessageId == "msg0") r.Close(TimeSpan.Zero, null);
                });
            Assert.IsTrue(closed.WaitOne(10000));

            ReceiverLink receiver2 = new ReceiverLink(session, "receiver2-" + testName, testTarget.Path);
            for (int i = 0; i < nMsgs; ++i)
            {
                Message message = receiver2.Receive();
                Trace.WriteLine(TraceLevel.Verbose, "receive: {0}", message.Properties.MessageId);
                receiver2.Accept(message);
            }

            receiver2.Close();
            sender.Close();
            session.Close();
            connection.Close();
        }

#if NETFX || NETFX35 || NETFX_CORE || DOTNET
        [TestMethod]
#endif
        public void TestMethod_ReleaseMessage()
        {
            string testName = "ReleaseMessage";
            const int nMsgs = 20;
            Connection connection = new Connection(testTarget.Address);
            Session session = new Session(connection);

            SenderLink sender = new SenderLink(session, "sender-" + testName, testTarget.Path);
            for (int i = 0; i < nMsgs; ++i)
            {
                Message message = new Message();
                message.Properties = new Properties() { MessageId = "msg" + i };
                message.ApplicationProperties = new ApplicationProperties();
                message.ApplicationProperties["sn"] = i;
                sender.Send(message, null, null);
            }

            ReceiverLink receiver = new ReceiverLink(session, "receiver-" + testName, testTarget.Path);
            for (int i = 0; i < nMsgs; ++i)
            {
                Message message = receiver.Receive();
                Trace.WriteLine(TraceLevel.Verbose, "receive: {0}", message.Properties.MessageId);
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

            ReceiverLink receiver2 = new ReceiverLink(session, "receiver2-" + testName, testTarget.Path);
            for (int i = 0; i < nMsgs / 2; ++i)
            {
                Message message = receiver2.Receive();
                Trace.WriteLine(TraceLevel.Verbose, "receive: {0}", message.Properties.MessageId);
                receiver2.Accept(message);
            }

            receiver2.Close();
            sender.Close();
            session.Close();
            connection.Close();
        }

#if NETFX || NETFX35 || NETFX_CORE || DOTNET
        [TestMethod]
#endif
        public void TestMethod_ModifyMessage()
        {
            string testName = "ModifyMessage";
            const int nMsgs = 20;
            Connection connection = new Connection(testTarget.Address);
            Session session = new Session(connection);

            SenderLink sender = new SenderLink(session, "sender-" + testName, testTarget.Path);
            for (int i = 0; i < nMsgs; ++i)
            {
                Message message = new Message();
                message.MessageAnnotations = new MessageAnnotations();
                message.MessageAnnotations[(Symbol)"a1"] = 12345L;
                message.Properties = new Properties() { MessageId = "msg" + i };
                message.ApplicationProperties = new ApplicationProperties();
                message.ApplicationProperties["sn"] = i;
                sender.Send(message, null, null);
            }

            ReceiverLink receiver = new ReceiverLink(session, "receiver-" + testName, testTarget.Path);
            for (int i = 0; i < nMsgs; ++i)
            {
                Message message = receiver.Receive();
                Trace.WriteLine(TraceLevel.Verbose, "receive: {0}", message.Properties.MessageId);
                if (i % 2 == 0)
                {
                    receiver.Accept(message);
                }
                else
                {
                    receiver.Modify(message, true, false, new Fields() { { (Symbol)"reason", "app offline" } });
                }
            }
            receiver.Close();

            ReceiverLink receiver2 = new ReceiverLink(session, "receiver2-" + testName, testTarget.Path);
            for (int i = 0; i < nMsgs / 2; ++i)
            {
                Message message = receiver2.Receive();
                Trace.WriteLine(TraceLevel.Verbose, "receive: {0}", message.Properties.MessageId);
                Assert.IsTrue(message.Header != null, "header is null");
                Assert.IsTrue(message.Header.DeliveryCount > 0, "delivery-count is 0");
                Assert.IsTrue(message.MessageAnnotations != null, "annotation is null");
                Assert.IsTrue(message.MessageAnnotations[(Symbol)"a1"].Equals(12345L));
                Assert.IsTrue(message.MessageAnnotations[(Symbol)"reason"].Equals("app offline"));
                receiver2.Accept(message);
            }

            receiver2.Close();
            sender.Close();
            session.Close();
            connection.Close();
        }

#if NETFX || NETFX35 || NETFX_CORE || DOTNET
        [TestMethod]
#endif
        public void TestMethod_SendAck()
        {
            string testName = "SendAck";
            const int nMsgs = 20;
            Connection connection = new Connection(testTarget.Address);
            Session session = new Session(connection);

            SenderLink sender = new SenderLink(session, "sender-" + testName, testTarget.Path);
            ManualResetEvent done = new ManualResetEvent(false);
            OutcomeCallback callback = (l, m, o, s) =>
            {
                Trace.WriteLine(TraceLevel.Verbose, "send complete: sn {0} outcome {1}", m.ApplicationProperties["sn"], o.Descriptor.Name);
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

            done.WaitOne(10000);

            ReceiverLink receiver = new ReceiverLink(session, "receiver-" + testName, testTarget.Path);
            for (int i = 0; i < nMsgs; ++i)
            {
                Message message = receiver.Receive();
                Trace.WriteLine(TraceLevel.Verbose, "receive: {0}", message.ApplicationProperties["sn"]);
                receiver.Accept(message);
            }
            
            sender.Close();
            receiver.Close();
            session.Close();
            connection.Close();
        }

#if NETFX || NETFX35 || NETFX_CORE || DOTNET
        [TestMethod]
#endif
        public void TestMethod_MessageId()
        {
            string testName = "MessageId";
            Connection connection = new Connection(testTarget.Address);
            Session session = new Session(connection);
            object[] idList = new object[] { null, "string-id", 20000UL, Guid.NewGuid(), Encoding.UTF8.GetBytes("binary-id") };

            SenderLink sender = new SenderLink(session, "sender-" + testName, testTarget.Path);
            for (int i = 0; i < idList.Length; ++i)
            {
                Message message = new Message() { Properties = new Properties() };
                message.Properties.SetMessageId(idList[i]);
                message.Properties.SetCorrelationId(idList[(i + 2) % idList.Length]);
                sender.Send(message, null, null);
            }

            ReceiverLink receiver = new ReceiverLink(session, "receiver-" + testName, testTarget.Path);
            for (int i = 0; i < idList.Length; ++i)
            {
                Message message = receiver.Receive();
                receiver.Accept(message);
                Assert.AreEqual(idList[i], message.Properties.GetMessageId());
                Assert.AreEqual(idList[(i + 2) % idList.Length], message.Properties.GetCorrelationId());
            }

            connection.Close();

            // invalid types
            Properties prop = new Properties();
            try
            {
                prop.SetMessageId(0);
                Assert.IsTrue(false, "not a valid identifier type");
            }
            catch (AmqpException ae)
            {
                Assert.AreEqual(ErrorCode.NotAllowed, (string)ae.Error.Condition);
            }

            try
            {
                prop.SetCorrelationId(new Symbol("symbol"));
                Assert.IsTrue(false, "not a valid identifier type");
            }
            catch (AmqpException ae)
            {
                Assert.AreEqual(ErrorCode.NotAllowed, (string)ae.Error.Condition);
            }
        }

#if NETFX || NETFX35 || NETFX_CORE || DOTNET
        [TestMethod]
#endif
        public void TestMethod_ReceiveWaiter()
        {
            string testName = "ReceiveWaiter";
            Connection connection = new Connection(testTarget.Address);
            Session session = new Session(connection);

            ReceiverLink receiver = new ReceiverLink(session, "receiver-" + testName, testTarget.Path);
            ManualResetEvent gotMessage = new ManualResetEvent(false);
            StartThread(() =>
            {
                Message message = receiver.Receive(TimeSpan.MaxValue);
                if (message != null)
                {
                    receiver.Accept(message);
                    gotMessage.Set();
                }
            });

            SenderLink sender = new SenderLink(session, "sender-" + testName, testTarget.Path);
            Message msg = new Message() { Properties = new Properties() { MessageId = "123456" } };
            sender.Send(msg, null, null);

            Assert.IsTrue(gotMessage.WaitOne(5000), "No message was received");

            sender.Close();
            receiver.Close();
            session.Close();
            connection.Close();
        }

#if NETFX || NETFX35 || NETFX_CORE || DOTNET
        [TestMethod]
#endif
        public void TestMethod_ReceiveWaiterZero()
        {
            string testName = "ReceiveWaiterZero";
            Connection connection = new Connection(testTarget.Address);
            Session session = new Session(connection);

            ReceiverLink receiver = new ReceiverLink(session, "receiver-" + testName, testTarget.Path);
            Message msg = receiver.Receive(TimeSpan.Zero);
            Assert.IsTrue(msg == null);

            SenderLink sender = new SenderLink(session, "sender-" + testName, testTarget.Path);
            msg = new Message() { Properties = new Properties() { MessageId = "123456" } };
            sender.Send(msg, null, null);

            for (int i = 0; i < 1000; i++)
            {
                msg = receiver.Receive(TimeSpan.Zero);
                if (msg != null)
                {
                    receiver.Accept(msg);
                    break;
                }

#if NETFX_CORE
                System.Threading.Tasks.Task.Delay(10).Wait();
#else
                Thread.Sleep(10);
#endif
            }

            Assert.IsTrue(msg != null, "Message not received");

            sender.Close();
            receiver.Close();
            session.Close();
            connection.Close();
        }

#if NETFX || NETFX35 || NETFX_CORE || DOTNET
        [TestMethod]
#endif
        public void TestMethod_ReceiveWithFilter()
        {
            string testName = "ReceiveWithFilter";
            Connection connection = new Connection(testTarget.Address);
            Session session = new Session(connection);

            Message message = new Message("I can match a filter");
            message.Properties = new Properties() { GroupId = "abcdefg" };
            message.ApplicationProperties = new ApplicationProperties();
            message.ApplicationProperties["sn"] = 100;

            SenderLink sender = new SenderLink(session, "sender-" + testName, testTarget.Path);
            sender.Send(message, null, null);

            // update the filter descriptor and expression according to the broker
            Map filters = new Map();
            // JMS selector filter: code = 0x0000468C00000004L, symbol="apache.org:selector-filter:string"
            filters.Add(new Symbol("f1"), new DescribedValue(new Symbol("apache.org:selector-filter:string"), "sn = 100"));
            ReceiverLink receiver = new ReceiverLink(session, "receiver-" + testName, new Source() { Address = testTarget.Path, FilterSet = filters }, null);
            Message message2 = receiver.Receive();
            receiver.Accept(message2);

            sender.Close();
            receiver.Close();
            session.Close();
            connection.Close();
        }

#if NETFX || NETFX35 || NETFX_CORE || DOTNET
        [TestMethod]
#endif
        public void TestMethod_LinkCloseWithSettledSend()
        {
            string testName = "LinkCloseWithSettledSend";
            Connection connection = new Connection(testTarget.Address);
            Session session = new Session(connection);
            SenderLink sender = new SenderLink(session, "sender-" + testName, testTarget.Path);

            Message message = new Message(testName);
            sender.Send(message, null, null);
            sender.Close();

            ReceiverLink receiver = new ReceiverLink(session, "receiver-" + testName, testTarget.Path);
            message = receiver.Receive(new TimeSpan(0, 0, 10));
            Assert.IsTrue(message != null, "no message was received.");
            receiver.Accept(message);

            connection.Close();
        }

#if NETFX || NETFX35 || NETFX_CORE || DOTNET
        [TestMethod]
#endif
        public void TestMethod_SynchronousSend()
        {
            string testName = "SynchronousSend";
            Connection connection = new Connection(testTarget.Address);
            Session session = new Session(connection);
            SenderLink sender = new SenderLink(session, "sender-" + testName, testTarget.Path);
            Message message = new Message("hello");
            sender.Send(message);

            ReceiverLink receiver = new ReceiverLink(session, "receiver-" + testName, testTarget.Path);
            message = receiver.Receive();
            Assert.IsTrue(message != null, "no message was received.");
            receiver.Accept(message);

            sender.Close();
            receiver.Close();
            session.Close();
            connection.Close();
        }

#if NETFX || NETFX35 || NETFX_CORE || DOTNET
        [TestMethod]
#endif
        public void TestMethod_DynamicSenderLink()
        {
            string testName = "DynamicSenderLink";
            Connection connection = new Connection(testTarget.Address);
            Session session = new Session(connection);

            string targetAddress = null;
            OnAttached onAttached = (link, attach) =>
            {
                targetAddress = ((Target)attach.Target).Address;
            };

            SenderLink sender = new SenderLink(session, "sender-" + testName, new Target() { Dynamic = true }, onAttached);
            Message message = new Message("hello");
            sender.Send(message);

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

#if NETFX || NETFX35 || NETFX_CORE || DOTNET
        [TestMethod]
#endif
        public void TestMethod_DynamicReceiverLink()
        {
            string testName = "DynamicReceiverLink";
            Connection connection = new Connection(testTarget.Address);
            Session session = new Session(connection);

            string remoteSource = null;
            ManualResetEvent attached = new ManualResetEvent(false);
            OnAttached onAttached = (link, attach) => { remoteSource = ((Source)attach.Source).Address; attached.Set(); };
            ReceiverLink receiver = new ReceiverLink(session, "receiver-" + testName, new Source() { Dynamic = true }, onAttached);

            attached.WaitOne(10000);

            Assert.IsTrue(remoteSource != null, "dynamic source not attached");

            SenderLink sender = new SenderLink(session, "sender-" + testName, remoteSource);
            Message message = new Message("hello");
            sender.Send(message);

            message = receiver.Receive();
            Assert.IsTrue(message != null, "no message was received.");
            receiver.Accept(message);

            sender.Close();
            receiver.Close();
            session.Close();
            connection.Close();
        }

#if NETFX || NETFX35 || NETFX_CORE || DOTNET
        [TestMethod]
#endif
        public void TestMethod_RequestResponse()
        {
            string testName = "RequestResponse";
            Connection connection = new Connection(testTarget.Address);
            Session session = new Session(connection);

            // server app: the request handler
            ReceiverLink requestLink = new ReceiverLink(session, "srv.requester-" + testName, testTarget.Path);
            requestLink.Start(10, (l, m) =>
                {
                    l.Accept(m);

                    // got a request, send back a reply
                    SenderLink sender = new SenderLink(session, "srv.replier-" + testName, m.Properties.ReplyTo);
                    Message reply = new Message("received");
                    reply.Properties = new Properties() { CorrelationId = m.Properties.MessageId };
                    sender.Send(reply, (a, b, c, d) => ((Link)a).Close(TimeSpan.Zero), sender);
                });

            // client: setup a temp queue and waits for responses
            OnAttached onAttached = (l, at) =>
                {
                    // client: sends a request to the request queue, specifies the temp queue as the reply queue
                    SenderLink sender = new SenderLink(session, "cli.requester-" + testName, testTarget.Path);
                    Message request = new Message("hello");
                    request.Properties = new Properties() { MessageId = "request1", ReplyTo = ((Source)at.Source).Address };
                    sender.Send(request, (a, b, c, d) => ((Link)a).Close(TimeSpan.Zero), sender);
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

#if NETFX || NETFX35 || NETFX_CORE || DOTNET
        [TestMethod]
#endif
        public void TestMethod_AdvancedLinkFlowControl()
        {
            string testName = "AdvancedLinkFlowControl";
            int nMsgs = 20;
            Connection connection = new Connection(testTarget.Address);
            Session session = new Session(connection);

            SenderLink sender = new SenderLink(session, "sender-" + testName, testTarget.Path);
            for (int i = 0; i < nMsgs; ++i)
            {
                Message message = new Message();
                message.Properties = new Properties() { MessageId = "msg" + i };
                sender.Send(message, null, null);
            }

            ReceiverLink receiver = new ReceiverLink(session, "receiver-" + testName, testTarget.Path);
            receiver.SetCredit(2, false);
            Message m1 = receiver.Receive();
            Message m2 = receiver.Receive();
            Assert.AreEqual("msg0", m1.Properties.MessageId);
            Assert.AreEqual("msg1", m2.Properties.MessageId);
            receiver.Accept(m1);
            receiver.Accept(m2);

            ReceiverLink receiver2 = new ReceiverLink(session, "receiver2-" + testName, testTarget.Path);
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

#if !NETMF && !NETFX_CORE
        [TestMethod]
        public void TestMethod_ProtocolHandler()
        {
            string testName = "ProtocolHandler";
            int nMsgs = 5;

            var handler = new TestHandler(e =>
            {
                if (e.Id == EventId.SocketConnect)
                {
                    ((System.Net.Sockets.Socket)e.Context).SendBufferSize = 4096;
                }
                else if (e.Id == EventId.SslAuthenticate)
                {
                    ((System.Net.Security.SslStream)e.Context).AuthenticateAsClient("localhost");
                }
            });

            Address sslAddress = new Address("amqps://guest:guest@localhost:5671");
            Connection connection = new Connection(sslAddress, handler);
            Session session = new Session(connection);
            SenderLink sender = new SenderLink(session, "sender-" + testName, testTarget.Path);

            for (int i = 0; i < nMsgs; ++i)
            {
                Message message = new Message("msg" + i);
                message.Properties = new Properties() { GroupId = "abcdefg" };
                message.ApplicationProperties = new ApplicationProperties();
                message.ApplicationProperties["sn"] = i;
                sender.Send(message, null, null);
            }

            ReceiverLink receiver = new ReceiverLink(session, "receiver-" + testName, testTarget.Path);
            for (int i = 0; i < nMsgs; ++i)
            {
                Message message = receiver.Receive();
                Trace.WriteLine(TraceLevel.Verbose, "receive: {0}", message.ApplicationProperties["sn"]);
                receiver.Accept(message);
            }

            sender.Close();
            receiver.Close();
            session.Close();
            connection.Close();
        }
#endif

        /// <summary>
        /// This test proves that issue #14 is fixed.
        /// https://github.com/Azure/amqpnetlite/issues/14
        /// </summary>
#if NETFX || NETFX35 || NETFX_CORE || DOTNET
        [TestMethod]
#endif
        public void TestMethod_SendEmptyMessage()
        {
            string testName = "SendEmptyMessage";

            Connection connection = new Connection(testTarget.Address);
            Session session = new Session(connection);
            SenderLink sender = new SenderLink(session, "sender-" + testName, testTarget.Path);

            bool threwArgEx = false;
            try
            {
                sender.Send(new Message());
            }
            catch (ArgumentException)
            {
                threwArgEx = true;
            }
            finally
            {
                sender.Close();
                session.Close();
                connection.Close();
            }

            Assert.IsTrue(threwArgEx, "Should throw an argument exception when sending an empty message.");
        }

#if NETFX || NETFX35 || NETFX_CORE || DOTNET
        [TestMethod]
#endif
        public void TestMethod_SendToNonExistingNode()
        {
            string testName = "SendToNonExistingNode";

            Connection connection = new Connection(testTarget.Address);
            Session session = new Session(connection);
            SenderLink sender = new SenderLink(session, "$explicit:sender-" + testName, Guid.NewGuid().ToString());

            try
            {
                sender.Send(new Message("test"));
                Assert.IsTrue(false, "Send does not fail");
            }
            catch (AmqpException exception)
            {
                Assert.AreEqual((Symbol)ErrorCode.NotFound, exception.Error.Condition);
            }
            finally
            {
                connection.Close();
            }
        }

#if NETFX || NETFX35 || NETFX_CORE || DOTNET
        [TestMethod]
#endif
        public void TestMethod_ReceiveFromNonExistingNode()
        {
            string testName = "ReceiveFromNonExistingNode";

            Connection connection = new Connection(testTarget.Address);
            Session session = new Session(connection);
            ReceiverLink receiver = new ReceiverLink(session, "$explicit:receiver-" + testName, Guid.NewGuid().ToString());

            try
            {
                receiver.Receive();
                Assert.IsTrue(false, "receive does not fail");
            }
            catch (AmqpException exception)
            {
                Assert.AreEqual((Symbol)ErrorCode.NotFound, exception.Error.Condition);
            }
            finally
            {
                connection.Close();
            }
        }

#if NETFX || NETFX35 || NETFX_CORE || DOTNET
        [TestMethod]
#endif
        public void TestMethod_ConnectionCreateClose()
        {
            Connection connection = new Connection(testTarget.Address);
            connection.Close();
            Assert.IsTrue(connection.Error == null, "connection has error!");
        }

#if NETFX || NETFX35 || NETFX_CORE || DOTNET
        [TestMethod]
#endif
        public void TestMethod_SessionCreateClose()
        {
            Connection connection = new Connection(testTarget.Address);
            Session session = new Session(connection);
            session.Close(TimeSpan.Zero);
            connection.Close();
            Assert.IsTrue(connection.Error == null, "connection has error!");
        }

#if NETFX || NETFX35 || NETFX_CORE || DOTNET
        [TestMethod]
#endif
        public void TestMethod_LinkCreateClose()
        {
            Connection connection = new Connection(testTarget.Address);
            Session session = new Session(connection);
            SenderLink sender = new SenderLink(session, "sender", testTarget.Path);
            ReceiverLink receiver = new ReceiverLink(session, "receiver", testTarget.Path);
            sender.Close(TimeSpan.Zero);
            receiver.Close(TimeSpan.Zero);
            session.Close(TimeSpan.Zero);
            connection.Close();
            Assert.IsTrue(connection.Error == null, "connection has error!");
        }

#if NETFX || NETFX35 || NETFX_CORE || DOTNET
        [TestMethod]
#endif
        public void TestMethod_LinkReopen()
        {
            string testName = "LinkReopen";

            Connection connection = new Connection(testTarget.Address);
            Session session = new Session(connection);
            SenderLink sender = new SenderLink(session, "sender", testTarget.Path);
            sender.Send(new Message("test") { Properties = new Properties() { MessageId = testName } });
            sender.Close();

            sender = new SenderLink(session, "sender", testTarget.Path);
            sender.Send(new Message("test2") { Properties = new Properties() { MessageId = testName } });
            sender.Close();

            ReceiverLink receiver = new ReceiverLink(session, "receiver", testTarget.Path);
            for (int i = 1; i <= 2; i++)
            {
                var m = receiver.Receive();
                Assert.IsTrue(m != null, "Didn't receive message " + i);
                receiver.Accept(m);
            }

            session.Close(TimeSpan.Zero);
            connection.Close();
            Assert.IsTrue(connection.Error == null, "connection has error!");
        }

#if NETFX_CORE
        static void StartThread(Action action)
        {
            System.Threading.Tasks.Task.Factory.StartNew(action);
        }
#elif NETMF
        static void StartThread(ThreadStart threadStart)
        {
            new Thread(threadStart).Start();
        }
#else
        static void StartThread(ThreadStart threadStart)
        {
            ThreadPool.QueueUserWorkItem(
                o => { ((ThreadStart)o)(); },
                threadStart);
        }
#endif
    }
#if NETMF

    static class Extensions
    {
        internal static bool WaitOne(this ManualResetEvent mre, int milliseconds)
        {
            return mre.WaitOne(milliseconds, false);
        }
    }
#endif
}
