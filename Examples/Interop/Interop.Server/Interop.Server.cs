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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amqp;
using Amqp.Framing;
using Amqp.Types;

namespace Interop.Server
{
    class Server
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
        // Sample invocation: Interop.Server amqp://guest:guest@localhost:5672
        //
        static int Main(string[] args)
        {
            String url = "amqp://guest:guest@127.0.0.1:5672";
            String requestQueueName = "service_queue";

            if (args.Length > 0)
                url = args[0];

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

                // Create server receiver link
                // When messages arrive, reply to them
                ReceiverLink receiver = new ReceiverLink(session, "Interop.Server-receiver", requestQueueName);
                int linkId = 0;
                while (true)
                {
                    Message request = receiver.Receive();
                    if (null != request)
                    {
                        receiver.Accept(request);
                        String replyTo = request.Properties.ReplyTo;
                        SenderLink sender = new SenderLink(session, "Interop.Server-sender-" + (++linkId).ToString(), replyTo);

                        Message response = new Message(GetContent(request).ToUpper());
                        response.Properties = new Properties() { CorrelationId = request.Properties.MessageId };

                        try
                        {
                            sender.Send(response, 10000);
                        }
                        catch (Exception exception)
                        {
                            Console.WriteLine("Error waiting for response to be sent: {0} exception {1}",
                                GetContent(response), exception.Message);
                            break;
                        }
                        sender.Close();
                        Console.WriteLine("Processed request: {0} -> {1}",
                            GetContent(request), GetContent(response));
                    }
                    else
                    {
                        // timed out waiting for request. This is normal.
                        Console.WriteLine("Timeout waiting for request. Keep waiting...");
                    }
                }
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
