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

namespace Examples.Interop
{
    using System;

    public class Options
    {
        private string url;
        private string address;
        private int    timeout;
        private bool   durable;
        private int    count;
        private string id;
        private string replyto;
        private string content;
        private bool   print;

        public Options(string[] args)
        {
            this.url = "amqp://guest:guest@127.0.0.1:5672";
            this.address = "";
            this.timeout = 0;
            this.durable = false;
            this.count = 1;
            this.id = "";
            this.replyto = "";
            this.content = "";
            this.print = false;
            Parse(args);
        }

        private void Usage()
        {
            Console.WriteLine("Usage: Interop.Spout [OPTIONS] --address STRING");
            Console.WriteLine("");
            Console.WriteLine("Create a connection, attach a sender to an address, and send messages.");
            Console.WriteLine("Options:");
            Console.WriteLine(" --broker [amqp://guest:guest@127.0.0.1:5672] - AMQP 1.0 peer connection address");
            Console.WriteLine(" --address STRING  []      - AMQP 1.0 terminus name");
            Console.WriteLine(" --timeout SECONDS [0]     - send for N seconds; 0 disables timeout");
            Console.WriteLine(" --durable         [false] - send messages marked as durable");
            Console.WriteLine(" --count INT       [1]     - send this many messages and exit; 0 disables count based exit");
            Console.WriteLine(" --id STRING       [guid]  - message id");
            Console.WriteLine(" --replyto STRING  []      - message ReplyTo address");
            Console.WriteLine(" --content STRING  []      - message content");
            Console.WriteLine(" --print           [false] - print each message's content");
            Console.WriteLine(" --help                    - print this message and exit");
            Console.WriteLine("");
            Console.WriteLine("Exit codes:");
            Console.WriteLine(" 0 - successfully received all messages");
            Console.WriteLine(" 2 - other error");
            System.Environment.Exit(2);
        }

        private void Parse(string[] args)
        {
            int argCount = args.Length;
            int current = 0;

            while (current < argCount)
            {
                string arg = args[current];
                if (arg == "--help")
                {
                    Usage();
                }
                else if (arg == "--broker")
                {
                    this.url = args[++current];
                }
                else if (arg == "--address")
                {
                    this.address = args[++current];
                }
                else if (arg == "--timeout")
                {
                    arg = args[++current];
                    int i = int.Parse(arg);
                    if (i >= 0)
                    {
                        this.timeout = i;
                    }
                }
                else if (arg == "--durable")
                {
                    this.durable = true;
                }
                else if (arg == "--count")
                {
                    arg = args[++current];
                    int i = int.Parse(arg);
                    if (i >= 0)
                    {
                        this.count = i;
                    }
                }
                else if (arg == "--id")
                {
                    this.id = args[++current];
                }
                else if (arg == "--replyto")
                {
                    this.replyto = args[++current];
                }
                else if (arg == "--content")
                {
                    this.content = args[++current];
                }
                else if (arg == "--print")
                {
                    this.print = true;
                }
                else
                {
                    throw new ArgumentException(String.Format("unknown argument \"{0}\"", arg));
                }

                current++;
            }

            if (this.address.Equals(""))
            {
                throw new ArgumentException("missing argument: --address");
            }
        }

        public string Url
        {
            get { return this.url; }
        }

        public string Address
        {
            get { return this.address; }
        }

        public int Timeout
        {
            get { return this.timeout; }
        }

        public bool Durable
        {
            get { return this.durable; }
        }

        public int Count
        {
            get { return this.count; }
        }

        public string Id
        {
            get { return this.id; }
        }

        public string ReplyTo
        {
            get { return this.replyto; }
        }

        public string Content
        {
            get { return this.content; }
        }

        public bool Print
        {
            get { return this.print; }
        }
    }
}
