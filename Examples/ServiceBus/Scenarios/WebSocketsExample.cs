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

namespace ServiceBus.Scenarios
{
    using System.Text;
    using System.Threading.Tasks;
    using Amqp;
    using Amqp.Framing;

    /// <summary>
    /// This example assumes a queue is precreated. Example.Entity should be set to the queue name.
    /// </summary>
    class WebSocketsExample : Example
    {
        public override void Run()
        {
            this.SendReceiveAsync(10).GetAwaiter().GetResult();
        }

        async Task SendReceiveAsync(int count)
        {
            // it is also possible to create the Address object form a Uri string as follows,
            //   wss://[sas-policy]:[sas-key]@[ns].servicebus.windows.net/$servicebus/websocket
            // note that [sas-policy] and [sas-key] should be URL encoded
            Address wsAddress = new Address(this.Namespace, 443, this.KeyName, this.KeyValue, "/$servicebus/websocket", "wss");
            WebSocketTransportFactory wsFactory = new WebSocketTransportFactory("AMQPWSB10");

            Trace.WriteLine(TraceLevel.Information, "Establishing a connection...");
            ConnectionFactory connectionFactory = new ConnectionFactory(new TransportProvider[] { wsFactory });
            Connection connection = await connectionFactory.CreateAsync(wsAddress);

            Trace.WriteLine(TraceLevel.Information, "Creating a session...");
            Session session = new Session(connection);

            Trace.WriteLine(TraceLevel.Information, "Creating a sender link...");
            SenderLink sender = new SenderLink(session, "websocket-sender-link", this.Entity);

            Trace.WriteLine(TraceLevel.Information, "Sending {0} messages...", count);
            for (int i = 0; i < count; i++)
            {
                Message message = new Message("testing");
                message.Properties = new Properties() { MessageId = "websocket-test-" + i };
                await sender.SendAsync(message);
            }

            Trace.WriteLine(TraceLevel.Information, "Closing sender...");
            await sender.CloseAsync();

            Trace.WriteLine(TraceLevel.Information, "Receiving messages...");
            ReceiverLink receiver = new ReceiverLink(session, "websocket-receiver-link", this.Entity);
            for (int i = 0; i < count; i++)
            {
                Message message = await receiver.ReceiveAsync();
                if (message == null)
                {
                    break;
                }

                receiver.Accept(message);
            }

            Trace.WriteLine(TraceLevel.Information, "Closing receiver...");
            await receiver.CloseAsync();

            Trace.WriteLine(TraceLevel.Information, "Shutting down...");
            await session.CloseAsync();
            await connection.CloseAsync();
        }
    }
}
