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
            Console.WriteLine("Link processor is registered.");

            Console.WriteLine("Start the client");
            var client = new Client(address);
            var task = Task.Run(() => client.Run());

            Console.WriteLine("Press enter key to exit...");
            Console.ReadLine();

            client.Close();
            host.Close();
        }

        class LinkProcessor : ILinkProcessor
        {
            public void Process(AttachContext attachContext)
            {
                if (!attachContext.Attach.Role)
                {
                    attachContext.Complete(new Error(ErrorCode.NotAllowed) { Description = "Only receiver link is allowed." });
                    return;
                }

                string address = ((Source)attachContext.Attach.Source).Address;
                if (!string.Equals("$monitoring", address, StringComparison.OrdinalIgnoreCase))
                {
                    attachContext.Complete(new Error(ErrorCode.NotFound) { Description = "Cannot find address " + address });
                    return;
                }

                Console.WriteLine("A client has connected. LinkName " + attachContext.Attach.LinkName);
                attachContext.Complete(new MonitoringLinkEndpoint(attachContext.Link), 0);
            }
        }

        class MonitoringLinkEndpoint : LinkEndpoint
        {
            ListenerLink link;
            Random random;
            Timer timer;
            int credit;

            public MonitoringLinkEndpoint(ListenerLink link)
            {
                this.link = link;
                this.link.Closed += this.OnLinkClosed;
                this.random = new Random();
                this.timer = new Timer(OnTimer, this, 1000, 1000);
            }

            public override void OnFlow(FlowContext flowContext)
            {
                Interlocked.Add(ref this.credit, flowContext.Messages);
            }

            public override void OnDisposition(DispositionContext dispositionContext)
            {
            }

            static void OnTimer(object state)
            {
                var thisPtr = (MonitoringLinkEndpoint)state;
                if (Interlocked.Decrement(ref thisPtr.credit) >= 0)
                {
                    var message = new Message(new Map()
                        {
                            { "CPU", thisPtr.random.Next(10, 100) },
                            { "Memory", thisPtr.random.Next(2, 2048) }
                        });
                    message.Properties = new Properties();
                    message.Properties.MessageId = "Machine Status";

                    try
                    {
                        thisPtr.link.SendMessage(message);
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine("Exception: " + exception.Message);
                    }
                }
                else
                {
                    Interlocked.Increment(ref thisPtr.credit);
                }
            }

            void OnLinkClosed(IAmqpObject sender, Error error)
            {
                Console.WriteLine("A client has disconnected.");
                this.timer.Dispose();
            }
        }

        class Client
        {
            readonly Address address;
            Connection connection;

            public Client(Address address)
            {
                this.address = address;
            }

            public void Run()
            {
                this.connection = new Connection(this.address);
                var session = new Session(connection);
                var receiver = new ReceiverLink(session, "monitoring-receiver", "$monitoring");
                while (true)
                {
                    var message = receiver.Receive();
                    if (message == null)
                    {
                        Console.WriteLine("Client exiting.");
                        break;
                    }

                    receiver.Accept(message);
                    Console.WriteLine("Received " + message.Body);
                }
            }

            public void Close()
            {
                if (this.connection != null)
                {
                    this.connection.Close();
                }
            }
        }
    }
}
