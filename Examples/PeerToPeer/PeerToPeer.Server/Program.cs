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
    using System.Threading.Tasks;
    using Amqp;
    using Amqp.Framing;
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

            string requestProcessor = "request_processor";
            host.RegisterRequestProcessor(requestProcessor, new RequestProcessor());
            Console.WriteLine("Request processor is registered on {0}", requestProcessor);

            Console.WriteLine("Press enter key to exist...");
            Console.ReadLine();

            host.Close();
        }

        class RequestProcessor : IRequestProcessor
        {
            int offset;

            void IRequestProcessor.Process(RequestContext requestContext)
            {
                Console.WriteLine("Received a request " + requestContext.Message.Body);
                var task = this.ReplyAsync(requestContext);
            }

            async Task ReplyAsync(RequestContext requestContext)
            {
                if (this.offset == 0)
                {
                    this.offset = (int)requestContext.Message.ApplicationProperties["offset"];
                }

                while (this.offset < 1000)
                {
                    try
                    {
                        Message response = new Message("reply" + this.offset);
                        response.ApplicationProperties = new ApplicationProperties();
                        response.ApplicationProperties["offset"] = this.offset;
                        requestContext.ResponseLink.SendMessage(response, null);
                        this.offset++;
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine("Exception: " + exception.Message);
                        if (requestContext.State == ContextState.Aborted)
                        {
                            Console.WriteLine("Request is aborted. Last offset: " + this.offset);
                            return;
                        }
                    }

                    await Task.Delay(1000);
                }

                requestContext.Complete(new Message("done"));
            }
        }
    }
}
