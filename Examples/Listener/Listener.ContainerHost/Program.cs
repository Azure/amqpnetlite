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

namespace Listener.ContainerHost
{
    using Amqp;
    using Amqp.Framing;
    using Amqp.Listener;
    using Amqp.Types;
    using System;
    using System.Threading.Tasks;

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

            host.RegisterLinkProcessor(new LinkProcessor());
            Console.WriteLine("Link processor is registered");

            Console.WriteLine("Press enter key to exit...");
            Console.ReadLine();

            host.Close();
        }

        class LinkProcessor : ILinkProcessor
        {
            public void Process(AttachContext attachContext)
            {
                // start a task to process this request
                var task = this.ProcessAsync(attachContext);
            }

            async Task ProcessAsync(AttachContext attachContext)
            {
                // simulating an async operation required to complete the task
                await Task.Delay(100);

                if (attachContext.Attach.LinkName == "")
                {
                    // how to fail the attach request
                    attachContext.Complete(new Error() { Condition = ErrorCode.InvalidField, Description = "Empty link name not allowed." });
                }
                else if (attachContext.Link.Role)
                {
                    attachContext.Complete(new IncomingLinkEndpoint(), 300);
                }
                else
                {
                    attachContext.Complete(new OutgoingLinkEndpoint(), 0);
                }
            }
        }

        class IncomingLinkEndpoint : LinkEndpoint
        {
            public override void OnMessage(MessageContext messageContext)
            {
                // this can also be done when an async operation, if required, is done
                messageContext.Complete();
            }

            public override void OnFlow(FlowContext flowContext)
            {
            }

            public override void OnDisposition(DispositionContext dispositionContext)
            {
            }
        }

        class OutgoingLinkEndpoint : LinkEndpoint
        {
            public override void OnFlow(FlowContext flowContext)
            {
                for (int i = 0; i < flowContext.Messages; i++)
                {
                    var message = new Message("Hello!");
                    message.Properties = new Properties() { Subject = "Welcome Message" };
                    flowContext.Link.SendMessage(message);
                }
            }

            public override void OnDisposition(DispositionContext dispositionContext)
            {
                if (!dispositionContext.Settled)
                {
                    dispositionContext.Link.DisposeMessage(dispositionContext.Message, new Accepted(), true);
                }
            }
        }
    }
}
