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
using Amqp;
using Amqp.Listener;
using System.Reflection;

namespace PeerToPeer.CustomType
{
    public class MessageProcessor : IMessageProcessor
    {
        public int Credit
        {
            get { return 300; }
        }

        public void Process(MessageContext messageContext)
        {
            object body = null;
            var subject = messageContext.Message.Properties.Subject;
            switch(subject)
            {
                case "Person":
                    body = messageContext.Message.GetBody<Person>();
                    break;
                case "MapAddress":
                    body = messageContext.Message.GetBody<MapAddress>();
                    break;
                case "InternationalAddress":
                    body = messageContext.Message.GetBody<InternationalAddress>();
                    break;
                default:
                    break;
            }

            if (body == null)
            {
                Console.WriteLine("Received a message with unknown type {0} in subject.", subject);
            }
            else
            {
                Console.WriteLine("Received a message with body {0}", body.GetType().Name);
            }

            messageContext.Complete();
        }
    }
}
