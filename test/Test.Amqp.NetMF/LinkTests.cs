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
using System.Threading;
#if !NETMF
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif

namespace Test.Amqp
{
#if !NETMF
    [TestClass]
#endif
    public class LinkTests
    {
        Address address = new Address("amqp://guest:guest@localhost:5672");

#if !NETMF
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

#if !NETMF
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

            done.WaitOne(10000, true);

            sender.Close();
            receiver.Close();
            session.Close();
            connection.Close();
        }

#if !NETMF
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

            done.WaitOne(10000, true);

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

#if !NETMF
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
    }
}
