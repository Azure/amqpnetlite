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
using System.Collections.Generic;
using Amqp;
using Amqp.Framing;
using Amqp.Listener;
using Amqp.Types;

namespace PeerToPeer.CustomType
{
    class Program
    {
        private const string Address = "amqp://guest:guest@127.0.0.1:5672";
        private const string MsgProcName = "messages";

        static void Main(string[] args)
        {
            //Create host and register message processor
            var host = new ContainerHost(new Address(Address));
            host.RegisterMessageProcessor(MsgProcName, new MessageProcessor());
            host.Open();

            //Create client
            var connection = new Connection(new Address(Address));
            var session = new Session(connection);
            var sender = new SenderLink(session, "message-client", MsgProcName);

            //Send message with an object of the base class as the body
            var person = new Person() { EyeColor = "brown", Height = 175, Weight = 75 };
            SendMessage(sender, "Person", person);

            //Send message with an object of a derived class as the body
            var student = new Student()
            {
                GPA = 4.8,
                Address = new ListAddress() { Street = "123 Main St.", City = "Big Apple", State = "NY", Zip = "12345" }
            };
            SendMessage(sender, "Person", student);

            //Send message with an object of a derived class as the body
            var teacher = new Teacher()
            {
                Department = "Computer Science",
                Classes = new List<string>() { "CS101", "CS106", "CS210" }
            };
            SendMessage(sender, "Person", teacher);

            //Send message with nested simple map as the body
            var address = new InternationalAddress()
            {
                Address = new MapAddress() { Street = "123 Main St.", City = "Big Apple", State = "NY", Zip = "12345" },
                Country = "usa"
            };
            SendMessage(sender, "InternationalAddress", address);

            //Send message with an AMQP value (the described list form of a student) as the body
            var described = new DescribedValue(
                new Symbol("samples.amqpnetlite:student"),
                new List()
                {
                    80,
                    6,
                    "black",
                    4.9,
                    new DescribedValue(
                        new Symbol("PeerToPeer.CustomType.ListAddress"),
                        new List()
                        {
                            "123 Main St.",
                            "Big Apple",
                            "NY",
                            "12345"
                        }
                    )
                }
            );
            SendMessage(sender, "Person", described);

            //Send message with an AMQP value (simple map of an InternationalAddress) as the body
            var map = new Map()
            {
                { "street", "123 Main St." },
                { "city", "Big Apple" },
                { "state", "NY" },
                { "zip", "12345" }
            };
            SendMessage(sender, "MapAddress", map);

            //Send message with an AMQP value (simple map of an InternationalAddress) as the body
            var map2 = new Map()
            {
                { "address", new Map() { { "street", "123 Main St." }, { "city", "Big Apple" }, { "state", "NY" }, { "zip", "12345" } } },
                { "country", "usa" }
            };
            SendMessage(sender, "InternationalAddress", map2);

            sender.Close();
            session.Close();
            connection.Close();

            host.Close();
        }

        static void SendMessage(SenderLink sender, string subject, object body)
        {
            var message = new Message(body);
            message.Properties = new Properties() { Subject = subject };
            sender.Send(message);
        }
    }
}
