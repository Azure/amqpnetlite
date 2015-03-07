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
using Amqp.Types;

namespace Examples.Interop
{
    class Client
    {
        //
        // Return message as string.
        //
        static String GetContent(Message msg)
        {
            object body = msg.Body;
            return body == null ? null : body.ToString();
        }

        //
        // Sample invocation: Interop.Client amqp://guest:guest@localhost:5672 [loopcount]
        //
        static int Main(string[] args)
        {
            String url = "amqp://guest:guest@localhost:5672";
            String requestQueueName = "service_queue";
            int loopcount = 1;

            if (args.Length > 0)
                url = args[0];
            if (args.Length > 1)
                loopcount = Convert.ToInt32(args[1]);

            Connection.DisableServerCertValidation = true;
            // uncomment the following to write frame traces
            //Trace.TraceLevel = TraceLevel.Frame;
            //Trace.TraceListener = (f, a) => Console.WriteLine(DateTime.Now.ToString("[hh:ss.fff]") + " " + string.Format(f, a));

            Connection connection = null;
            try
            {
                Address address = new Address(url);
                connection = new Connection(address);
                Session session = new Session(connection);

                // Sender attaches to fixed request queue name
                SenderLink sender  = new SenderLink(session, "Interop.Client-sender", requestQueueName);

                // Receiver attaches to dynamic address.
                // Discover its name when it attaches.
                String replyTo = "";
                ManualResetEvent receiverAttached = new ManualResetEvent(false);
                OnAttached onReceiverAttached = (l, a) =>
                {
                    replyTo = ((Source)a.Source).Address;
                    receiverAttached.Set();
                };

                // Create receiver and wait for it to attach.
                ReceiverLink receiver = new ReceiverLink(
                    session, "Interop.Client-receiver", new Source() { Dynamic = true }, onReceiverAttached);
                if (receiverAttached.WaitOne(10000))
                {
                    // Receiver is attached.
                    // Send a series of requests, gather and print responses.
                    String[] requests = new String[] {
                        "Twas brillig, and the slithy toves",
                        "Did gire and gymble in the wabe.",
                        "All mimsy were the borogoves,",
                        "And the mome raths outgrabe."
                    };

                    for (int j = 0; j < loopcount; j++)
                    {
                        Console.WriteLine("Pass {0}", j);
                        for (int i = 0; i < requests.Length; i++)
                        {
                            Message request = new Message(requests[i]);
                            request.Properties = new Properties() { MessageId = "request" + i, ReplyTo = replyTo };
                            sender.Send(request);
                            Message response = receiver.Receive(10000);
                            if (null != response)
                            {
                                receiver.Accept(response);
                                Console.WriteLine("Processed request: {0} -> {1}",
                                    GetContent(request), GetContent(response));
                            }
                            else
                            {
                                Console.WriteLine("Receiver timeout receiving response {0}", i);
                                break;
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Receiver attach timeout");
                }
                receiver.Close();
                sender.Close();
                session.Close();
                connection.Close();
                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception {0}.", e);
                if (null != connection)
                {
                    connection.Close();
                }
            }
            return 1;
        }
    }
}
