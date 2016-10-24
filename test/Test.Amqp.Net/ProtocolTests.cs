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
using System.Net;
using System.Threading;
using Amqp;
using Amqp.Framing;
using Amqp.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test.Amqp
{
    [TestClass]
    public class ProtocolTests
    {
        const int port = 5679;
        TestListener testListener;

        static ProtocolTests()
        {
            Trace.TraceLevel = TraceLevel.Frame;
            Trace.TraceListener = (f, a) => System.Diagnostics.Trace.WriteLine(System.DateTime.Now.ToString("[hh:mm:ss.fff]") + " " + string.Format(f, a));
        }

        [TestInitialize]
        public void Initialize()
        {
            this.testListener = new TestListener(new IPEndPoint(IPAddress.Any, port));
            this.testListener.Open();
        }

        [TestCleanup]
        public void Cleanup()
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
            Connection connection = new Connection(new Address("amqp://localhost:" + port));
            Session session = new Session(connection);
            SenderLink sender = new SenderLink(session, "sender-" + testName, "any");
            sender.Send(new Message("test") { Properties = new Properties() { MessageId = testName } });
            connection.Close();
            Assert.IsTrue(connection.Error == null, "connection has error!" + connection.Error);
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
            Connection connection = new Connection(new Address("amqp://localhost:" + port));
            Session session = new Session(connection);
            SenderLink sender = new SenderLink(session, "sender-" + testName, "any");
            sender.Send(new Message("test") { Properties = new Properties() { MessageId = testName } });
            connection.Close();
            Assert.IsTrue(connection.Error == null, "connection has error!" + connection.Error);
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
            Connection connection = new Connection(new Address("amqp://localhost:" + port));
            Session session = new Session(connection);
            SenderLink sender = new SenderLink(session, "sender-" + testName, "any");
            sender.Send(new Message("test") { Properties = new Properties() { MessageId = testName } });
            session.Close(0);
            connection.Close();
            Assert.IsTrue(connection.Error == null, "connection has error!" + connection.Error);
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
            Connection connection = new Connection(new Address("amqp://localhost:" + port));
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

        [TestMethod]
        public void SendWithProtocolErrorTest()
        {
            this.testListener.RegisterTarget(TestPoint.Transfer, (stream, channel, fields) =>
            {
                // send an end with invalid channel
                TestListener.FRM(stream, 0x17UL, 0, 33);
                return TestOutcome.Stop;
            });

            string testName = "SendWithProtocolErrorTest";
            Connection connection = new Connection(new Address("amqp://localhost:" + port));
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

        [TestMethod]
        public void ReceiveWithConnectionResetTest()
        {
            this.testListener.RegisterTarget(TestPoint.Flow, (stream, channel, fields) =>
            {
                stream.Dispose();
                return TestOutcome.Continue;
            });

            string testName = "ReceiveWithConnectionResetTest";
            Connection connection = new Connection(new Address("amqp://localhost:" + port));
            Session session = new Session(connection);
            ReceiverLink receiver = new ReceiverLink(session, "receiver-" + testName, "any");
            DateTime start = DateTime.UtcNow;
            Message message = receiver.Receive();
            Assert.IsTrue(message == null);
            Assert.IsTrue(DateTime.UtcNow.Subtract(start).TotalMilliseconds < 1000, "Receive call is not cancelled.");
            connection.Close();
            Assert.AreEqual(ErrorCode.ConnectionForced, (string)connection.Error.Condition);
        }
    }
}
