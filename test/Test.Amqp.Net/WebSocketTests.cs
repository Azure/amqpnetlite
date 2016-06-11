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
using System.Threading.Tasks;
using Amqp;
using Amqp.Framing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test.Amqp
{
    [TestClass]
    public class WebSocketTests
    {
        const string address = "ws://guest:guest@localhost:18080/test";

        [TestMethod]
        public void WebSocketContainerHostTests()
        {
            int total = 0;
            int passed = 0;

            foreach (var mi in typeof(ContainerHostTests).GetMethods())
            {
                if (mi.GetCustomAttributes(typeof(TestMethodAttribute), false).Length > 0 &&
                    mi.GetCustomAttributes(typeof(IgnoreAttribute), false).Length == 0)
                {
                    total++;

                    ContainerHostTests test = new ContainerHostTests();
                    test.Uri = new System.Uri(address);
                    test.ClassInitialize();

                    try
                    {
                        mi.Invoke(test, new object[0]);
                        System.Diagnostics.Trace.WriteLine(mi.Name + " passed");
                        passed++;
                    }
                    catch (Exception exception)
                    {
                        System.Diagnostics.Trace.WriteLine(mi.Name + " failed: " + exception.ToString());
                    }

                    test.ClassCleanup();
                }
            }

            Assert.AreEqual(total, passed, string.Format("Not all tests passed {0}/{1}", passed, total));
        }

        [TestMethod]
        public async Task WebSocketSendReceiveAsync()
        {
            string testName = "WebSocketSendReceiveAsync";

            // assuming it matches the broker's setup and port is not taken
            Address wsAddress = new Address(address);
            int nMsgs = 50;

            Connection connection = await Connection.Factory.CreateAsync(wsAddress);
            Session session = new Session(connection);
            SenderLink sender = new SenderLink(session, "sender-" + testName, "q1");

            for (int i = 0; i < nMsgs; ++i)
            {
                Message message = new Message();
                message.Properties = new Properties() { MessageId = "msg" + i, GroupId = testName };
                message.ApplicationProperties = new ApplicationProperties();
                message.ApplicationProperties["sn"] = i;
                await sender.SendAsync(message);
            }

            ReceiverLink receiver = new ReceiverLink(session, "receiver-" + testName, "q1");
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
    }
}
