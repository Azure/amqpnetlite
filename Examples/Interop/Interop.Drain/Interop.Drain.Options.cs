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
        private bool   forever;
        private int    count;

        private int    initialCredit;
        private int    resetCredit;
        private bool   quiet;

        public Options(string[] args)
        {
            this.url = "amqp://guest:guest@127.0.0.1:5672";
            this.address = "";
            this.timeout = 1;
            this.forever = false;
            this.count = 1;
            this.initialCredit = 10;
            this.resetCredit = 5;
            this.quiet = false;
            Parse(args);
        }

        private void Usage()
        {
            Console.WriteLine("Usage: interop.drain [OPTIONS] --address STRING");
            Console.WriteLine("");
            Console.WriteLine("Create a connection, attach a receiver to an address, and receive messages.");
            Console.WriteLine("Options:");
            Console.WriteLine(" --broker [amqp://guest:guest@127.0.0.1:5672] - AMQP 1.0 peer connection address");
            Console.WriteLine(" --address STRING     []      - AMQP 1.0 terminus name");
            Console.WriteLine(" --timeout SECONDS    [1]     - time to wait for each message to be received");
            Console.WriteLine(" --forever            [false] - use infinite receive timeout");
            Console.WriteLine(" --count INT          [1]     - receive this many messages and exit; 0 disables count based exit");
            Console.WriteLine(" --initial-credit INT [10]    - receiver initial credit");
            Console.WriteLine(" --reset-credit INT   [5]     - reset credit to initial-credit every reset-credit messages");
            Console.WriteLine(" --quiet              [false] - do not print each message's content");
            Console.WriteLine(" --help                       - print this message and exit");
            Console.WriteLine("");
            Console.WriteLine("Exit codes:");
            Console.WriteLine(" 0 - successfully received all messages");
            Console.WriteLine(" 1 - timeout waiting for a message");
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
                else if (arg == "--forever")
                {
                    this.forever = true;
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
                else if (arg == "--initial-credit")
                {
                    arg = args[++current];
                    int i = int.Parse(arg);
                    if (i >= 0)
                    {
                        this.initialCredit = i;
                    }
                }
                else if (arg == "--reset-credit")
                {
                    arg = args[++current];
                    int i = int.Parse(arg);
                    if (i >= 0)
                    {
                        this.resetCredit = i;
                    }
                }
                else if (arg == "--quiet")
                {
                    this.quiet = true;
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

        public bool Forever
        {
            get { return this.forever; }
        }

        public int Count
        {
            get { return this.count; }
        }

        public int InitialCredit
        {
            get { return this.initialCredit; }
        }

        public int ResetCredit
        {
            get { return this.resetCredit; }
        }

        public bool Quiet
        {
            get { return this.quiet; }
        }
    }
}
