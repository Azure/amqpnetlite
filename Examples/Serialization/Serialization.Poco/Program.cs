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

namespace Serialization.Poco
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Amqp;
    using Amqp.Framing;
    using Amqp.Serialization;
    using Amqp.Types;

    class Program
    {
        static void Main(string[] args)
        {
            string address = "amqp://guest:guest@127.0.0.1:5672";
            if (args.Length > 0)
            {
                address = args[0];
            }

            RunAsync(address).GetAwaiter().GetResult();
        }

        static async Task RunAsync(string address)
        {
            var serializer = new AmqpSerializer(new PocoContractResolver()
            {
                PrefixList = new[] { "Serialization.Poco" }
            });

            try
            {
                Connection connection = await Connection.Factory.CreateAsync(new Address(address));
                Session session = new Session(connection);
                SenderLink sender = new SenderLink(session, "sender", "q1");
                Shape shape1 = new Circle()
                {
                    Id = Guid.NewGuid(),
                    Attributes = new Dictionary<string, object>()
                    {
                        { "Time", DateTime.UtcNow },
                        { "Source", "amqpnetlite.samples" }
                    },
                    Radius = 5.3
                };
                Console.WriteLine("Sending {0}", shape1);
                await sender.SendAsync(new Message() { BodySection = new AmqpValue<object>(shape1, serializer) });

                Shape shape2 = new Rectangle()
                {
                    Id = Guid.NewGuid(),
                    Attributes = new Dictionary<string, object>()
                    {
                        { "Color", new Symbol("blue") },
                        { "Line", true }
                    },
                    Width = 8,
                    Height = 10
                };
                Console.WriteLine("Sending {0}", shape2);
                await sender.SendAsync(new Message() { BodySection = new AmqpValue<object>(shape2, serializer) });

                await connection.CloseAsync();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.ToString());
                return;
            }

            try
            {
                Connection connection = await Connection.Factory.CreateAsync(new Address(address));
                Session session = new Session(connection);
                ReceiverLink receiver = new ReceiverLink(session, "receiver", "q1");
                using (Message message = await receiver.ReceiveAsync())
                {
                    Shape shape = message.GetBody<Shape>(serializer);
                    Console.WriteLine("Received {0}", shape);
                    receiver.Accept(message);
                }
                using (Message message = await receiver.ReceiveAsync())
                {
                    Shape shape = message.GetBody<Shape>(serializer);
                    Console.WriteLine("Received {0}", shape);
                    receiver.Accept(message);
                }
                await connection.CloseAsync();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.ToString());
            }
        }
    }
}
