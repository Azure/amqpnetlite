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
using Listener.IContainer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace Test.Amqp
{
    [TestClass]
    public class TestBrokerTests
    {
        const string address = "amqp://guest:guest@localhost:15674";
        static TestAmqpBroker broker;

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            broker = new TestAmqpBroker(new[] { address }, null, null, null);
            broker.Start();
        }

        [ClassCleanup]
        public static void Cleanup()
        {
            broker.Stop();
        }

        [TestMethod]
        public async Task MessageBatchTest()
        {
            string testName = "MessageBatchTest";

            var connection = await Connection.Factory.CreateAsync(new Address(address));
            Session session = new Session(connection);
            SenderLink sender = new SenderLink(session, "sender", testName);

            var batch = MessageBatch.Create(new[] { "data1", "data2", "data3", "data4" });
            await sender.SendAsync(batch);

            ReceiverLink receiver = new ReceiverLink(session, "receiver", testName);
            for (int i = 0; i < 4; i++)
            {
                var m = await receiver.ReceiveAsync();
                Assert.IsTrue(m != null, "Didn't receive message " + i);
                receiver.Accept(m);
            }

            await connection.CloseAsync();
        }
    }
}
