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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Amqp;
using Amqp.Framing;
using System.Threading.Tasks;

namespace Test.Amqp
{
    [TestClass]
    public class TaskTests
    {
        Address address = new Address("amqp://guest:guest@localhost:5672");

        [TestInitialize]
        public void Initialize()
        {
            // uncomment the following to write frame traces
            //Trace.TraceLevel = TraceLevel.Frame;
            //Trace.TraceListener = (f, a) => System.Diagnostics.Trace.WriteLine(System.DateTime.Now.ToString("[hh:ss.fff]") + " " + string.Format(f, a));
        }

        [TestMethod]
        public async Task BasicSendReceiveAsyncTest()
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
                await sender.SendAsync(message);
            }

            ReceiverLink receiver = new ReceiverLink(session, "receive-link", "q1");
            for (int i = 0; i < 200; ++i)
            {
                if (i % 50 == 0) receiver.SetCredit(50);
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
