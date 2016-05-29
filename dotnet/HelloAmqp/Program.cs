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

namespace ConsoleApplication
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Run().Wait();
        }

        static async Task Run()
        {
            string address = "amqp://guest:guest@localhost:5672";

            Connection connection = await Connection.Factory.CreateAsync(new Address(address));
            Session session = new Session(connection);
            SenderLink sender = new SenderLink(session, "test-sender", "q1");

            Message message1 = new Message("Hello AMQP!");
            await sender.SendAsync(message1);

            ReceiverLink receiver = new ReceiverLink(session, "test-receiver", "q1");
            Message message2 = await receiver.ReceiveAsync();
            Console.WriteLine(message2.GetBody<string>());
            receiver.Accept(message2);

            await sender.CloseAsync();
            await receiver.CloseAsync();
            await session.CloseAsync();
            await connection.CloseAsync();
        }
    }
}
