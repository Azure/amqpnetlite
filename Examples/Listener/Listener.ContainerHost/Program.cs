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
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Amqp;
    using Amqp.Framing;
    using Amqp.Listener;
    using Amqp.Types;

    class Program
    {
        static void Main(string[] args)
        {
            Address address = new Address("amqp://guest:guest@127.0.0.1:5672");
            if (args.Length > 0)
            {
                address = new Address(args[0]);
            }

            // uncomment the following to write frame traces
            //Trace.TraceLevel = TraceLevel.Frame;
            //Trace.TraceListener = (l, f, a) => Console.WriteLine(DateTime.Now.ToString("[hh:mm:ss.fff]") + " " + string.Format(f, a));

            ContainerHost host = new ContainerHost(address);
            host.Open();
            Console.WriteLine("Container host is listening on {0}:{1}", address.Host, address.Port);

            host.RegisterLinkProcessor(new LinkProcessor());
            Console.WriteLine("Link processor is registered");

            Console.WriteLine("Press enter key to exit...");
            Console.ReadLine();

            host.Close();
        }

        class LinkProcessor : ILinkProcessor
        {
            SharedLinkEndpoint sharedLinkEndpoint = new SharedLinkEndpoint();

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
                    attachContext.Complete(new Error(ErrorCode.InvalidField) { Description = "Empty link name not allowed." });
                }
                else if (attachContext.Link.Role)
                {
                    var target = attachContext.Attach.Target as Target;
                    if (target != null)
                    {
                        if (target.Address == "slow-queue")
                        {
                            // how to do manual link flow control
                            new SlowLinkEndpoint(attachContext);
                        }
                        else if (target.Address == "shared-queue")
                        {
                            // how to do flow control across links
                            this.sharedLinkEndpoint.AttachLink(attachContext);
                        }
                        else
                        {
                            // default link flow control
                            attachContext.Complete(new IncomingLinkEndpoint(), 300);
                        }
                    }
                }
                else
                {
                    attachContext.Complete(new OutgoingLinkEndpoint(), 0);
                }
            }
        }

        class SlowLinkEndpoint : LinkEndpoint
        {
            ListenerLink link;
            CancellationTokenSource cts;

            public SlowLinkEndpoint(AttachContext attachContext)
            {
                this.link = attachContext.Link;
                this.cts = new CancellationTokenSource();
                link.Closed += (o, e) => this.cts.Cancel();
                attachContext.Complete(this, 0);
                this.link.SetCredit(1, false, false);
            }

            public override void OnMessage(MessageContext messageContext)
            {
                messageContext.Complete();

                // delay 1s for the next message
                Task.Delay(1000, this.cts.Token).ContinueWith(
                    t =>
                    {
                        if (!t.IsCanceled)
                        {
                            this.link.SetCredit(1, false, false);
                        }
                    });
            }

            public override void OnFlow(FlowContext flowContext)
            {
            }

            public override void OnDisposition(DispositionContext dispositionContext)
            {
            }
        }

        class SharedLinkEndpoint : LinkEndpoint
        {
            const int Capacity = 5; // max concurrent request
            SemaphoreSlim semaphore;
            ConcurrentDictionary<Link, CancellationTokenSource> ctsMap;

            public SharedLinkEndpoint()
            {
                this.semaphore = new SemaphoreSlim(Capacity);
                this.ctsMap = new ConcurrentDictionary<Link, CancellationTokenSource>();
            }

            public void AttachLink(AttachContext attachContext)
            {
                this.semaphore.WaitAsync(30000).ContinueWith(
                    t =>
                    {
                        if (!t.Result)
                        {
                            attachContext.Complete(new Error(ErrorCode.ResourceLimitExceeded));
                        }
                        else
                        {
                            this.semaphore.Release();
                            attachContext.Complete(this, 1);
                        }
                    });
            }

            public override void OnMessage(MessageContext messageContext)
            {
                this.semaphore.WaitAsync(30000).ContinueWith(
                    t =>
                    {
                        if (!t.Result)
                        {
                            messageContext.Complete(new Error(ErrorCode.ResourceLimitExceeded));
                        }
                        else
                        {
                            this.semaphore.Release();
                            messageContext.Complete();
                        }
                    });
            }

            public override void OnFlow(FlowContext flowContext)
            {
            }

            public override void OnDisposition(DispositionContext dispositionContext)
            {
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
            long id;

            public override void OnFlow(FlowContext flowContext)
            {
                for (int i = 0; i < flowContext.Messages; i++)
                {
                    var message = new Message("Hello!");
                    message.Properties = new Properties() { Subject = "Message" + Interlocked.Increment(ref this.id) };
                    flowContext.Link.SendMessage(message);
                }
            }

            public override void OnDisposition(DispositionContext dispositionContext)
            {
                if (!(dispositionContext.DeliveryState is Accepted))
                {
                    // Handle the case where message is not accepted by the client
                }

                dispositionContext.Complete();
            }
        }
    }
}
