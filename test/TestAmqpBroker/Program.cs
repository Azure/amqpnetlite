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

namespace TestAmqpBroker
{
    using Amqp;
    using Listener.IContainer;
    using System;
    using System.Collections.Generic;

    class Program
    {
        static void Usage()
        {
            Console.WriteLine("AmqpTestBroker url [url] [/creds:user:pwd] [/cert:ssl_cert] [/trace:level] [/queues:q1;q2;...]");
            Console.WriteLine("  url=amqp|amqps://host[:port] (can be multiple)");
            Console.WriteLine("  creds=username:passwrod");
            Console.WriteLine("  cert=ssl cert find value (thumbprint or subject)");
            Console.WriteLine("  trace=level (info, warn, error, frame)");
            Console.WriteLine("  queues: semicolon separated queue names. If not specified, the broker implicitly");
            Console.WriteLine("          creates a new node for any address and deletes it when the connection is closed.");
        }

        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Usage();
            }
            else
            {
                try
                {
                    Run(args);
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception.ToString());
                }
            }
        }

        static void Run(string[] args)
        {
            List<string> endpoints = new List<string>();
            string creds = null;
            string trace = null;
            string sslValue = null;
            string[] queues = null;
            bool parseEndpoint = true;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i][0] != '/' && parseEndpoint)
                {
                    endpoints.Add(args[i]);
                }
                else
                {
                    parseEndpoint = false;
                    if (args[i].StartsWith("/creds:", StringComparison.OrdinalIgnoreCase))
                    {
                        creds = args[i].Substring(7);
                    }
                    else if (args[i].StartsWith("/trace:", StringComparison.OrdinalIgnoreCase))
                    {
                        trace = args[i].Substring(7);
                    }
                    else if (args[i].StartsWith("/queues:", StringComparison.OrdinalIgnoreCase))
                    {
                        queues = args[i].Substring(8).Split(';');
                    }
                    else if (args[i].StartsWith("/cert:", StringComparison.OrdinalIgnoreCase))
                    {
                        sslValue = args[i].Substring(6);
                    }
                    else
                    {
                        Console.WriteLine("Unknown argument: {0}", args[i]);
                        Usage();
                        return;
                    }
                }
            }

            if (trace != null)
            {
                TraceLevel level = 0;
                switch (trace)
                {
                    case "info":
                        level = TraceLevel.Information;
                        break;
                    case "warn":
                        level = TraceLevel.Warning;
                        break;
                    case "error":
                        level = TraceLevel.Error;
                        break;
                    case "verbose":
                        level = TraceLevel.Verbose;
                        break;
                    case "frame":
                        level = TraceLevel.Frame;
                        break;
                    default:
                        Usage();
                        return;
                }

                Trace.TraceLevel = level;
                Trace.TraceListener = (f, a) => Console.WriteLine(DateTime.Now.ToString("[hh:mm:ss.fff]") + " " + string.Format(f, a));
            }

            var broker = new TestAmqpBroker(endpoints, creds, sslValue, queues);
            broker.Start();

            Console.WriteLine("Broker started. Press the enter key to exit...");
            Console.ReadLine();

            broker.Stop();
            Console.WriteLine("Broker stopped");
        }
    }
}
