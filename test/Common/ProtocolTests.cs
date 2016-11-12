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
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amqp;
using Amqp.Framing;
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
            //Trace.TraceLevel = TraceLevel.Frame;
            //Trace.TraceListener = (f, a) => System.Diagnostics.Trace.WriteLine(System.DateTime.Now.ToString("[hh:mm:ss.fff]") + " " + string.Format(f, a));
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
                session.Close(0);
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
                session.Close(0);
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
        public void ClosedEventOnTransportResetTest()
        {
            this.testListener.RegisterTarget(TestPoint.Begin, (stream, channel, fields) =>
            {
                stream.Dispose();
                return TestOutcome.Continue;
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
                Assert.IsTrue(closed.WaitOne(5000), "closed event not fired");
                Assert.AreEqual(ErrorCode.ConnectionForced, (string)connection.Error.Condition);
            }).Unwrap().GetAwaiter().GetResult();
        }

        [TestMethod]
        public void InvalidSaslProtocolHeaderTest()
        {
            Stream transport = null;

            this.testListener.RegisterTarget(TestPoint.Header, (stream, channel, fields) =>
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
                    transport.WriteByte(1);
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
                    transport.WriteByte(2);
                    Assert.IsTrue(false, "transport not disposed 2.");
                }
                catch (ObjectDisposedException) { }
                catch (IOException) { }
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
                DateTime start = DateTime.UtcNow;
                Message message = receiver.Receive();
                Assert.IsTrue(message == null);
                Assert.IsTrue(DateTime.UtcNow.Subtract(start).TotalMilliseconds < 5000, "Receive call is not cancelled.");
                connection.Close();
                Assert.AreEqual(ErrorCode.ConnectionForced, (string)connection.Error.Condition);
            }

            Trace.WriteLine(TraceLevel.Information, "async test");
            Task.Factory.StartNew(async () =>
            {
                Connection connection = await Connection.Factory.CreateAsync(this.address);
                Session session = new Session(connection);
                ReceiverLink receiver = new ReceiverLink(session, "receiver-" + testName, "any");
                DateTime start = DateTime.UtcNow;
                Message message = await receiver.ReceiveAsync();
                Assert.IsTrue(message == null);
                Assert.IsTrue(DateTime.UtcNow.Subtract(start).TotalMilliseconds < 1000, "Receive call is not cancelled.");
                await connection.CloseAsync();
                Assert.AreEqual(ErrorCode.ConnectionForced, (string)connection.Error.Condition);
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
            }

            Trace.WriteLine(TraceLevel.Information, "async test");
            Task.Factory.StartNew(async () =>
            {
                ManualResetEvent closed = new ManualResetEvent(false);
                Connection connection = await Connection.Factory.CreateAsync(this.address);
                connection.Closed += (s, a) => closed.Set();
                Session session = new Session(connection);
                ReceiverLink receiver = new ReceiverLink(session, "receiver-" + testName, "any");
                Assert.IsTrue(closed.WaitOne(5000), "Connection not closed");
                Assert.AreEqual(ErrorCode.TransferLimitExceeded, (string)connection.Error.Condition);
            }).Unwrap().GetAwaiter().GetResult();
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
            }

            Trace.WriteLine(TraceLevel.Information, "async test");
            Task.Factory.StartNew(async () =>
            {
                closeReceived = new ManualResetEvent(false);
                closedNotified = new ManualResetEvent(false);
                Connection connection = await Connection.Factory.CreateAsync(this.address);
                connection.Closed += (o, e) => closedNotified.Set();
                Session session = new Session(connection);
                Assert.IsTrue(closeReceived.WaitOne(5000), "Close not received");
                Assert.IsTrue(closedNotified.WaitOne(5000), "Closed event not fired");
                Assert.AreEqual(ErrorCode.NotFound, (string)connection.Error.Condition);
            }).Unwrap().GetAwaiter().GetResult();
        }
    }
}
