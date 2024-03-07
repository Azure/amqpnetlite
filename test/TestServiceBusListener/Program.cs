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

namespace Test.ServiceBusListener
{
    using System;

    class Program
    {
        static void Usage()
        {
            Console.WriteLine("TestServiceBusListener address");
            Console.WriteLine("  url=amqp|amqps://host[:port]");
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
            var listener = new TestServiceBusListener(new Uri(args[0]));
            listener.Open();

            Console.WriteLine("Listener started. Press the enter key to exit...");
            Console.ReadLine();

            listener.Close();
            Console.WriteLine("Broker stopped");
        }
    }
}
