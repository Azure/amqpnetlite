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

using System.Threading;
using System.Threading.Tasks;
using Amqp;
using Amqp.Framing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Transactions;

namespace Test.Amqp
{
    [TestClass]
    public class TransactionTests
    {
        TestTarget testTarget = new TestTarget();

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
        }

        // if the project is targeted 4.5.1, TransactionScope can be created with
        // TransactionScopeAsyncFlowOption.Enabled option, and message can be sent
        // using SendAsync method.
        [TestMethod]
        public void TransactedPosting()
        {
            string testName = "TransactedPosting";
            int nMsgs = 5;

            Connection connection = new Connection(testTarget.Address);
            Session session = new Session(connection);
            SenderLink sender = new SenderLink(session, "sender-" + testName, testTarget.Path);

            // commit
            using (var ts = new TransactionScope())
            {
                for (int i = 0; i < nMsgs; i++)
                {
                    Message message = new Message("test");
                    message.Properties = new Properties() { MessageId = "commit" + i, GroupId = testName };
                    sender.Send(message);
                }

                ts.Complete();
            }

            // rollback
            using (var ts = new TransactionScope())
            {
                for (int i = nMsgs; i < nMsgs * 2; i++)
                {
                    Message message = new Message("test");
                    message.Properties = new Properties() { MessageId = "rollback" + i, GroupId = testName };
                    sender.Send(message);
                }
            }

            // commit
            using (var ts = new TransactionScope())
            {
                for (int i = 0; i < nMsgs; i++)
                {
                    Message message = new Message("test");
                    message.Properties = new Properties() { MessageId = "commit" + i, GroupId = testName };
                    sender.Send(message);
                }

                ts.Complete();
            }

            ReceiverLink receiver = new ReceiverLink(session, "receiver-" + testName, testTarget.Path);
            for (int i = 0; i < nMsgs * 2; i++)
            {
                Message message = receiver.Receive();
                Trace.WriteLine(TraceLevel.Information, "receive: {0}", message.Properties.MessageId);
                receiver.Accept(message);
                Assert.IsTrue(message.Properties.MessageId.StartsWith("commit"));
            }

            connection.Close();
        }

        [TestMethod]
        public void TransactedRetiring()
        {
            string testName = "TransactedRetiring";
            int nMsgs = 10;

            Connection connection = new Connection(testTarget.Address);
            Session session = new Session(connection);
            SenderLink sender = new SenderLink(session, "sender-" + testName, testTarget.Path);

            // send one extra for validation
            for (int i = 0; i < nMsgs + 1; i++)
            {
                Message message = new Message("test");
                message.Properties = new Properties() { MessageId = "msg" + i, GroupId = testName };
                sender.Send(message);
            }

            ReceiverLink receiver = new ReceiverLink(session, "receiver-" + testName, testTarget.Path);
            Message[] messages = new Message[nMsgs];
            for (int i = 0; i < nMsgs; i++)
            {
                messages[i] = receiver.Receive();
                Trace.WriteLine(TraceLevel.Information, "receive: {0}", messages[i].Properties.MessageId);
            }

            // commit half
            using (var ts = new TransactionScope())
            {
                for (int i = 0; i < nMsgs / 2; i++)
                {
                    receiver.Accept(messages[i]);
                }

                ts.Complete();
            }

            // rollback
            using (var ts = new TransactionScope())
            {
                for (int i = nMsgs / 2; i < nMsgs; i++)
                {
                    receiver.Accept(messages[i]);
                }
            }

            // after rollback, messages should be still acquired
            {
                Message message = receiver.Receive();
                Assert.AreEqual("msg" + nMsgs, message.Properties.MessageId);
                receiver.Release(message);
            }

            // commit
            using (var ts = new TransactionScope())
            {
                for (int i = nMsgs / 2; i < nMsgs; i++)
                {
                    receiver.Accept(messages[i]);
                }

                ts.Complete();
            }

            // only the last message is left
            {
                Message message = receiver.Receive();
                Assert.AreEqual("msg" + nMsgs, message.Properties.MessageId);
                receiver.Accept(message);
            }

            // at this point, the queue should have zero messages.
            // If there are messages, it is a bug in the broker.

            connection.Close();
        }

        [TestMethod]
        public void TransactedRetiringAndPosting()
        {
            string testName = "TransactedRetiringAndPosting";
            int nMsgs = 10;

            Connection connection = new Connection(testTarget.Address);
            Session session = new Session(connection);
            SenderLink sender = new SenderLink(session, "sender-" + testName, testTarget.Path);

            for (int i = 0; i < nMsgs; i++)
            {
                Message message = new Message("test");
                message.Properties = new Properties() { MessageId = "msg" + i, GroupId = testName };
                sender.Send(message);
            }

            ReceiverLink receiver = new ReceiverLink(session, "receiver-" + testName, testTarget.Path);

            receiver.SetCredit(2, false);
            Message message1 = receiver.Receive();
            Message message2 = receiver.Receive();

            // ack message1 and send a new message in a txn
            using (var ts = new TransactionScope())
            {
                receiver.Accept(message1);

                Message message = new Message("test");
                message.Properties = new Properties() { MessageId = "msg" + nMsgs, GroupId = testName };
                sender.Send(message);

                ts.Complete();
            }

            // ack message2 and send a new message in a txn but abort the txn
            using (var ts = new TransactionScope())
            {
                receiver.Accept(message2);

                Message message = new Message("test");
                message.Properties = new Properties() { MessageId = "msg" + (nMsgs + 1), GroupId = testName };
                sender.Send(message1);
            }

            receiver.Release(message2);

            // receive all messages. should see the effect of the first txn
            receiver.SetCredit(nMsgs, false);
            for (int i = 1; i <= nMsgs; i++)
            {
                Message message = receiver.Receive();
                Trace.WriteLine(TraceLevel.Information, "receive: {0}", message.Properties.MessageId);
                receiver.Accept(message);
                Assert.AreEqual("msg" + i, message.Properties.MessageId);
            }

            connection.Close();
        }
    }
}
