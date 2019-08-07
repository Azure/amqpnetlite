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
using System.Threading;
using Amqp;
using Amqp.Framing;

namespace PeerToPeer.PairedLinkClient
{
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
            //Trace.TraceListener = (l, f, a) => Console.WriteLine(DateTime.Now.ToString("[hh:mm:ss.fff]") + " " + string.Format(f, a));

            Console.WriteLine("Running request client...");
            new Client(address).Run();
        }

        class Client
        {
            readonly string address;
            string replyTo;
            Connection connection;
            Session session;
            int offset;
            private InitiatorPairedLink pairedLink;

            public Client(string address)
            {
                this.address = address;
                this.replyTo = "client-" + Guid.NewGuid().ToString();
            }

            public void Run()
            {
                while (true)
                {
                    try
                    {
                        this.Cleanup();
                        this.Setup();

                        this.RunOnce();

                        this.Cleanup();
                        break;
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine("Reconnect on exception: " + exception.Message);

                        Thread.Sleep(5000);
                    }
                }
            }

            void Setup()
            {
                this.connection = new Connection(new Address(address));
                this.session = new Session(connection);


                var remoteSource = new Source() { Address = "request_processor" };
                var remoteTarget = new Target() { Address = "request_processor" };

                this.pairedLink =
                    new InitiatorPairedLink(session, "request-client", remoteTarget, remoteSource);
                this.pairedLink.Start(300);
            }

            void Cleanup()
            {
                var temp = Interlocked.Exchange(ref this.connection, null);
                if (temp != null)
                {
                    temp.Close();
                }
            }

            void RunOnce()
            {
                Message request = new Message("hello " + this.offset);
                request.Properties = new Properties() { MessageId = "command-request", ReplyTo = "$me" };
                request.ApplicationProperties = new ApplicationProperties();
                request.ApplicationProperties["offset"] = this.offset;
                pairedLink.Sender.Send(request, null, null);
                Console.WriteLine("Sent request {0} body {1}", request.Properties, request.Body);

                while (true)
                {
                    Message response = pairedLink.Receiver.Receive();
                    if (response != null)
                    {
                        pairedLink.Receiver.Accept(response);
                        Console.WriteLine("Received response: {0} body {1}", response.Properties, response.Body);

                        if (string.Equals("done", response.Body))
                        {
                            break;
                        }

                        this.offset = (int) response.ApplicationProperties["offset"];
                    }
                }
            }
        }
    }

    

}
