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
using System.Threading;
using System.Threading.Tasks;
using Amqp;
using Amqp.Framing;
using Amqp.Types;
using System.Linq;
#if NETFX_CORE
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif

namespace Test.Amqp
{
    [TestClass]
    public class TaskTests
    {
        TestTarget testTarget = new TestTarget();

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
        }

#if !NETFX40
        [TestMethod]
        public async Task BasicSendReceiveAsync()
        {
            string testName = "BasicSendReceiveAsync";
            int nMsgs = 100;

            Connection connection = await Connection.Factory.CreateAsync(this.testTarget.Address);
            Session session = new Session(connection);
            SenderLink sender = new SenderLink(session, "sender-" + testName, testTarget.Path);

            for (int i = 0; i < nMsgs; ++i)
            {
                Message message = new Message();
                message.Properties = new Properties() { MessageId = "msg" + i, GroupId = testName };
                message.ApplicationProperties = new ApplicationProperties();
                message.ApplicationProperties["sn"] = i;
                await sender.SendAsync(message);
            }

            ReceiverLink receiver = new ReceiverLink(session, "receiver-" + testName, testTarget.Path);
            for (int i = 0; i < nMsgs; ++i)
            {
                Message message = await receiver.ReceiveAsync();
                Trace.WriteLine(TraceLevel.Information, "receive: {0}", message.ApplicationProperties["sn"]);
                receiver.Accept(message);
            }

            await sender.CloseAsync();
            await receiver.CloseAsync();
            await session.CloseAsync();
            await connection.CloseAsync();
        }

        [TestMethod]
        public async Task InterfaceSendReceiveAsync()
        {
            string testName = "BasicSendReceiveAsync";
            int nMsgs = 100;

            IConnectionFactory factory = new ConnectionFactory();
            IConnection connection = await factory.CreateAsync(this.testTarget.Address);
            ISession session = connection.CreateSession();
            ISenderLink sender = session.CreateSender("sender-" + testName, testTarget.Path);

            for (int i = 0; i < nMsgs; ++i)
            {
                Message message = new Message();
                message.Properties = new Properties() { MessageId = "msg" + i, GroupId = testName };
                message.ApplicationProperties = new ApplicationProperties();
                message.ApplicationProperties["sn"] = i;
                await sender.SendAsync(message);
            }

            IReceiverLink receiver = session.CreateReceiver("receiver-" + testName, testTarget.Path);
            for (int i = 0; i < nMsgs; ++i)
            {
                Message message = await receiver.ReceiveAsync(Timeout.InfiniteTimeSpan);
                Trace.WriteLine(TraceLevel.Information, "receive: {0}", message.ApplicationProperties["sn"]);
                receiver.Accept(message);
            }

            await sender.CloseAsync();
            await receiver.CloseAsync();
            await session.CloseAsync();
            await connection.CloseAsync();
        }

        [TestMethod]
        public async Task SendToNonExistingAsync()
        {
            string testName = "SendToNonExistingAsync";

            Connection connection = await Connection.Factory.CreateAsync(this.testTarget.Address);
            Session session = new Session(connection);
            SenderLink sender = new SenderLink(session, "$explicit:sender-" + testName, Guid.NewGuid().ToString());
            try
            {
                await sender.SendAsync(new Message("test"));
                Assert.IsTrue(false, "Send should fail with not-found error");
            }
            catch (AmqpException exception)
            {
                Assert.AreEqual((Symbol)ErrorCode.NotFound, exception.Error.Condition);
            }

            await connection.CloseAsync();
        }

        [TestMethod]
        public async Task ReceiveFromNonExistingAsync()
        {
            string testName = "ReceiveFromNonExistingAsync";

            Connection connection = await Connection.Factory.CreateAsync(this.testTarget.Address);
            Session session = new Session(connection);
            ReceiverLink receiver = new ReceiverLink(session, "$explicit:receiver-" + testName, Guid.NewGuid().ToString());
            try
            {
                await receiver.ReceiveAsync();
                Assert.IsTrue(false, "Receive should fail with not-found error");
            }
            catch (AmqpException exception)
            {
                Assert.AreEqual((Symbol)ErrorCode.NotFound, exception.Error.Condition);
            }

            await connection.CloseAsync();
        }
#endif

#if NETFX40
        // 40 cannot handle TestMethod with Task return type
        // MsTest fails with "Index was outside the bounds of the array."
        [TestMethod]
        public void BasicSendReceiveAsync()
        {
            this.BasicSendReceiveAsyncTest().GetAwaiter().GetResult();
        }

        async Task BasicSendReceiveAsyncTest()
        {
            string testName = "BasicSendReceiveAsync";
            int nMsgs = 100;

            Connection connection = await Connection.Factory.CreateAsync(this.testTarget.Address);
            Session session = new Session(connection);
            SenderLink sender = new SenderLink(session, "sender-" + testName, testTarget.Path);

            for (int i = 0; i < nMsgs; ++i)
            {
                Message message = new Message();
                message.Properties = new Properties() { MessageId = "msg" + i, GroupId = testName };
                message.ApplicationProperties = new ApplicationProperties();
                message.ApplicationProperties["sn"] = i;
                await sender.SendAsync(message);
            }

            ReceiverLink receiver = new ReceiverLink(session, "receiver-" + testName, testTarget.Path);
            for (int i = 0; i < nMsgs; ++i)
            {
                Message message = await receiver.ReceiveAsync();
                Trace.WriteLine(TraceLevel.Information, "receive: {0}", message.ApplicationProperties["sn"]);
                receiver.Accept(message);
            }

            await sender.CloseAsync();
            await receiver.CloseAsync();
            await session.CloseAsync();
            await connection.CloseAsync();
        }
#endif

#if NETFX && !NETFX40
        [TestMethod]
        public async Task CustomMessgeBody()
        {
            string testName = "CustomMessgeBody";

            Connection connection = await Connection.Factory.CreateAsync(this.testTarget.Address);
            Session session = new Session(connection);
            SenderLink sender = new SenderLink(session, "sender-" + testName, testTarget.Path);

            Student student = new Student("Tom");
            student.Age = 16;
            student.Address = new StreetAddress() { FullAddress = "100 Main St. Small Town" };
            student.DateOfBirth = new System.DateTime(1988, 5, 1, 1, 2, 3, 100, System.DateTimeKind.Utc);

            Message message = new Message(student);
            message.Properties = new Properties() { MessageId = "student" };
            await sender.SendAsync(message);

            ReceiverLink receiver = new ReceiverLink(session, "receiver-" + testName, testTarget.Path);
            Message message2 = await receiver.ReceiveAsync();
            Trace.WriteLine(TraceLevel.Information, "receive: {0}", message2.Properties);
            receiver.Accept(message);

            await sender.CloseAsync();
            await receiver.CloseAsync();
            await session.CloseAsync();
            await connection.CloseAsync();

            Student student2 = message2.GetBody<Student>();
            Assert.AreEqual(student.Age, student2.Age - 1); // incremented in OnDeserialized
            Assert.AreEqual(student.DateOfBirth, student2.DateOfBirth);
            Assert.AreEqual(student.Address.FullAddress, student2.Address.FullAddress);
        }

        [TestMethod]
        public async Task LargeMessageSendReceiveAsync()
        {
            string testName = "LargeMessageSendReceiveAsync";
            int nMsgs = 50;

            Connection connection = await Connection.Factory.CreateAsync(
                this.testTarget.Address, new Open() { ContainerId = "c1", MaxFrameSize = 4096 }, null);
            Session session = new Session(connection);
            SenderLink sender = new SenderLink(session, "sender-" + testName, testTarget.Path);

            int messageSize = 100 * 1024;
            for (int i = 0; i < nMsgs; ++i)
            {
                Message message = new Message(new string('D', messageSize));
                message.Properties = new Properties() { MessageId = "msg" + i, GroupId = testName };
                message.ApplicationProperties = new ApplicationProperties();
                message.ApplicationProperties["sn"] = i;
                await sender.SendAsync(message);
            }

            ReceiverLink receiver = new ReceiverLink(session, "receiver-" + testName, testTarget.Path);
            for (int i = 0; i < nMsgs; ++i)
            {
                Message message = await receiver.ReceiveAsync();
                string value = message.GetBody<string>();
                Trace.WriteLine(TraceLevel.Information, "receive: {0} body {1}x{2}",
                    message.ApplicationProperties["sn"], value[0], value.Length);
                receiver.Accept(message);
            }

            await sender.CloseAsync();
            await receiver.CloseAsync();
            await session.CloseAsync();
            await connection.CloseAsync();
        }

        [TestMethod]
        public async Task LargeMessageOnMessageCallback()
        {
            string testName = "LargeMessageOnMessageCallback";
            int nMsgs = 50;

            Connection connection = await Connection.Factory.CreateAsync(
                this.testTarget.Address, new Open() { ContainerId = "c1", MaxFrameSize = 4096 }, null);
            Session session = new Session(connection);
            SenderLink sender = new SenderLink(session, "sender-" + testName, testTarget.Path);

            int messageSize = 10 * 1024;
            for (int i = 0; i < nMsgs; ++i)
            {
                Message message = new Message(new string('D', messageSize));
                message.Properties = new Properties() { MessageId = "msg" + i, GroupId = testName };
                message.ApplicationProperties = new ApplicationProperties();
                message.ApplicationProperties["sn"] = i;
                sender.Send(message, null, null);
            }

            ReceiverLink receiver = new ReceiverLink(session, "receiver-" + testName, testTarget.Path);
            ManualResetEvent done = new ManualResetEvent(false);
            int count = 0;
            receiver.Start(30, (link, message) =>
            {
                string value = message.GetBody<string>();
                Trace.WriteLine(TraceLevel.Information, "receive: {0} body {1}x{2}",
                    message.ApplicationProperties["sn"], value[0], value.Length);
                receiver.Accept(message);

                if (++count == nMsgs) done.Set();
            });

            Assert.IsTrue(done.WaitOne(120000));

            connection.Close();
        }

        [TestMethod]
        public async Task CustomTransportConfiguration()
        {
            string testName = "CustomTransportConfiguration";

            ConnectionFactory factory = new ConnectionFactory();
            factory.TCP.NoDelay = true;
            factory.TCP.SendBufferSize = 16 * 1024;
            factory.TCP.SendTimeout = 30000;
            factory.SSL.RemoteCertificateValidationCallback = (a, b, c, d) => true;
            factory.AMQP.MaxFrameSize = 64 * 1024;
            factory.AMQP.HostName = "contoso.com";
            factory.AMQP.ContainerId = "container:" + testName;

            Address sslAddress = new Address("amqps://guest:guest@127.0.0.1:5671");
            Connection connection = await factory.CreateAsync(sslAddress);
            Session session = new Session(connection);
            SenderLink sender = new SenderLink(session, "sender-" + testName, testTarget.Path);

            Message message = new Message("custom transport config");
            message.Properties = new Properties() { MessageId = testName };
            await sender.SendAsync(message);

            ReceiverLink receiver = new ReceiverLink(session, "receiver-" + testName, testTarget.Path);
            Message message2 = await receiver.ReceiveAsync();
            Assert.IsTrue(message2 != null, "no message received");
            receiver.Accept(message2);

            await connection.CloseAsync();
        }

        [TestMethod]
        public async Task MultipleConcurrentWriters()
        {
            // Up to version 2.1.3, multiple writers from a single task could cause a deadlock
            // This test checks it's fixed
            var senderName = "Sender";
            const int NbQueues = 2;
            const int NbProducerTasks = 4;
            const int NbMessages = 10;
            const int MessageSize = 102400;
            int startedProducers = 0;
            bool testStarted = false;
            Trace.TraceLevel = TraceLevel.Information;
            Trace.TraceListener = (l, f, a) => Console.WriteLine(DateTime.Now.ToString("[hh:mm:ss.fff]") + " " + string.Format(f, a));

            var senderLinks = new SenderLink[NbQueues];

            for (int i = 0; i < NbQueues; i++)
            {
                Connection connection = await Connection.Factory.CreateAsync(
                    this.testTarget.Address, new Open() { ContainerId = "c1", MaxFrameSize = 4096 }, null);

                var queueName = "q" + (i % NbQueues);
                Session session = new Session(connection);
                senderLinks[i] = new SenderLink(session, senderName, queueName);
            }

            var tasks = new Task[NbProducerTasks];
            var latch = new object();

            for (int i = 0; i < tasks.Length; i++)
            {
                var testId = i;
                tasks[i] = Task.Run(async () =>
                {
                    lock (latch)
                    {
                        startedProducers++;
                        Trace.WriteLine(TraceLevel.Information, "Producer {0} ready; waiting for start signal", testId);
                        while (!testStarted)
                        {
                            Monitor.Pulse(latch);
                            Monitor.Wait(latch);
                        }
                    }

                    Trace.WriteLine(TraceLevel.Information, "Starting test run {0}", testId);

                    var rand = new Random();
                    var data = Enumerable.Range(0, rand.Next(MessageSize) + 1000).Select(x => (byte)x).ToArray();
                    for (int j = 0; j < NbMessages; j++)
                    {
                        data[0] = (byte)j;
                        var message = new Message()
                        {
                            BodySection = new Data()
                            {
                                Binary = data
                            },
                            Properties = new Properties
                            {
                                AbsoluteExpiryTime = DateTime.Today.AddDays(1),
                                MessageId = j.ToString()
                            }
                        };


                        Trace.WriteLine(TraceLevel.Information, "Test {0} - Sending message {1}", testId, j);
                        await senderLinks[j % NbQueues].SendAsync(message, TimeSpan.FromSeconds(10));
                        Trace.WriteLine(TraceLevel.Information, "Test {0} - Message {1} sent", testId, j);

                    }
                });
            }

            lock (latch)
            {
                Trace.WriteLine(TraceLevel.Information, "Waiting for producers");
                while (startedProducers < tasks.Length)
                {
                    Trace.WriteLine(TraceLevel.Information, "Started producers: {0}", startedProducers);
                    Monitor.Wait(latch);
                }

                // All producers are started; wake them up!
                testStarted = true;
                Monitor.PulseAll(latch);
            }

            await Task.WhenAll(tasks);
        }
        
        //private static async Task<Connection> Connect()
        //{
        //    var connectionFactory = new ConnectionFactory();
        //    connectionFactory.AMQP.ContainerId = Guid.NewGuid().ToString();

        //    // This is required on RHEL to avoid timeouts, especially for small messages
        //    connectionFactory.TCP.ReceiveBufferSize = 1_048_576; // 1MB
        //    connectionFactory.TCP.SendBufferSize = 1_048_576; // 1MB

        //    connectionFactory.SSL.RemoteCertificateValidationCallback = delegate { return true; };

        //    var mq = new
        //    {
        //        Scheme = "amqp",
        //        UserName = "amq",
        //        Password = "amq",
        //        ServerName = "localhost",
        //        Port = 5672
        //    };
        //    var address = new Address($"{mq.Scheme}://{mq.UserName}:{mq.Password}@{mq.ServerName}:{mq.Port}");
        //    Log.Information("Connecting MessageQueueHandler to {Url}", address);

        //    var connection = await connectionFactory.CreateAsync(address).ConfigureAwait(false);
        //    return connection;
        //}
        
#endif
    }
}
