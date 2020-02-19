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
using Amqp.Listener;
using Test.Amqp;

namespace PeerToPeer.CustomTransport
{
    class Program
    {
        const string address = "pipe://guest:guest@localhost/custom_transport";
        const string nodeName = "messages";

        static void Main(string[] args)
        {
            //Create host and register custom transport listener
            var host = new ContainerHost(new Address(address));
            host.CustomTransports.Add("pipe", NamedPipeTransport.Listener);
            host.RegisterMessageProcessor(nodeName, new MessageProcessor());
            host.Open();
            Console.WriteLine("Listener: running");

            //Create factory with custom transport factory
            var factory = new ConnectionFactory(new TransportProvider[] { NamedPipeTransport.Factory });
            var connection = factory.CreateAsync(new Address(address)).GetAwaiter().GetResult();
            var session = new Session(connection);
            var sender = new SenderLink(session, "message-client", nodeName);
            Console.WriteLine("Client: sending a message");
            sender.Send(new Message("Hello Pipe!"));
            sender.Close();
            session.Close();
            connection.Close();
            Console.WriteLine("Client: closed");

            host.Close();
            Console.WriteLine("Listener: closed");
        }

        class MessageProcessor : IMessageProcessor
        {
            public int Credit
            {
                get { return 300; }
            }

            public void Process(MessageContext messageContext)
            {
                Console.WriteLine("Listener: received '{0}'.", messageContext.Message.Body);
                messageContext.Complete();
            }
        }
    }
}
