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

namespace PeerToPeer.Server
{
    using System;
    using Amqp;
    using Amqp.Listener;

    class Program
    {
        static void Main(string[] args)
        {
            string address = "amqp://guest:guest@127.0.0.1:5672";
            if (args.Length > 0)
            {
                address = args[0];
            }

            // uncomment the following to write frame traces
            //Trace.TraceLevel = TraceLevel.Frame;
            //Trace.TraceListener = (f, a) => Console.WriteLine(DateTime.Now.ToString("[hh:ss.fff]") + " " + string.Format(f, a));

            Uri addressUri = new Uri(address);
            ContainerHost host = new ContainerHost(new Uri[] { addressUri }, null, addressUri.UserInfo);
            host.Open();
            Console.WriteLine("Container host is listening on {0}:{1}", addressUri.Host, addressUri.Port);

            string messageProcessor = "message_processor";
            host.RegisterMessageProcessor(messageProcessor, new MessageProcessor());
            Console.WriteLine("Message processor is registered on {0}", messageProcessor);

            string requestProcessor = "request_processor";
            host.RegisterRequestProcessor(requestProcessor, new RequestProcessor());
            Console.WriteLine("Request processor is registered on {0}", requestProcessor);

            Console.WriteLine("Press enter key to exist...");
            Console.ReadLine();

            host.Close();
        }

        static void PrintMessage(Message message)
        {
            if (message.Header != null) Console.WriteLine(message.Header.ToString());
            if (message.DeliveryAnnotations != null) Console.WriteLine(message.DeliveryAnnotations.ToString());
            if (message.MessageAnnotations != null) Console.WriteLine(message.MessageAnnotations.ToString());
            if (message.Properties != null) Console.WriteLine(message.Properties.ToString());
            if (message.ApplicationProperties != null) Console.WriteLine(message.ApplicationProperties.ToString());
            if (message.BodySection != null) Console.WriteLine("body:{0}", message.Body.ToString());
            if (message.Footer != null) Console.WriteLine(message.Footer.ToString());
        }

        class MessageProcessor : IMessageProcessor
        {
            void IMessageProcessor.Process(MessageContext messageContext)
            {
                Console.WriteLine("Received a message.");
                PrintMessage(messageContext.Message);

                messageContext.Complete();
            }
        }

        class RequestProcessor : IRequestProcessor
        {
            void IRequestProcessor.Process(RequestContext requestContext)
            {
                Console.WriteLine("Received a request.");
                PrintMessage(requestContext.Message);

                Message response = new Message("welcome");
                requestContext.Complete(response);
            }
        }
    }
}
