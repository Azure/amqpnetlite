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
using Test.Common;

namespace LongHaulTest
{
    class Program
    {
        static void Main(string[] args)
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

        static void Run(string[] args)
        {
            bool writeUsage = false;
            IRunnable role = null;
            CommonArguments commonArgs = null;

            if (args.Length > 0)
            {
                if (args.Length == 1)
                {
                    writeUsage = true;
                }

                if (string.Equals("send", args[0], StringComparison.OrdinalIgnoreCase))
                {
                    Publisher publisher = new Publisher(args, 1);
                    commonArgs = publisher.Args;
                    role = publisher;
                }
                else if (string.Equals("receive", args[0], StringComparison.OrdinalIgnoreCase))
                {
                    Consumer consumer = new Consumer(args, 1);
                    commonArgs = consumer.Args;
                    role = consumer;
                }
                else
                {
                    writeUsage = true;
                }
            }
            else
            {
                writeUsage = true;
            }

            if (writeUsage)
            {
                if (commonArgs == null)
                {
                    Console.WriteLine("LongHaulText.ext send|receive [options]");
                }
                else
                {
                    Console.WriteLine(Arguments.PrintArguments(commonArgs.GetType()));
                }
            }
            else
            {
                role.Run();
            }
        }
    }
}
