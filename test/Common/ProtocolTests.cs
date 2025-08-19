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
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Amqp;
using Amqp.Framing;
using Amqp.Handler;
using Amqp.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test.Amqp
{
    [TestClass]
    public class ProtocolTests
    {
        const int port = 5674;
        TestListener testListener;
        Address address;

        static ProtocolTests()
        {
            //Trace.TraceLevel = TraceLevel.Frame | TraceLevel.Information;
            //Trace.TraceListener = (l, f, a) => System.Diagnostics.Trace.WriteLine(System.DateTime.Now.ToString("[hh:mm:ss.fff]") + " " + string.Format(f, a));
        }

        [TestInitialize]
        public void TestInitialize()
        {
            this.testListener = new TestListener(new IPEndPoint(IPAddress.Any, port));
            this.testListener.Open();
            this.address = new Address("amqp://127.0.0.1:" + port);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            this.testListener.Close();
        }

        [TestMethod]
        public void ConnectionMaxFrameSizeTest()
        {
            this.testListener.RegisterTarget(TestPoint.Open, (stream, channel, fields) =>
            {
                TestListener.FRM(stream, 0x10UL, 0, 0, "TestListener", "localhost", 512u);
                return TestOutcome.Stop;
            });

            this.testListener.RegisterTarget(TestPoint.Begin, (stream, channel, fields) =>
            {
                TestListener.FRM(stream, 0x11UL, 0, channel, channel, 0u, 100u, 100u, 8u, null, null,
                    new Fields() { { new Symbol("big-string"), new string('a', 1024) } });
                return TestOutcome.Stop;
            });

            string testName = "ConnectionMaxFrameSizeTest";

            Trace.WriteLine(TraceLevel.Information, "sync test");
            {
                Open open = new Open() { ContainerId = testName, HostName = "localhost", MaxFrameSize = 2048 };
                Connection connection = new Connection(this.address, null, open, null);
                Session session = new Session(connection);
                SenderLink sender = new SenderLink(session, "sender-" + testName, "any");
                sender.Send(new Message("test") { Properties = new Properties() { MessageId = testName } });
                connection.Close();
                Assert.IsTrue(connection.Error == null, "connection has error!" + connection.Error);
            }

            Trace.WriteLine(TraceLevel.Information, "async test");
            Task.Factory.StartNew(async () =>
            {
                ConnectionFactory factory = new ConnectionFactory();
                factory.AMQP.MaxFrameSize = 2048;
                Connection connection = await factory.CreateAsync(this.address);
                Session session = new Session(connection);
                SenderLink sender = new SenderLink(session, "sender-" + testName, "any");
                await sender.SendAsync(new Message("test") { Properties = new Properties() { MessageId = testName } });
                await connection.CloseAsync();
                Assert.IsTrue(connection.Error == null, "connection has error!" + connection.Error);
            }).Unwrap().GetAwaiter().GetResult();
        }

        [TestMethod]
        public void ConnectionMaxFrameSizeNegativeTest()
        {
            Stream networkStream = null;
            this.testListener.RegisterTarget(TestPoint.Flow, (stream, channel, fields) =>
            {
                networkStream = stream;
                TestListener.FRM(stream, 0x13UL, 0, channel, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, false, false,
                    new Fields() { { new Symbol("big-string"), new string('a', 1024) } });  // flow
                return TestOutcome.Stop;
            });

            string testName = "ConnectionMaxFrameSizeNegativeTest";

            Trace.WriteLine(TraceLevel.Information, "sync test");
            {
                Open open = new Open() { ContainerId = testName, HostName = "localhost", MaxFrameSize = 512 };
                Connection connection = new Connection(this.address, null, open, null);
                Session session = new Session(connection);
                ReceiverLink receiver = new ReceiverLink(session, "receiver-" + testName, "any");
                try
                {
                    receiver.Receive();
                }
                catch (AmqpException) { }
                Assert.IsTrue(connection.Error != null);
                Assert.AreEqual((Symbol)ErrorCode.InvalidField, connection.Error.Condition);
                Assert.AreEqual(ConnectionState.End, connection.ConnectionState);
                try
                {
                    for (int i = 0; i < 20; i++)
                    {
                        networkStream.WriteByte(0);
                        Thread.Sleep(100);
                    }

                    Assert.IsTrue(false, "transport connection not closed");
                }
                catch (IOException) { }
                catch (ObjectDisposedException) { }
            }

            Trace.WriteLine(TraceLevel.Information, "async test");
            networkStream = null;
            Task.Factory.StartNew(async () =>
            {
                ConnectionFactory factory = new ConnectionFactory();
                factory.AMQP.MaxFrameSize = 512;
                IConnection connection = await factory.CreateAsync(this.address);
                ISession session = connection.CreateSession();
                IReceiverLink receiver = session.CreateReceiver("receiver-" + testName, "any");
                try
                {
                    await receiver.ReceiveAsync();
                }
                catch (AmqpException) { }
                Assert.IsTrue(connection.Error != null);
                Assert.AreEqual((Symbol)ErrorCode.InvalidField, connection.Error.Condition);
                try
                {
                    for (int i = 0; i < 20; i++)
                    {
                        networkStream.WriteByte(0);
                        Thread.Sleep(100);
                    }

                    Assert.IsTrue(false, "transport connection not closed");
                }
                catch (IOException) { }
                catch (ObjectDisposedException) { }
                await connection.CloseAsync();
            }).Unwrap().GetAwaiter().GetResult();
        }

        [TestMethod]
        public void ConnectionWithUserOpenTest()
        {
            string testName = "ConnectionWithUserOpenTest";
            List receivedOpen = null;
            this.testListener.RegisterTarget(TestPoint.Open, (stream, channel, fields) =>
            {
                receivedOpen = fields;
                return TestOutcome.Continue;
            });

            Task.Factory.StartNew(async () =>
            {
                ConnectionFactory factory = new ConnectionFactory();
                factory.AMQP.MaxFrameSize = 2048;
                factory.AMQP.MaxSessionsPerConnection = 10;

                Open open = new Open() { ContainerId = testName, HostName = "localhost", MaxFrameSize = 4096, ChannelMax = 128 };
                Connection connection = await factory.CreateAsync(this.address, open, null);
                Session session = new Session(connection);
                SenderLink sender = new SenderLink(session, "sender-" + testName, "any");
                await sender.SendAsync(new Message("test") { Properties = new Properties() { MessageId = testName } });
                await connection.CloseAsync();

                Assert.IsTrue(receivedOpen != null);
                Assert.AreEqual(open.MaxFrameSize, receivedOpen[2]);
                Assert.AreEqual(open.ChannelMax, receivedOpen[3]);
            }).Unwrap().GetAwaiter().GetResult();
        }

        [TestMethod]
        public void RemoteSessionChannelTest()
        {
            this.testListener.RegisterTarget(TestPoint.Begin, (stream, channel, fields) =>
            {
                // send a large channel number to test if client can grow the table correctly
                TestListener.FRM(stream, 0x11UL, 0, (ushort)(channel + 100), channel, 0u, 100u, 100u, 8u);
                return TestOutcome.Stop;
            });

            string testName = "ConnectionChannelTest";

            Open open = new Open() { ContainerId = testName, HostName = "localhost", MaxFrameSize = 2048 };
            Connection connection = new Connection(this.address, null, open, null);
            for (int i = 0; i < 10; i++)
            {
                Session session = new Session(connection);
            }

            connection.Close();
            Assert.IsTrue(connection.Error == null, "connection has error!" + connection.Error);
        }

        [TestMethod]
        public void FlowNextIncomingIdTest()
        {
            string testName = "FlowNextIncomingIdTest";

            List flow = null;
            ManualResetEvent received = new ManualResetEvent(false);

            this.testListener.RegisterTarget(TestPoint.Begin, (stream, channel, fields) =>
            {
                return TestOutcome.Stop;
            });

            this.testListener.RegisterTarget(TestPoint.Attach, (stream, channel, fields) =>
            {
                return TestOutcome.Stop;
            });

            this.testListener.RegisterTarget(TestPoint.Flow, (stream, channel, fields) =>
            {
                TestListener.FRM(stream, 0x11UL, 0, channel, channel, 0u, 100u, 100u, 8u);
                TestListener.FRM(stream, 0x12UL, 0, channel, testName, 0u, false, null, null, new Source(), new Target());
                flow = fields;
                received.Set();
                return TestOutcome.Stop;
            });

            Open open = new Open() { ContainerId = testName, HostName = "localhost", MaxFrameSize = 2048 };
            Connection connection = new Connection(this.address, null, open, null);
            Session session = new Session(connection);
            ReceiverLink receiver = new ReceiverLink(session, testName, "foo");
            receiver.SetCredit(100);
            received.WaitOne(3000);
            connection.Close();

            Assert.IsTrue(flow != null, "flow is null");
            Assert.IsTrue(flow[0] == null, "next-incoming-id is not null");
        }

        [TestMethod]
        public void FlowDeliveryCountTest()
        {
            string testName = "FlowDeliveryCountTest";

            List flow = null;
            ManualResetEvent received = new ManualResetEvent(false);

            this.testListener.RegisterTarget(TestPoint.Attach, (stream, channel, fields) =>
            {
                return TestOutcome.Stop;
            });

            this.testListener.RegisterTarget(TestPoint.Flow, (stream, channel, fields) =>
            {
                TestListener.FRM(stream, 0x12UL, 0, channel, testName, 0u, false, null, null, new Source(), new Target());
                flow = fields;
                received.Set();
                return TestOutcome.Stop;
            });

            Open open = new Open() { ContainerId = testName, HostName = "localhost", MaxFrameSize = 2048 };
            Connection connection = new Connection(this.address, null, open, null);
            Session session = new Session(connection);
            ReceiverLink receiver = new ReceiverLink(session, testName, "foo");
            receiver.SetCredit(100);
            received.WaitOne(3000);
            connection.Close();

            Assert.IsTrue(flow != null, "flow is null");
            Assert.IsTrue(flow[5] == null, "delivery-count is not null");
        }

        [TestMethod]
        public void RemoteLinkHandleTest()
        {
            this.testListener.RegisterTarget(TestPoint.Begin, (stream, channel, fields) =>
            {
                TestListener.FRM(stream, 0x11UL, 0, channel, channel, 0u, 100u, 100u, 8000u);
                return TestOutcome.Stop;
            });

            this.testListener.RegisterTarget(TestPoint.Attach, (stream, channel, fields) =>
            {
                uint handle = (uint)fields[1];
                fields[1] = handle + 100u;
                return TestOutcome.Continue;
            });

            string testName = "RemoteLinkHandleTest";

            Open open = new Open() { ContainerId = testName, HostName = "localhost", MaxFrameSize = 2048 };
            Connection connection = new Connection(this.address, null, open, null);
            Session session = new Session(connection);
            for (int i = 0; i < 10; i++)
            {
                SenderLink sender = new SenderLink(session, "sender-" + i, "any");
            }

            connection.Close();
            Assert.IsTrue(connection.Error == null, "connection has error!" + connection.Error);
        }

        [TestMethod]
        public void ConnectionIdleTimeoutRemoteTest()
        {
            ManualResetEvent received = new ManualResetEvent(false);

            this.testListener.RegisterTarget(TestPoint.Open, (stream, channel, fields) =>
            {
                TestListener.FRM(stream, 0x10UL, 0, 0, "TestListener", "localhost", 512u, (ushort)8, 1000u);
                return TestOutcome.Stop;
            });

            this.testListener.RegisterTarget(TestPoint.Empty, (stream, channel, fields) =>
            {
                received.Set();
                return TestOutcome.Continue;
            });

            string testName = "ConnectionIdleTimeoutRemoteTest";

            Trace.WriteLine(TraceLevel.Information, "sync test");
            {
                Connection connection = new Connection(this.address);
                Session session = new Session(connection);
                SenderLink sender = new SenderLink(session, "sender-" + testName, "any");
                sender.Send(new Message("test") { Properties = new Properties() { MessageId = testName } });
                var h = connection.GetType().GetField("heartBeat", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(connection);
                Assert.IsTrue(h != null, "heart beat is not initialized");
                Assert.IsTrue(received.WaitOne(5000), "Heartbeat not received");
                connection.Close();
            }
#if !NETFX40
            received = new ManualResetEvent(false);
            Trace.WriteLine(TraceLevel.Information, "async test");
            Task.Factory.StartNew(async () =>
            {
                ConnectionFactory factory = new ConnectionFactory();
                Connection connection = await factory.CreateAsync(this.address);
                Session session = new Session(connection);
                SenderLink sender = new SenderLink(session, "sender-" + testName, "any");
                await sender.SendAsync(new Message("test") { Properties = new Properties() { MessageId = testName } });
                var h = connection.GetType().GetField("heartBeat", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(connection);
                Assert.IsTrue(h != null, "heart beat is not initialized");
                await Task.Yield();
                Assert.IsTrue(received.WaitOne(5000), "Heartbeat not received");
                await connection.CloseAsync();
            }).Unwrap().GetAwaiter().GetResult();
#endif
        }

        [TestMethod]
        public void ConnectionIdleTimeoutLocalTest()
        {
            string testName = "ConnectionIdleTimeoutLocalTest";

            Trace.WriteLine(TraceLevel.Information, "sync test");
            {
                Open open = new Open() { ContainerId = testName, HostName = "localhost", IdleTimeOut = 500 };
                Connection connection = new Connection(this.address, null, open, null);
                Session session = new Session(connection);
                SenderLink sender = new SenderLink(session, "sender-" + testName, "any");
                sender.Send(new Message("test") { Properties = new Properties() { MessageId = testName } });
                var h = connection.GetType().GetField("heartBeat", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(connection);
                Assert.IsTrue(h != null, "heart beat is not initialized");
                Thread.Sleep(500);
                Assert.IsTrue(!connection.IsClosed, "connection should not be closed");
                Assert.Wait(5000, () => connection.IsClosed, "connection not closed");
            }

#if !NETFX40
            Trace.WriteLine(TraceLevel.Information, "async test");
            Task.Factory.StartNew(async () =>
            {
                ConnectionFactory factory = new ConnectionFactory();
                factory.AMQP.IdleTimeout = 1000;
                Connection connection = await factory.CreateAsync(this.address);
                Session session = new Session(connection);
                SenderLink sender = new SenderLink(session, "sender-" + testName, "any");
                await sender.SendAsync(new Message("test") { Properties = new Properties() { MessageId = testName } });
                await Task.Delay(1200);
                var h = connection.GetType().GetField("heartBeat", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(connection);
                Assert.IsTrue(h != null, "heart beat is not initialized");
                Assert.Wait(1000, () => connection.IsClosed, "connection not closed");
            }).Unwrap().GetAwaiter().GetResult();
#endif
        }

        [TestMethod]
        public void ConnectionIdleTimeoutNoHeartbeatTest()
        {
            string testName = "ConnectionIdleTimeoutNoHeartbeatTest";

            Trace.WriteLine(TraceLevel.Information, "sync test");
            {
                Open open = new Open() { ContainerId = testName, HostName = "localhost" };
                Connection connection = new Connection(this.address, null, open, null);
                Session session = new Session(connection);
                SenderLink sender = new SenderLink(session, "sender-" + testName, "any");
                sender.Send(new Message("test") { Properties = new Properties() { MessageId = testName } });
                var h = connection.GetType().GetField("heartBeat", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(connection);
                Assert.IsTrue(h == null, "heart beat should not be initialized");
                connection.Close();
            }

#if !NETFX40
            Trace.WriteLine(TraceLevel.Information, "async test");
            Task.Factory.StartNew(async () =>
            {
                ConnectionFactory factory = new ConnectionFactory();
                factory.AMQP.IdleTimeout = -1;
                Connection connection = await factory.CreateAsync(this.address);
                Session session = new Session(connection);
                SenderLink sender = new SenderLink(session, "sender-" + testName, "any");
                await sender.SendAsync(new Message("test") { Properties = new Properties() { MessageId = testName } });
                var h = connection.GetType().GetField("heartBeat", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(connection);
                Assert.IsTrue(h == null, "heart beat should not be initialized");
                await connection.CloseAsync();
            }).Unwrap().GetAwaiter().GetResult();
#endif
        }

#if !NETFX40
        [TestMethod]
        public void ConnectionIdleTimeoutNegativeValueTest()
        {
            string testName = "ConnectionIdleTimeoutNegativeValueTest";
            uint? timeout = null;
            int value = -1000;

            this.testListener.RegisterTarget(TestPoint.Open, (stream, channel, fields) =>
            {
                timeout = (uint)fields[4];
                return TestOutcome.Continue;
            });

            Task.Factory.StartNew(async () =>
            {
                ConnectionFactory factory = new ConnectionFactory();
                factory.AMQP.IdleTimeout = value;
                Connection connection = await factory.CreateAsync(this.address);
                Session session = new Session(connection);
                SenderLink sender = new SenderLink(session, "sender-" + testName, "any");
                await sender.SendAsync(new Message("test") { Properties = new Properties() { MessageId = testName } });
                await connection.CloseAsync();
            }).Unwrap().GetAwaiter().GetResult();

            Assert.IsTrue(timeout == (uint)value / 2);
        }
#endif

        [TestMethod]
        public void ConnectionHeartBeatCloseTimeoutTest()
        {
            this.testListener.RegisterTarget(TestPoint.Close, (stream, channel, fields) =>
            {
                return TestOutcome.Stop;
            });

            string testName = "ConnectionHeartBeatCloseTimeoutTest";

            typeof(Connection).GetField("HeartBeatCloseTimeout", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, 100);
            bool closeCalled = false;

            Open open = new Open() { ContainerId = testName, IdleTimeOut = 1000, HostName = this.address.Host };
            Connection connection = new Connection(this.address, null, open, null);
            connection.AddClosedCallback((s, e) => closeCalled = true);
            Session session = new Session(connection);
            SenderLink sender = new SenderLink(session, "sender-" + testName, "any");
            sender.Send(new Message("test") { Properties = new Properties() { MessageId = testName } });
            Assert.Wait(5000, () => closeCalled);
        }

        [TestMethod]
        public void SaslMismatchTest()
        {
            this.testListener.RegisterTarget(TestPoint.Header, (stream, channel, fields) =>
            {
                stream.Write(new byte[] { (byte)'A', (byte)'M', (byte)'Q', (byte)'P', 3, 1, 0, 0}, 0, 8);
                stream.Write(new byte[] { (byte)'A', (byte)'M', (byte)'Q', (byte)'P', 0, 1, 0, 0 }, 0, 8);
                TestListener.FRM(stream, 0x10UL, 0, 0, "TestListener", "localhost", 512u);
                TestListener.FRM(stream, 0x18UL, 0, 0);
                return TestOutcome.Stop;
            });

            string testName = "SaslMismatchTest";
            bool failed;

            Trace.WriteLine(TraceLevel.Information, "sync test");
            {
                failed = true;
                try
                {
                    Open open = new Open() { ContainerId = testName, HostName = "localhost", MaxFrameSize = 2048 };
                    Connection connection = new Connection(this.address, null, open, null);
                    connection.Close(TimeSpan.FromSeconds(5));
                    failed = connection.Error != null;
                }
                catch (Exception e)
                {
                    Trace.WriteLine(TraceLevel.Information, "Exception {0}:{1}", e.GetType().Name, e.Message);
                }
                Assert.IsTrue(failed, "should fail");
            }

            Trace.WriteLine(TraceLevel.Information, "async test");
            Task.Factory.StartNew(async () =>
            {
                failed = true;
                try
                {
                    ConnectionFactory factory = new ConnectionFactory();
                    factory.AMQP.MaxFrameSize = 2048;
                    Connection connection = await factory.CreateAsync(this.address);
                    await connection.CloseAsync(TimeSpan.FromSeconds(5));
                    Trace.WriteLine(TraceLevel.Frame, "Error {0}", connection.Error);
                    failed = connection.Error != null;
                }
                catch (Exception e)
                {
                    Trace.WriteLine(TraceLevel.Information, "Exception {0}:{1}", e.GetType().Name, e.Message);
                }
                Assert.IsTrue(failed, "should fail");
            }).Unwrap().GetAwaiter().GetResult();
        }

        [TestMethod]
        public void CloseConnectionWithDetachTest()
        {
            this.testListener.RegisterTarget(TestPoint.Close, (stream, channel, fields) =>
            {
                // send a detach
                TestListener.FRM(stream, 0x16UL, 0, channel, 0u, true);
                return TestOutcome.Continue;
            });

            string testName = "CloseConnectionWithDetachTest";

            Trace.WriteLine(TraceLevel.Information, "sync test");
            {
                Connection connection = new Connection(this.address);
                Session session = new Session(connection);
                SenderLink sender = new SenderLink(session, "sender-" + testName, "any");
                sender.Send(new Message("test") { Properties = new Properties() { MessageId = testName } });
                connection.Close();
                Assert.IsTrue(connection.Error == null, "connection has error!" + connection.Error);
            }

            Trace.WriteLine(TraceLevel.Information, "async test");
            Task.Factory.StartNew(async () =>
            {
                Connection connection = await Connection.Factory.CreateAsync(this.address);
                Session session = new Session(connection);
                SenderLink sender = new SenderLink(session, "sender-" + testName, "any");
                await sender.SendAsync(new Message("test") { Properties = new Properties() { MessageId = testName } });
                await connection.CloseAsync();
                Assert.IsTrue(connection.Error == null, "connection has error!" + connection.Error);

            }).Unwrap().GetAwaiter().GetResult();
        }

        [TestMethod]
        public void CloseConnectionWithEndTest()
        {
            this.testListener.RegisterTarget(TestPoint.Close, (stream, channel, fields) =>
            {
                // send an end
                TestListener.FRM(stream, 0x17UL, 0, channel);
                return TestOutcome.Continue;
            });

            string testName = "CloseConnectionWithEndTest";

            Trace.WriteLine(TraceLevel.Information, "sync test");
            {
                Connection connection = new Connection(this.address);
                Session session = new Session(connection);
                SenderLink sender = new SenderLink(session, "sender-" + testName, "any");
                sender.Send(new Message("test") { Properties = new Properties() { MessageId = testName } });
                connection.Close();
                Assert.IsTrue(connection.Error == null, "connection has error!" + connection.Error);
            }

            Trace.WriteLine(TraceLevel.Information, "async test");
            Task.Factory.StartNew(async () =>
            {
                Connection connection = await Connection.Factory.CreateAsync(this.address);
                Session session = new Session(connection);
                SenderLink sender = new SenderLink(session, "sender-" + testName, "any");
                await sender.SendAsync(new Message("test") { Properties = new Properties() { MessageId = testName } });
                await connection.CloseAsync();
                Assert.IsTrue(connection.Error == null, "connection has error!" + connection.Error);

            }).Unwrap().GetAwaiter().GetResult();
        }

        [TestMethod]
        public void CloseSessionWithDetachTest()
        {
            this.testListener.RegisterTarget(TestPoint.End, (stream, channel, fields) =>
            {
                // send a detach
                TestListener.FRM(stream, 0x16UL, 0, channel, 0u, true);
                return TestOutcome.Continue;
            });

            string testName = "CloseSessionWithDetachTest";

            Trace.WriteLine(TraceLevel.Information, "sync test");
            {
                Connection connection = new Connection(this.address);
                Session session = new Session(connection);
                SenderLink sender = new SenderLink(session, "sender-" + testName, "any");
                sender.Send(new Message("test") { Properties = new Properties() { MessageId = testName } });
                session.Close(TimeSpan.Zero);
                connection.Close();
                Assert.IsTrue(connection.Error == null, "connection has error!" + connection.Error);
            }

            Trace.WriteLine(TraceLevel.Information, "async test");
            Task.Factory.StartNew(async () =>
            {
                Connection connection = await Connection.Factory.CreateAsync(this.address);
                Session session = new Session(connection);
                SenderLink sender = new SenderLink(session, "sender-" + testName, "any");
                await sender.SendAsync(new Message("test") { Properties = new Properties() { MessageId = testName } });
                session.Close(TimeSpan.Zero);
                await connection.CloseAsync();
                Assert.IsTrue(connection.Error == null, "connection has error!" + connection.Error);

            }).Unwrap().GetAwaiter().GetResult();
        }

        [TestMethod]
        public void SendWithConnectionResetTest()
        {
            this.testListener.RegisterTarget(TestPoint.Transfer, (stream, channel, fields) =>
            {
                stream.Dispose();
                return TestOutcome.Continue;
            });

            string testName = "SendWithConnectionResetTest";

            Trace.WriteLine(TraceLevel.Information, "sync test");
            {
                Connection connection = new Connection(this.address);
                Session session = new Session(connection);
                SenderLink sender = new SenderLink(session, "sender-" + testName, "any");
                try
                {
                    sender.Send(new Message("test") { Properties = new Properties() { MessageId = testName } });
                    Assert.IsTrue(false, "Send should throw exception");
                }
                catch (AmqpException exception)
                {
                    Assert.AreEqual(ErrorCode.ConnectionForced, (string)exception.Error.Condition);
                }
                connection.Close();
                Assert.AreEqual(ErrorCode.ConnectionForced, (string)connection.Error.Condition);
            }

            Trace.WriteLine(TraceLevel.Information, "async test");
            Task.Factory.StartNew(async () =>
            {
                Connection connection = await Connection.Factory.CreateAsync(this.address);
                Session session = new Session(connection);
                SenderLink sender = new SenderLink(session, "sender-" + testName, "any");
                try
                {
                    await sender.SendAsync(new Message("test") { Properties = new Properties() { MessageId = testName } });
                    Assert.IsTrue(false, "Send should throw exception");
                }
                catch (AmqpException exception)
                {
                    Assert.AreEqual(ErrorCode.ConnectionForced, (string)exception.Error.Condition);
                }
                await connection.CloseAsync();
                Assert.AreEqual(ErrorCode.ConnectionForced, (string)connection.Error.Condition);
            }).Unwrap().GetAwaiter().GetResult();
        }

        [TestMethod]
        public void SendWithSessionEndTest()
        {
            string errorCode = "test:session-end";
            this.testListener.RegisterTarget(TestPoint.Transfer, (stream, channel, fields) =>
            {
                // end the session
                TestListener.FRM(stream, 0x17UL, 0, channel, new Error(errorCode));
                return TestOutcome.Stop;
            });
            this.testListener.RegisterTarget(TestPoint.End, (stream, channel, fields) =>
            {
                return TestOutcome.Stop;
            });

            string testName = "SendWithSessionEndTest";

            Trace.WriteLine(TraceLevel.Information, "sync test");
            {
                Connection connection = new Connection(this.address);
                Session session = new Session(connection);
                SenderLink sender = new SenderLink(session, "sender-" + testName, "any");
                try
                {
                    sender.Send(new Message("test") { Properties = new Properties() { MessageId = testName } });
                    Assert.IsTrue(false, "Send should throw exception");
                }
                catch (AmqpException exception)
                {
                    Assert.AreEqual(errorCode, (string)exception.Error.Condition);
                }
                connection.Close();
                Assert.AreEqual(errorCode, (string)sender.Error.Condition);
            }

            Trace.WriteLine(TraceLevel.Information, "async test");
            Task.Factory.StartNew(async () =>
            {
                Connection connection = await Connection.Factory.CreateAsync(this.address);
                Session session = new Session(connection);
                SenderLink sender = new SenderLink(session, "sender-" + testName, "any");
                try
                {
                    await sender.SendAsync(new Message("test") { Properties = new Properties() { MessageId = testName } });
                    Assert.IsTrue(false, "Send should throw exception");
                }
                catch (AmqpException exception)
                {
                    Assert.AreEqual(errorCode, (string)exception.Error.Condition);
                }
                await connection.CloseAsync();
                Assert.AreEqual(errorCode, (string)sender.Error.Condition);
            }).Unwrap().GetAwaiter().GetResult();
        }

        [TestMethod]
        public void SendWithLinkDetachTest()
        {
            this.testListener.RegisterTarget(TestPoint.Transfer, (stream, channel, fields) =>
            {
                // detach the link
                TestListener.FRM(stream, 0x16UL, 0, channel, fields[0], true);
                return TestOutcome.Stop;
            });
            this.testListener.RegisterTarget(TestPoint.Detach, (stream, channel, fields) =>
            {
                return TestOutcome.Stop;
            });

            string testName = "SendWithLinkDetachTest";

            Trace.WriteLine(TraceLevel.Information, "sync test");
            {
                Connection connection = new Connection(this.address);
                Session session = new Session(connection);
                SenderLink sender = new SenderLink(session, "sender-" + testName, "any");
                try
                {
                    sender.Send(new Message("test") { Properties = new Properties() { MessageId = testName } });
                    Assert.IsTrue(false, "Send should throw exception");
                }
                catch (AmqpException exception)
                {
                    Assert.AreEqual(ErrorCode.MessageReleased, (string)exception.Error.Condition);
                }
                connection.Close();
            }

            Trace.WriteLine(TraceLevel.Information, "async test");
            Task.Factory.StartNew(async () =>
            {
                Connection connection = await Connection.Factory.CreateAsync(this.address);
                Session session = new Session(connection);
                SenderLink sender = new SenderLink(session, "sender-" + testName, "any");
                try
                {
                    await sender.SendAsync(new Message("test") { Properties = new Properties() { MessageId = testName } });
                    Assert.IsTrue(false, "Send should throw exception");
                }
                catch (AmqpException exception)
                {
                    Assert.AreEqual(ErrorCode.MessageReleased, (string)exception.Error.Condition);
                }
                await connection.CloseAsync();
            }).Unwrap().GetAwaiter().GetResult();
        }

        [TestMethod]
        public void SendTimeoutTest()
        {
            this.testListener.RegisterTarget(TestPoint.Transfer, (stream, channel, fields) =>
            {
                return TestOutcome.Stop;
            });

            string testName = "SendTimeoutTest";
            TimeSpan timeout = TimeSpan.FromMilliseconds(600);

            Trace.WriteLine(TraceLevel.Information, "sync test");
            {
                Connection connection = new Connection(this.address);
                Session session = new Session(connection);
                SenderLink sender = new SenderLink(session, "sender-" + testName, "any");
                try
                {
                    sender.Send(new Message("test") { Properties = new Properties() { MessageId = testName } }, timeout);
                    Assert.IsTrue(false, "Send should throw exception");
                }
                catch (TimeoutException)
                {
                }
                connection.Close();
            }

            Trace.WriteLine(TraceLevel.Information, "async test");
            Task.Factory.StartNew(async () =>
            {
                Connection connection = await Connection.Factory.CreateAsync(this.address);
                Session session = new Session(connection);
                SenderLink sender = new SenderLink(session, "sender-" + testName, "any");
                try
                {
                    await sender.SendAsync(new Message("test") { Properties = new Properties() { MessageId = testName } }, timeout);
                    Assert.IsTrue(false, "Send should throw exception");
                }
                catch (TimeoutException)
                {
                }
                await connection.CloseAsync();
            }).Unwrap().GetAwaiter().GetResult();
        }

        [TestMethod]
        public void SmallSessionWindowTest()
        {
            ManualResetEvent done = new ManualResetEvent(false);

            int window = 37;
            int total = 8000;
            int received = 0;

            this.testListener.WindowSize = (uint)window;
            this.testListener.LinkCredit = 2000u;
            this.testListener.RegisterTarget(TestPoint.Begin, (stream, channel, fields) =>
            {
                TestListener.FRM(stream, 0x11UL, 0, channel, channel, 0u, (uint)window, 65536u, 8u);
                return TestOutcome.Stop;
            });

            this.testListener.RegisterTarget(TestPoint.Transfer, (stream, channel, fields) =>
            {
                received++;
                if (received % window == 0)
                {
                    TestListener.FRM(stream, 0x13UL, 0, channel, (uint)received, (uint)window, 0u, 65536u);
                }

                if (received >= total)
                {
                    done.Set();
                }

                return TestOutcome.Stop;
            });

            string testName = "SmallSessionWindowTest";

            Connection connection = new Connection(this.address);
            Session session = new Session(connection);
            SenderLink[] senders = new SenderLink[8];
            for (int i = 0; i < senders.Length; i++)
            {
                senders[i] = new SenderLink(session, "sender:" + i, testName);
            }

            for (int i = 0; i < total; i++)
            {
                senders[i % senders.Length].Send(new Message("message" + i), null, null);
            }

            Assert.IsTrue(done.WaitOne(10000), $"Transferred {received} out of {total} messages");
            connection.Close();
        }

        [TestMethod]
        public void SmallSessionWindowWithLinkCloseTest()
        {
            ManualResetEvent done = new ManualResetEvent(false);
            ManualResetEvent sendDone = new ManualResetEvent(false);

            int window = 43;
            int total = 8000;
            int received = 0;
            int success = 0;
            int cancel = 0;

            this.testListener.WindowSize = (uint)window;
            this.testListener.LinkCredit = 4000u;
            this.testListener.RegisterTarget(TestPoint.Begin, (stream, channel, fields) =>
            {
                TestListener.FRM(stream, 0x11UL, 0, channel, channel, 0u, (uint)window, 65536u, 8u);
                return TestOutcome.Stop;
            });

            this.testListener.RegisterTarget(TestPoint.Detach, (stream, channel, fields) =>
            {
                return TestOutcome.Stop;
            });

            this.testListener.RegisterTarget(TestPoint.Transfer, (stream, channel, fields) =>
            {
                received++;
                TestListener.FRM(stream, 0x15UL, 0, channel, true, fields[1], null, true, new Accepted());
                if (received % window == 0)
                {
                    TestListener.FRM(stream, 0x13UL, 0, channel, (uint)received, (uint)window, 0u, 65536u);
                }
                if (received >= 3000 && received % 1000 == 0)
                {
                    sendDone.WaitOne();
                    TestListener.FRM(stream, 0x16UL, 0, channel, (uint)((received / 1000) - 3), true);
                }
                return TestOutcome.Stop;
            });

            string testName = "SmallSessionWindowWithLinkCloseTest";

            Connection connection = new Connection(this.address);
            Session session = new Session(connection);
            SenderLink[] senders = new SenderLink[8];
            for (int i = 0; i < senders.Length; i++)
            {
                senders[i] = new SenderLink(session, "sender:" + i, testName);
            }

            for (int i = 0; i < total; i++)
            {
                senders[i % senders.Length].Send(
                    new Message("message" + i),
                    (s, m, o, t) => {
                        if (o.Descriptor.Code == 0x24ul) success++; else cancel++;
                        if (success + cancel >= total) done.Set();
                    },
                    null);
            }

            sendDone.Set();

            Assert.IsTrue(done.WaitOne(10000), $"total:{total} received:{received} success:{success} fail:{cancel}");

            connection.Close();
        }

        [TestMethod]
        public void ClosedEventOnTransportResetTest()
        {
            this.testListener.RegisterTarget(TestPoint.Begin, (stream, channel, fields) =>
            {
                stream.Dispose();
                return TestOutcome.Stop;
            });

            Trace.WriteLine(TraceLevel.Information, "sync test");
            {
                ManualResetEvent closed = new ManualResetEvent(false);
                Connection connection = new Connection(this.address);
                connection.Closed += (o, e) => closed.Set();
                Session session = new Session(connection);
                Assert.IsTrue(closed.WaitOne(5000), "closed event not fired");
                Assert.AreEqual(ErrorCode.ConnectionForced, (string)connection.Error.Condition);
            }

            Trace.WriteLine(TraceLevel.Information, "async test");
            Task.Factory.StartNew(async () =>
            {
                ManualResetEvent closed = new ManualResetEvent(false);
                Connection connection = await Connection.Factory.CreateAsync(this.address);
                connection.Closed += (o, e) => closed.Set();
                Session session = new Session(connection);
                await Task.Factory.StartNew(o => ((ManualResetEvent)o).WaitOne(5000), closed);
                Assert.IsTrue(closed.WaitOne(10), "closed event not fired");
                Assert.AreEqual(ErrorCode.ConnectionForced, (string)connection.Error.Condition);
            }).Unwrap().GetAwaiter().GetResult();
        }

#if !NETFX40
        [TestMethod]
        public void CloseLinkTimeoutTest()
        {
            this.testListener.RegisterTarget(TestPoint.Detach, (stream, channel, fields) =>
            {
                return TestOutcome.Stop;
            });

            string testName = "CloseLinkTimeoutTest";

            Trace.WriteLine(TraceLevel.Information, "sync test");
            {
                Connection connection = new Connection(this.address);
                Session session = new Session(connection);
                SenderLink sender = new SenderLink(session, "sender-" + testName, "any");
                sender.Send(new Message("test") { Properties = new Properties() { MessageId = testName } });
                try
                {
                    sender.Close(TimeSpan.FromSeconds(1));
                    Assert.IsTrue(false, "timeout exception expected");
                }
                catch (TimeoutException) { }
                connection.Close();
            }

            Trace.WriteLine(TraceLevel.Information, "async test");
            Task.Factory.StartNew(async () =>
            {
                Connection connection = await Connection.Factory.CreateAsync(this.address);
                Session session = new Session(connection);
                SenderLink sender = new SenderLink(session, "sender-" + testName, "any");
                await sender.SendAsync(new Message("test") { Properties = new Properties() { MessageId = testName } });
                try
                {
                    await sender.CloseAsync(TimeSpan.FromSeconds(1), null);
                    Assert.IsTrue(false, "timeout exception expected");
                }
                catch (TimeoutException) { }
                await connection.CloseAsync();
            }).Unwrap().GetAwaiter().GetResult();
        }
#endif

        [TestMethod]
        public void CloseLinkLocalErrorTest()
        {
            this.testListener.RegisterTarget(TestPoint.Detach, (stream, channel, fields) =>
            {
                // detach without error
                TestListener.FRM(stream, 0x16UL, 0, channel, fields[0], true);
                return TestOutcome.Stop;
            });

            string testName = "CloseLinkLocalErrorTest";

            Trace.WriteLine(TraceLevel.Information, "sync test");
            {
                Connection connection = new Connection(this.address);
                Session session = new Session(connection);
                SenderLink sender = new SenderLink(session, "sender-" + testName, "any");
                sender.Send(new Message("test") { Properties = new Properties() { MessageId = testName } });
                sender.Close(TimeSpan.FromSeconds(60), new Error(ErrorCode.NotImplemented));
                connection.Close();
            }

            Trace.WriteLine(TraceLevel.Information, "async test");
            Task.Factory.StartNew(async () =>
            {
                Connection connection = await Connection.Factory.CreateAsync(this.address);
                Session session = new Session(connection);
                SenderLink sender = new SenderLink(session, "sender-" + testName, "any");
                await sender.SendAsync(new Message("test") { Properties = new Properties() { MessageId = testName } });
                await sender.CloseAsync(TimeSpan.FromSeconds(60), new Error(ErrorCode.NotImplemented));
                await connection.CloseAsync();
            }).Unwrap().GetAwaiter().GetResult();
        }

        [TestMethod]
        public void CloseLinkRemoteErrorTest()
        {
            this.testListener.RegisterTarget(TestPoint.Detach, (stream, channel, fields) =>
            {
                // detach with error
                TestListener.FRM(stream, 0x16UL, 0, channel, fields[0], true, new Error(ErrorCode.InternalError));
                return TestOutcome.Stop;
            });

            string testName = "CloseLinkRemoteErrorTest";

            Trace.WriteLine(TraceLevel.Information, "sync test");
            {
                Connection connection = new Connection(this.address);
                Session session = new Session(connection);
                SenderLink sender = new SenderLink(session, "sender-" + testName, "any");
                sender.Send(new Message("test") { Properties = new Properties() { MessageId = testName } });
                try
                {
                    sender.Close();
                    Assert.IsTrue(false, "exception expected");
                }
                catch (AmqpException) { }
                connection.Close();
            }

            Trace.WriteLine(TraceLevel.Information, "async test");
            Task.Factory.StartNew(async () =>
            {
                Connection connection = await Connection.Factory.CreateAsync(this.address);
                Session session = new Session(connection);
                SenderLink sender = new SenderLink(session, "sender-" + testName, "any");
                await sender.SendAsync(new Message("test") { Properties = new Properties() { MessageId = testName } });
                try
                {
                    await sender.CloseAsync();
                    Assert.IsTrue(false, "exception expected");
                }
                catch (AmqpException) { }
                await connection.CloseAsync();
            }).Unwrap().GetAwaiter().GetResult();
        }

        [TestMethod]
        public void DetachLinkTest()
        {
            this.testListener.RegisterTarget(TestPoint.Detach, (stream, channel, fields) =>
            {
                TestListener.FRM(stream, 0x16UL, 0, channel, fields[0], false);
                return TestOutcome.Stop;
            });

            string testName = "DetachLinkTest";

            Trace.WriteLine(TraceLevel.Information, "sync test");
            {
                Connection connection = new Connection(this.address);
                Session session = new Session(connection);
                SenderLink sender = new SenderLink(session, "sender-" + testName, "any");
                sender.Send(new Message("test") { Properties = new Properties() { MessageId = testName } });
                sender.Detach();
                connection.Close();
            }

            Trace.WriteLine(TraceLevel.Information, "async test");
            Task.Factory.StartNew(async () =>
            {
                Connection connection = await Connection.Factory.CreateAsync(this.address);
                Session session = new Session(connection);
                SenderLink sender = new SenderLink(session, "sender-" + testName, "any");
                await sender.SendAsync(new Message("test") { Properties = new Properties() { MessageId = testName } });
                await sender.DetachAsync();
                await connection.CloseAsync();
            }).Unwrap().GetAwaiter().GetResult();
        }

        [TestMethod]
        public void DetachLinkRemoteErrorTest()
        {
            this.testListener.RegisterTarget(TestPoint.Detach, (stream, channel, fields) =>
            {
                TestListener.FRM(stream, 0x16UL, 0, channel, fields[0], false, new Error(ErrorCode.InternalError));
                return TestOutcome.Stop;
            });

            string testName = "DetachLinkRemoteErrorTest";

            Trace.WriteLine(TraceLevel.Information, "sync test");
            {
                Connection connection = new Connection(this.address);
                Session session = new Session(connection);
                SenderLink sender = new SenderLink(session, "sender-" + testName, "any");
                sender.Send(new Message("test") { Properties = new Properties() { MessageId = testName } });
                try
                {
                    sender.Detach();
                    Assert.IsTrue(false, "exception expected");
                }
                catch (AmqpException) { }
                connection.Close();
            }

            Trace.WriteLine(TraceLevel.Information, "async test");
            Task.Factory.StartNew(async () =>
            {
                Connection connection = await Connection.Factory.CreateAsync(this.address);
                Session session = new Session(connection);
                SenderLink sender = new SenderLink(session, "sender-" + testName, "any");
                await sender.SendAsync(new Message("test") { Properties = new Properties() { MessageId = testName } });
                try
                {
                    await sender.DetachAsync();
                    Assert.IsTrue(false, "exception expected");
                }
                catch (AmqpException) { }
                await connection.CloseAsync();
            }).Unwrap().GetAwaiter().GetResult();
        }

        [TestMethod]
        public void DetachLinkRemoteCloseTest()
        {
            this.testListener.RegisterTarget(TestPoint.Detach, (stream, channel, fields) =>
            {
                TestListener.FRM(stream, 0x16UL, 0, channel, fields[0], true);
                return TestOutcome.Stop;
            });

            string testName = "DetachLinkRemoteCloseTest";

            Trace.WriteLine(TraceLevel.Information, "sync test");
            {
                Connection connection = new Connection(this.address);
                Session session = new Session(connection);
                SenderLink sender = new SenderLink(session, "sender-" + testName, "any");
                sender.Send(new Message("test") { Properties = new Properties() { MessageId = testName } });
                try
                {
                    sender.Detach();
                    Assert.IsTrue(false, "exception expected");
                }
                catch (AmqpException) { }
                connection.Close();
            }

            Trace.WriteLine(TraceLevel.Information, "async test");
            Task.Factory.StartNew(async () =>
            {
                Connection connection = await Connection.Factory.CreateAsync(this.address);
                Session session = new Session(connection);
                SenderLink sender = new SenderLink(session, "sender-" + testName, "any");
                await sender.SendAsync(new Message("test") { Properties = new Properties() { MessageId = testName } });
                try
                {
                    await sender.DetachAsync();
                    Assert.IsTrue(false, "exception expected");
                }
                catch (AmqpException) { }
                await connection.CloseAsync();
            }).Unwrap().GetAwaiter().GetResult();
        }

        [TestMethod]
        public void ClosedCallbackGuaranteeTest()
        {
            this.testListener.RegisterTarget(TestPoint.Open, (stream, channel, fields) =>
            {
                TestListener.FRM(stream, 0x10UL, 0, 0, "Test"); // open
                TestListener.FRM(stream, 0x18UL, 0, channel, new Error(ErrorCode.UnauthorizedAccess)); // close
                return TestOutcome.Stop;
            });

            Trace.WriteLine(TraceLevel.Information, "sync test");
            {
                ManualResetEvent closed = new ManualResetEvent(false);
                Connection connection = new Connection(this.address);
                connection.AddClosedCallback((o, e) => closed.Set());
                Assert.IsTrue(closed.WaitOne(5000), "closed event not fired");
                Assert.AreEqual(ErrorCode.UnauthorizedAccess, (string)connection.Error.Condition);
                closed.Reset();
                connection.AddClosedCallback((o, e) => closed.Set());
                Assert.IsTrue(closed.WaitOne(5000), "closed event not fired again");
            }

            Trace.WriteLine(TraceLevel.Information, "async test");
            Task.Factory.StartNew(async () =>
            {
                ManualResetEvent closed = new ManualResetEvent(false);
                Connection connection = await Connection.Factory.CreateAsync(this.address);
                connection.AddClosedCallback((o, e) => closed.Set());
                await Task.Factory.StartNew(o => ((ManualResetEvent)o).WaitOne(5000), closed);
                Assert.IsTrue(closed.WaitOne(10), "closed event not fired");
                Assert.AreEqual(ErrorCode.UnauthorizedAccess, (string)connection.Error.Condition);
                closed.Reset();
                connection.AddClosedCallback((o, e) => closed.Set());
                Assert.IsTrue(closed.WaitOne(5000), "closed event not fired again");
            }).Unwrap().GetAwaiter().GetResult();
        }

        [TestMethod]
        public void SaslInvalidProtocolHeaderTest()
        {
            Stream transport = null;

            this.testListener.RegisterTarget(TestPoint.SaslHeader, (stream, channel, fields) =>
            {
                transport = stream;
                stream.WriteByte(3);    // inject an extra byte
                return TestOutcome.Continue;
            });

            Address myAddress = new Address("amqp://guest:@" + this.address.Host + ":" + this.address.Port);
            Trace.WriteLine(TraceLevel.Information, "sync test");
            {
                try
                {
                    Connection connection = new Connection(myAddress);
                    Assert.IsTrue(false, "no exception was thrown 1");
                }
                catch (AmqpException) { }
                Assert.IsTrue(transport != null, "transport is null");
                try
                {
                    for (int i = 0; i < 5; i++)
                    {
                        transport.WriteByte(1);
                        Thread.Sleep(100);
                    }
                    Assert.IsTrue(false, "transport not disposed 1.");
                }
                catch (ObjectDisposedException) { }
                catch (IOException) { }
            }

            Trace.WriteLine(TraceLevel.Information, "async test");
            transport = null;
            Task.Factory.StartNew(async () =>
            {
                try
                {
                    Connection connection = await Connection.Factory.CreateAsync(myAddress);
                }
                catch (AmqpException) { }
                Assert.IsTrue(transport != null, "transport is null 2");
                try
                {
                    for (int i = 0; i < 5; i++)
                    {
                        transport.WriteByte(2);
                        Thread.Sleep(100);
                    }
                    Assert.IsTrue(false, "transport not disposed 2.");
                }
                catch (ObjectDisposedException) { }
                catch (IOException) { }
            }).Unwrap().GetAwaiter().GetResult();
        }

        [TestMethod]
        public void SaslCloseTransportTest()
        {
            this.testListener.RegisterTarget(TestPoint.SaslHeader, (stream, channel, fields) =>
            {
                stream.Dispose();
                return TestOutcome.Stop;
            });

            Address myAddress = new Address("amqp://guest:@" + this.address.Host + ":" + this.address.Port);
            Trace.WriteLine(TraceLevel.Information, "sync test");
            {
                try
                {
                    Connection connection = new Connection(myAddress);
                    Assert.IsTrue(false, "no exception was thrown 1");
                }
                catch (OperationCanceledException) { }
            }

            Trace.WriteLine(TraceLevel.Information, "async test");
            Task.Factory.StartNew(async () =>
            {
                try
                {
                    Connection connection = await Connection.Factory.CreateAsync(myAddress);
                }
                catch (OperationCanceledException) { }
            }).Unwrap().GetAwaiter().GetResult();
        }

        [TestMethod]
        public void SendWithInvalidRemoteChannelTest()
        {
            this.testListener.RegisterTarget(TestPoint.Transfer, (stream, channel, fields) =>
            {
                // send an end with invalid channel
                TestListener.FRM(stream, 0x17UL, 0, 33);
                return TestOutcome.Stop;
            });

            string testName = "SendWithProtocolErrorTest";

            Trace.WriteLine(TraceLevel.Information, "sync test");
            {
                Connection connection = new Connection(this.address);
                Session session = new Session(connection);
                SenderLink sender = new SenderLink(session, "sender-" + testName, "any");
                try
                {
                    sender.Send(new Message("test") { Properties = new Properties() { MessageId = testName } });
                    Assert.IsTrue(false, "Send should throw exception");
                }
                catch (AmqpException exception)
                {
                    Assert.AreEqual(ErrorCode.NotFound, (string)exception.Error.Condition);
                }
                connection.Close();
                Assert.AreEqual(ErrorCode.NotFound, (string)connection.Error.Condition);
            }

            Trace.WriteLine(TraceLevel.Information, "async test");
            Task.Factory.StartNew(async () =>
            {
                Connection connection = await Connection.Factory.CreateAsync(this.address);
                Session session = new Session(connection);
                SenderLink sender = new SenderLink(session, "sender-" + testName, "any");
                try
                {
                    await sender.SendAsync(new Message("test") { Properties = new Properties() { MessageId = testName } });
                    Assert.IsTrue(false, "Send should throw exception");
                }
                catch (AmqpException exception)
                {
                    Assert.AreEqual(ErrorCode.NotFound, (string)exception.Error.Condition);
                }
                await connection.CloseAsync();
                Assert.AreEqual(ErrorCode.NotFound, (string)connection.Error.Condition);
            }).Unwrap().GetAwaiter().GetResult();
        }

        [TestMethod]
        public void ReceiveWithConnectionResetTest()
        {
            this.testListener.RegisterTarget(TestPoint.Flow, (stream, channel, fields) =>
            {
                stream.Dispose();
                return TestOutcome.Continue;
            });

            string testName = "ReceiveWithConnectionResetTest";

            Trace.WriteLine(TraceLevel.Information, "sync test");
            {
                Connection connection = new Connection(this.address);
                Session session = new Session(connection);
                ReceiverLink receiver = new ReceiverLink(session, "receiver-" + testName, "any");
                try
                {
                    receiver.Receive();
                    Assert.IsTrue(false, "Receive should fail with error");
                }
                catch (AmqpException exception)
                {
                    Assert.AreEqual((Symbol)ErrorCode.ConnectionForced, exception.Error.Condition);
                }
                connection.Close();
                Assert.AreEqual(ErrorCode.ConnectionForced, (string)connection.Error.Condition);
            }

            Trace.WriteLine(TraceLevel.Information, "async test");
            Task.Factory.StartNew(async () =>
            {
                Connection connection = await Connection.Factory.CreateAsync(this.address);
                Session session = new Session(connection);
                ReceiverLink receiver = new ReceiverLink(session, "receiver-" + testName, "any");
                try
                {
                    await receiver.ReceiveAsync();
                    Assert.IsTrue(false, "Receive should fail with error");
                }
                catch (AmqpException exception)
                {
                    Assert.AreEqual((Symbol)ErrorCode.ConnectionForced, exception.Error.Condition);
                }
                await connection.CloseAsync();
                Assert.AreEqual(ErrorCode.ConnectionForced, (string)connection.Error.Condition);
            }).Unwrap().GetAwaiter().GetResult();
        }

        [TestMethod]
        public void ReceiveWithSessionEndTest()
        {
            this.testListener.RegisterTarget(TestPoint.Flow, (stream, channel, fields) =>
            {
                // end the session
                TestListener.FRM(stream, 0x17UL, 0, channel);
                return TestOutcome.Stop;
            });
            this.testListener.RegisterTarget(TestPoint.End, (stream, channel, fields) =>
            {
                return TestOutcome.Stop;
            });

            string testName = "ReceiveWithSessionCloseTest";

            Trace.WriteLine(TraceLevel.Information, "sync test");
            {
                Connection connection = new Connection(this.address);
                Session session = new Session(connection);
                ReceiverLink receiver = new ReceiverLink(session, "receiver-" + testName, "any");
                try
                {
                    receiver.Receive();
                    Assert.IsTrue(false, "Receive should fail with error");
                }
                catch (AmqpException exception)
                {
                    Assert.AreEqual((Symbol)ErrorCode.DetachForced, exception.Error.Condition);
                }
                connection.Close();
                Assert.AreEqual((Symbol)ErrorCode.DetachForced, receiver.Error.Condition);
            }

            Trace.WriteLine(TraceLevel.Information, "async test");
            Task.Factory.StartNew(async () =>
            {
                Connection connection = await Connection.Factory.CreateAsync(this.address);
                Session session = new Session(connection);
                ReceiverLink receiver = new ReceiverLink(session, "receiver-" + testName, "any");
                try
                {
                    await receiver.ReceiveAsync();
                    Assert.IsTrue(false, "Receive should fail with error");
                }
                catch (AmqpException exception)
                {
                    Assert.AreEqual((Symbol)ErrorCode.DetachForced, exception.Error.Condition);
                }
                await connection.CloseAsync();
                Assert.AreEqual((Symbol)ErrorCode.DetachForced, receiver.Error.Condition);
            }).Unwrap().GetAwaiter().GetResult();
        }

        [TestMethod]
        public void ReceiveWithLinkDetachErrorTest()
        {
            this.testListener.RegisterTarget(TestPoint.Flow, (stream, channel, fields) =>
            {
                // detach link with error. receive calls should throw
                TestListener.FRM(stream, 0x16UL, 0, channel, fields[0], true, new Error(ErrorCode.InternalError));
                return TestOutcome.Stop;
            });
            this.testListener.RegisterTarget(TestPoint.Detach, (stream, channel, fields) =>
            {
                return TestOutcome.Stop;
            });

            string testName = "ReceiveWithLinkDetachErrorTest";

            Trace.WriteLine(TraceLevel.Information, "sync test");
            {
                Connection connection = new Connection(this.address);
                Session session = new Session(connection);
                ReceiverLink receiver = new ReceiverLink(session, "receiver-" + testName, "any");
                try
                {
                    receiver.Receive();
                    Assert.IsTrue(false, "Receive should fail with error");
                }
                catch (AmqpException exception)
                {
                    Assert.AreEqual((Symbol)ErrorCode.InternalError, exception.Error.Condition);
                }
                connection.Close();
                Assert.AreEqual((Symbol)ErrorCode.InternalError, receiver.Error.Condition);
            }

            Trace.WriteLine(TraceLevel.Information, "async test");
            Task.Factory.StartNew(async () =>
            {
                Connection connection = await Connection.Factory.CreateAsync(this.address);
                Session session = new Session(connection);
                ReceiverLink receiver = new ReceiverLink(session, "receiver-" + testName, "any");
                try
                {
                    await receiver.ReceiveAsync();
                    Assert.IsTrue(false, "Receive should fail with error");
                }
                catch (AmqpException exception)
                {
                    Assert.AreEqual((Symbol)ErrorCode.InternalError, exception.Error.Condition);
                }
                await connection.CloseAsync();
                Assert.AreEqual((Symbol)ErrorCode.InternalError, receiver.Error.Condition);
            }).Unwrap().GetAwaiter().GetResult();
        }

        [TestMethod]
        public void ReceiveWithLinkDetachTest()
        {
            this.testListener.RegisterTarget(TestPoint.Flow, (stream, channel, fields) =>
            {
                // detach link without error. receivers should return null (eof)
                TestListener.FRM(stream, 0x16UL, 0, channel, fields[0], true);
                return TestOutcome.Stop;
            });
            this.testListener.RegisterTarget(TestPoint.Detach, (stream, channel, fields) =>
            {
                return TestOutcome.Stop;
            });

            string testName = "ReceiveWithLinkDetachTest";

            Trace.WriteLine(TraceLevel.Information, "sync test");
            {
                Connection connection = new Connection(this.address);
                Session session = new Session(connection);
                ReceiverLink receiver = new ReceiverLink(session, "receiver-" + testName, "any");
                DateTime dt = DateTime.UtcNow;
                var message = receiver.Receive();
                Assert.IsTrue(message == null);
                connection.Close();
                Assert.IsTrue(DateTime.UtcNow.Subtract(dt).TotalMilliseconds < 10000, "receive should return right away");
            }

            Trace.WriteLine(TraceLevel.Information, "async test");
            Task.Factory.StartNew(async () =>
            {
                Connection connection = await Connection.Factory.CreateAsync(this.address);
                Session session = new Session(connection);
                ReceiverLink receiver = new ReceiverLink(session, "receiver-" + testName, "any");
                DateTime dt = DateTime.UtcNow;
                var message = await receiver.ReceiveAsync();
                Assert.IsTrue(message == null);
                await connection.CloseAsync();
                Assert.IsTrue(DateTime.UtcNow.Subtract(dt).TotalMilliseconds < 10000, "receive should return right away");
            }).Unwrap().GetAwaiter().GetResult();
        }

        [TestMethod]
        public void ReceiveWithNoCreditTest()
        {
            this.testListener.RegisterTarget(TestPoint.Attach, (stream, channel, fields) =>
            {
                bool role = !(bool)fields[2];
                TestListener.FRM(stream, 0x12UL, 0, channel, fields[0], fields[1], role, fields[3], fields[4], new Source(), new Target());
                TestListener.FRM(stream, 0x14UL, 0, channel, fields[1], 0u, new byte[0], 0u, true, false);  // transfer
                return TestOutcome.Stop;
            });

            string testName = "ReceiveWithNoCreditTest";

            Trace.WriteLine(TraceLevel.Information, "sync test");
            {
                ManualResetEvent closed = new ManualResetEvent(false);
                Connection connection = new Connection(this.address);
                connection.Closed += (s, a) => closed.Set();
                Session session = new Session(connection);
                ReceiverLink receiver = new ReceiverLink(session, "receiver-" + testName, "any");
                Assert.IsTrue(closed.WaitOne(5000), "Connection not closed");
                Assert.AreEqual(ErrorCode.TransferLimitExceeded, (string)connection.Error.Condition);
                Assert.IsTrue(receiver.IsClosed);
            }

            Trace.WriteLine(TraceLevel.Information, "async test");
            Task.Factory.StartNew(async () =>
            {
                ManualResetEvent closed = new ManualResetEvent(false);
                Connection connection = await Connection.Factory.CreateAsync(this.address);
                connection.Closed += (s, a) => closed.Set();
                Session session = new Session(connection);
                ReceiverLink receiver = new ReceiverLink(session, "receiver-" + testName, "any");
                await Task.Factory.StartNew(o => ((ManualResetEvent)o).WaitOne(5000), closed);
                Assert.IsTrue(closed.WaitOne(10), "Connection not closed");
                Assert.AreEqual(ErrorCode.TransferLimitExceeded, (string)connection.Error.Condition);
                Assert.IsTrue(receiver.IsClosed);
            }).Unwrap().GetAwaiter().GetResult();
        }

        [TestMethod]
        public void ReceiverLinkCreditRestoreTest()
        {
            uint total = 0;

            this.testListener.RegisterTarget(TestPoint.Flow, (stream, channel, fields) =>
            {
                uint current = total;
                total = (uint)(fields[5] ?? 0u) + (uint)fields[6];
                for (uint i = current; i < total; i++)
                {
                    TestListener.FRM(stream, 0x14UL, 0, channel, fields[4], i, BitConverter.GetBytes(i), 0u, false, false);  // transfer
                }

                return TestOutcome.Stop;
            });

            string testName = "ReceiverLinkCreditRestoreTest";

            Connection connection = new Connection(this.address);
            Session session = new Session(connection);
            ReceiverLink receiver = new ReceiverLink(session, "receiver-" + testName, "any");
            receiver.SetCredit(10);
            for (int i = 0; i < 10; i++)
            {
                Message message = receiver.Receive();
                if (i % 2 == 0)
                {
                    receiver.Accept(message);
                }
            }

            receiver.Receive();

            Assert.AreEqual(15u, total);    // initial 10 + 5 accept calls
            connection.Close();
        }

        [TestMethod]
        public void ReceiverLinkSetCreditAutoTest()
        {
            uint total = 0;

            this.testListener.RegisterTarget(TestPoint.Flow, (stream, channel, fields) =>
            {
                uint current = total;
                total = (uint)(fields[5] ?? 0u) + (uint)fields[6];
                for (uint i = current; i < total; i++)
                {
                    TestListener.FRM(stream, 0x14UL, 0, channel, fields[4], i, BitConverter.GetBytes(i), 0u, false, false);  // transfer
                }

                return TestOutcome.Stop;
            });

            string testName = "ReceiverLinkSetCreditAutoTest";

            Connection connection = new Connection(this.address);
            Session session = new Session(connection);
            ReceiverLink receiver = new ReceiverLink(session, "receiver-" + testName, "any");
            int credit = 4;
            int received = 0;
            int pending = 0;
            receiver.SetCredit(credit);
            for (int i = 0; i < 3000; i++)
            {
                Message message = receiver.Receive();
                Interlocked.Increment(ref pending);
                Task.Factory.StartNew(() =>
                {
                    receiver.Accept(message);
                    if (Interlocked.Increment(ref received) % credit == 0)
                    {
                        receiver.SetCredit(credit);
                    }

                    Interlocked.Decrement(ref pending);
                });
            }

            while (pending > 0)
            {
                Thread.Sleep(10);
            }

            connection.Close();
        }

        [TestMethod]
        public void ReceiverLinkStoppingTest()
        {
            uint total = 0;

            this.testListener.RegisterTarget(TestPoint.Flow, (stream, channel, fields) =>
            {
                uint current = total;
                uint limit = (uint)(fields[5] ?? 0u) + (uint)fields[6];
                if (limit > total)
                {
                    total = limit;
                }
                for (uint i = current; i < limit; i++)
                {
                    TestListener.FRM(stream, 0x14UL, 0, channel, fields[4], i, BitConverter.GetBytes(i), 0u, false, false);  // transfer
                }
                return TestOutcome.Stop;
            });

            string testName = "ReceiverLinkStoppingTest";

            Connection connection = new Connection(this.address);
            Session session = new Session(connection);
            ReceiverLink receiver = new ReceiverLink(session, "receiver-" + testName, "any");
            receiver.SetCredit(10);
            receiver.Accept(receiver.Receive());
            receiver.SetCredit(0);
            for (int i = 0; i < 9; i++)
            {
                receiver.Accept(receiver.Receive());
            }

            Message message = receiver.Receive(TimeSpan.FromSeconds(1));
            Assert.IsTrue(message == null);

            Assert.AreEqual(10u, total);

            connection.Close();
        }

        [TestMethod]
        public void ReceiverLinkCreditReduceTest()
        {
            uint total = 0;
            uint id = 0;

            this.testListener.RegisterTarget(TestPoint.Flow, (stream, channel, fields) =>
            {
                uint current = total;
                total = Math.Max(total, (uint)(fields[5] ?? id) + (uint)fields[6]);
                for (uint i = current; i < total; i++)
                {
                    TestListener.FRM(stream, 0x14UL, 0, channel, fields[4], id, BitConverter.GetBytes(id), 0u, false, false);  // transfer
                    id++;
                }
                return TestOutcome.Stop;
            });

            string testName = "ReceiverLinkCreditReduceTest";

            Connection connection = new Connection(this.address);
            Session session = new Session(connection);
            ReceiverLink receiver = new ReceiverLink(session, "receiver-" + testName, "any");
            receiver.SetCredit(10);
            for (int i = 0; i < 4; i++)
            {
                receiver.Accept(receiver.Receive());
            }

            receiver.SetCredit(4);

            // should get at least 6 more
            for (int i = 0; i < 6; i++)
            {
                receiver.Accept(receiver.Receive());
            }

            Assert.IsTrue(total >= 10u, "total " + total);

            connection.Close();
        }

        [TestMethod]
        public void ReceiverLinkAutoCreditLastTest()
        {
            uint total = 0;
            uint id = 0;

            this.testListener.RegisterTarget(TestPoint.Flow, (stream, channel, fields) =>
            {
                uint current = total;
                total = Math.Max(total, (uint)(fields[5] ?? id) + (uint)fields[6]);
                for (uint i = current; i < total; i++)
                {
                    TestListener.FRM(stream, 0x14UL, 0, channel, fields[4], id, BitConverter.GetBytes(id), 0u, false, false);  // transfer
                    id++;
                }
                return TestOutcome.Stop;
            });

            string testName = "ReceiverLinkAutoCreditLastTest";

            Connection connection = new Connection(this.address);
            Session session = new Session(connection);
            ReceiverLink receiver = new ReceiverLink(session, "receiver-" + testName, "any");
            receiver.SetCredit(10);
            List<Message> messages = new List<Message>();
            for (int i = 0; i < 10; i++)
            {
                messages.Add(receiver.Receive());
            }

            receiver.Accept(messages[9]);
            receiver.SetCredit(10);
            Message msg = receiver.Receive();
            Assert.IsTrue(msg != null);
            Assert.AreEqual(11u, total);

            connection.Close();
        }

        [TestMethod]
        public void ReceiverLinkManualCreditTest()
        {
            uint id = 0;
            uint available = 0;

            this.testListener.RegisterTarget(TestPoint.Flow, (stream, channel, fields) =>
            {
                uint credit = (uint)fields[6];
                for (uint i = 0; i < available; i++)
                {
                    TestListener.FRM(stream, 0x14UL, 0, channel, fields[4], id, BitConverter.GetBytes(id), 0u, false, false);  // transfer
                    id++;
                }

                if (available < credit)
                {
                    TestListener.FRM(stream, 0x13UL, 0, channel, 0u, 100u, id, 100u, fields[4], id + credit - available, 0u, 0u, false);  // flow
                }

                return TestOutcome.Stop;
            });

            string testName = "ReceiverLinkManualCreditTest";

            Connection connection = new Connection(this.address);
            Session session = new Session(connection);
            ReceiverLink receiver = new ReceiverLink(session, "receiver-" + testName, "any");

            available = 10;
            receiver.SetCredit(10, false);
            for (int i = 0; i < 10; i++)
            {
                receiver.Accept(receiver.Receive());
            }

            available = 6;
            receiver.SetCredit(10, false);
            for (int i = 0; i < 6; i++)
            {
                receiver.Accept(receiver.Receive());
            }

            Message message = receiver.Receive(TimeSpan.FromMilliseconds(100));
            Assert.IsTrue(message == null, "should not get messages.");

            connection.Close();
        }

        [TestMethod]
        public void AcceptMessageOnWrongLinkTest()
        {
            uint id = 0;
            this.testListener.RegisterTarget(TestPoint.Flow, (stream, channel, fields) =>
            {
                TestListener.FRM(stream, 0x14UL, 0, channel, fields[4], id, BitConverter.GetBytes(id), 0u, false, false);  // transfer
                id++;
                return TestOutcome.Stop;
            });

            string testName = "AcceptMessageOnWrongLinkTest";

            Connection connection = new Connection(this.address);
            Session session = new Session(connection);
            ReceiverLink receiver1 = new ReceiverLink(session, "receiver1-" + testName, "any");
            Message msg1 = receiver1.Receive();
            Assert.IsTrue(msg1 != null);
            ReceiverLink receiver2 = new ReceiverLink(session, "receiver2-" + testName, "any");
            Message msg2 = receiver2.Receive();
            Assert.IsTrue(msg2 != null);

            try
            {
                receiver2.Accept(msg1);
                Assert.IsTrue(false, "Expect failure");
            }
            catch (InvalidOperationException) { }

            connection.Close();
        }

        [TestMethod]
        public void ConnectionEventsOnProtocolError()
        {
            ManualResetEvent closeReceived = null;
            ManualResetEvent closedNotified = null;

            this.testListener.RegisterTarget(TestPoint.Begin, (stream, channel, fields) =>
            {
                // begin with invalid remote channel
                TestListener.FRM(stream, 0x11UL, 0, channel, (ushort)2, 0u, 100u, 100u, 8u);
                return TestOutcome.Stop;
            });

            this.testListener.RegisterTarget(TestPoint.Close, (stream, channel, fields) =>
            {
                closeReceived.Set();
                return TestOutcome.Continue;
            });

            Trace.WriteLine(TraceLevel.Information, "sync test");
            {
                closeReceived = new ManualResetEvent(false);
                closedNotified = new ManualResetEvent(false);
                Connection connection = new Connection(this.address);
                connection.Closed += (o, e) => closedNotified.Set();
                Session session = new Session(connection);
                Assert.IsTrue(closeReceived.WaitOne(5000), "Close not received");
                Assert.IsTrue(closedNotified.WaitOne(5000), "Closed event not fired");
                Assert.AreEqual(ErrorCode.NotFound, (string)connection.Error.Condition);
                Assert.IsTrue(session.IsClosed);
                Assert.IsTrue(connection.IsClosed);
            }

            Trace.WriteLine(TraceLevel.Information, "async test");
            Task.Factory.StartNew(async () =>
            {
                closeReceived = new ManualResetEvent(false);
                closedNotified = new ManualResetEvent(false);
                Connection connection = await Connection.Factory.CreateAsync(this.address);
                connection.Closed += (o, e) => closedNotified.Set();
                Session session = new Session(connection);
                await Task.Factory.StartNew(o => ((ManualResetEvent)o).WaitOne(5000), closeReceived);
                await Task.Factory.StartNew(o => ((ManualResetEvent)o).WaitOne(5000), closedNotified);
                Assert.IsTrue(closeReceived.WaitOne(10), "Close not received");
                Assert.IsTrue(closedNotified.WaitOne(10), "Closed event not fired");
                Assert.AreEqual(ErrorCode.NotFound, (string)connection.Error.Condition);
                Assert.IsTrue(session.IsClosed);
                Assert.IsTrue(connection.IsClosed);
            }).Unwrap().GetAwaiter().GetResult();
        }

        [TestMethod]
        public void HandlerSslAuthenticateTest()
        {
            Event evt = default(Event);

            var handler = new TestHandler(e =>
            {
                if (e.Id == EventId.SslAuthenticate)
                {
                    evt = e;
                }
            });

            var sslAddress = new Address("amqps://127.0.0.1:" + port);
            try
            {
                Connection connection = new Connection(sslAddress, handler);
            }
            catch
            {
                // OK to fail as the listener doesnt support SSL
                // but the event should fire.
            }

            Assert.AreEqual(EventId.SslAuthenticate, evt.Id);
        }

        [TestMethod]
        public void HandlerTest()
        {
            string testName = "HandlerTest";
            this.testListener.RegisterTarget(TestPoint.Flow, (stream, channel, fields) =>
            {
                TestListener.FRM(stream, 0x14UL, 0, channel, fields[4], 0u, BitConverter.GetBytes(0), 0u, true);  // transfer
                return TestOutcome.Stop;
            });

            Action<Dictionary<EventId, int>> validator = dict =>
            {
                Assert.AreEqual(11, dict.Count);
                Assert.AreEqual(1, dict[EventId.SocketConnect]);
                Assert.AreEqual(1, dict[EventId.ConnectionLocalOpen]);
                Assert.AreEqual(1, dict[EventId.ConnectionRemoteOpen]);
                Assert.AreEqual(1, dict[EventId.SessionLocalOpen]);
                Assert.AreEqual(1, dict[EventId.SessionRemoteOpen]);
                Assert.AreEqual(2, dict[EventId.LinkLocalOpen]);
                Assert.AreEqual(2, dict[EventId.LinkRemoteOpen]);
                Assert.AreEqual(1, dict[EventId.SendDelivery]);
                Assert.AreEqual(2, dict[EventId.LinkLocalOpen]);
                Assert.AreEqual(2, dict[EventId.LinkRemoteOpen]);
                Assert.AreEqual(1, dict[EventId.ReceiveDelivery]);
                Assert.AreEqual(1, dict[EventId.ConnectionLocalClose]);
                Assert.AreEqual(1, dict[EventId.ConnectionRemoteClose]);
            };

            Trace.WriteLine(TraceLevel.Information, "sync test");
            {
                var events = new Dictionary<EventId, int>();
                var handler = new TestHandler(e =>
                {
                    int count = 0;
                    lock (events)
                    {
                        events.TryGetValue(e.Id, out count);
                        events[e.Id] = count + 1;
                    }
                });

                Connection connection = new Connection(this.address, handler);
                Session session = new Session(connection);
                SenderLink sender = new SenderLink(session, "sender-" + testName, "any");
                sender.Send(new Message("test") { Properties = new Properties() { MessageId = testName } });
                ReceiverLink receiver = new ReceiverLink(session, "receiver-" + testName, "any");
                Message message = receiver.Receive();
                receiver.Accept(message);
                connection.Close();

                validator(events);
            }

            Trace.WriteLine(TraceLevel.Information, "async test");
            Task.Factory.StartNew(async () =>
            {
                var events = new Dictionary<EventId, int>();
                var handler = new TestHandler(e =>
                {
                    int count = 0;
                    lock (events)
                    {
                        events.TryGetValue(e.Id, out count);
                        events[e.Id] = count + 1;
                    }
                });

                var factory = new ConnectionFactory();
                Connection connection = await factory.CreateAsync(this.address, handler);
                Session session = new Session(connection);
                SenderLink sender = new SenderLink(session, "sender-" + testName, "any");
                await sender.SendAsync(new Message("test") { Properties = new Properties() { MessageId = testName } });
                ReceiverLink receiver = new ReceiverLink(session, "receiver-" + testName, "any");
                Message message = await receiver.ReceiveAsync();
                receiver.Accept(message);
                await connection.CloseAsync();

                validator(events);
            }).Unwrap().GetAwaiter().GetResult();
        }

        [TestMethod]
        public void MessageFormatSendTest()
        {
            string testName = "MessageFormatSendTest";

            uint format = uint.MaxValue;
            this.testListener.RegisterTarget(TestPoint.Transfer, (stream, channel, fields) =>
            {
                format = (uint)fields[3];
                return TestOutcome.Continue;
            });

            var connection = new Connection(this.address);
            var session = new Session(connection);
            var sender = new SenderLink(session, "sender-" + testName, "any");
            var message = MessageBatch.Create(new[] { "test1", "test2", "test3" });
            sender.Send(message);
            connection.Close();

            Assert.AreEqual(message.Format, format);
        }

        [TestMethod]
        public void MessageFormatReceiveTest()
        {
            string testName = "MessageFormatReceiveTest";

            this.testListener.RegisterTarget(TestPoint.Flow, (stream, channel, fields) =>
            {
                TestListener.FRM(stream, 0x14UL, 0, channel, fields[4], 0u, BitConverter.GetBytes(0), 123u, false, false);  // transfer
                return TestOutcome.Stop;
            });

            Connection connection = new Connection(this.address);
            Session session = new Session(connection);
            ReceiverLink receiver = new ReceiverLink(session, "receiver-" + testName, "any");
            Message message = receiver.Receive();
            Assert.AreEqual(123u, message.Format);
            receiver.Accept(message);
            connection.Close();
        }

        [TestMethod]
        public void ForwardMessageTest()
        {
            string testName = "ForwardMessageTest";

            uint total = 0;
            this.testListener.RegisterTarget(TestPoint.Flow, (stream, channel, fields) =>
            {
                uint current = total;
                total = Math.Min(510u, (uint)fields[5] + (uint)fields[6]);
                for (uint i = current; i < total; i++)
                {
                    TestListener.FRM(stream, 0x14UL, 0, channel, fields[4], i, BitConverter.GetBytes(i), 0u, false, false);  // transfer
                }

                return TestOutcome.Stop;
            });

            int port2 = port + 1;
            var listener2 = new TestListener(new IPEndPoint(IPAddress.Any, port2));
            listener2.Open();

            try
            {
                Connection connection = new Connection(this.address);
                Session session = new Session(connection);
                ReceiverLink receiver = new ReceiverLink(session, "receiver-" + testName, "any");

                Connection connection2 = new Connection(new Address("amqp://127.0.0.1:" + port2));
                Session session2 = new Session(connection2);
                SenderLink sender = new SenderLink(session2, "sender-" + testName, "any");

                int count = 0;
                var done = new ManualResetEvent(false);
                receiver.Start(300, (r, m) =>
                {
                    r.Accept(m);
                    sender.Send(m, (a, b, c, d) => { if (count++ >= 500) done.Set(); }, null);
                });

                done.WaitOne(10000);
                Trace.WriteLine(TraceLevel.Information, "done: {0}", count);

                connection.Close();
                connection2.Close();
            }
            finally
            {
                listener2.Close();
            }
        }
    }
}
