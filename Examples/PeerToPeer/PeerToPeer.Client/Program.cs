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

namespace PeerToPeer.Client
{
    using System;
    using Amqp;
    using Amqp.Framing;

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

            Console.WriteLine("Running message client...");
            RunMessageClient(address);

            Console.WriteLine("Running request client...");
            RunRequestClient(address);
        }

        static void RunMessageClient(string address)
        {
            const int nMsgs = 10;
            Connection connection = new Connection(new Address(address));
            Session session = new Session(connection);
            SenderLink sender = new SenderLink(session, "message-client", "message_processor");

            for (int i = 0; i < nMsgs; ++i)
            {
                Message message = new Message("hello");
                message.Properties = new Properties() { MessageId = "msg" + i };
                message.ApplicationProperties = new ApplicationProperties();
                message.ApplicationProperties["sn"] = i;
                sender.Send(message);
                Console.WriteLine("Sent message {0} body {1}", message.Properties, message.Body);
            }

            sender.Close();
            session.Close();
            connection.Close();
        }

        static void RunRequestClient(string address)
        {
            Connection connection = new Connection(new Address(address));
            Session session = new Session(connection);

            string replyTo = "client-reply-to";
            Attach recvAttach = new Attach()
            {
                Source = new Source() { Address = "request_processor" },
                Target = new Target() { Address = replyTo }
            };

            ReceiverLink receiver = new ReceiverLink(session, "request-client-receiver", recvAttach, null);
            SenderLink sender = new SenderLink(session, "request-client-sender", "request_processor");

            Message request = new Message("hello");
            request.Properties = new Properties() { MessageId = "request1", ReplyTo = replyTo };
            sender.Send(request, null, null);
            Console.WriteLine("Sent request {0} body {1}", request.Properties, request.Body);

            Message response = receiver.Receive();
            Console.WriteLine("Received response: {0} body {1}", response.Properties, response.Body);
            receiver.Accept(response);

            receiver.Close();
            sender.Close();
            session.Close();
            connection.Close();
        }
    }
}
