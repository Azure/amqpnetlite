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

namespace PeerToPeer.CustomType
{
    class Program
    {
        private const string Address = "amqp://guest:guest@127.0.0.1:5672";
        private const string MsgProcName = "messages";

        static void Main(string[] args)
        {
            //Create host and register message processor
            var uri = new Uri(Address);
            var host = new ContainerHost(new List<Uri>() { uri }, null, uri.UserInfo);
            host.RegisterMessageProcessor(MsgProcName, new MessageProcessor());
            host.Open();

            //Create client
            var connection = new Connection(new Address(Address));
            var session = new Session(connection);
            var sender = new SenderLink(session, "message-client", MsgProcName);

            //Send message with an object of custom type as the body
            var person = new Person() { EyeColor = "brown", Height = 175, Weight = 75 };
            sender.Send(new Message(person));

            sender.Close();
            session.Close();
            connection.Close();

            host.Close();
        }
    }
}
