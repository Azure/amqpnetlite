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
using Amqp;
using Listener.IContainer;
using AmqpTrace = Amqp.Trace;

namespace Test.Amqp
{
    public class Program
    {
        public static int Main(string[] args)
        {
            AmqpTrace.TraceLevel = TraceLevel.Output;
            AmqpTrace.TraceListener = Program.WriteTrace;
            Connection.DisableServerCertValidation = true;

            TestAmqpBroker broker = null;
            if (args.Length == 0)
            {
                Environment.SetEnvironmentVariable("CoreBroker", "1");
                broker = new TestAmqpBroker(new string[] { LinkTests.AddressString },
                    string.Join(":", LinkTests.address.User, LinkTests.address.Password), null, null);
                broker.Start();
            }
            else
            {
                Environment.SetEnvironmentVariable("CoreBroker", "0");
            }

            try
            {
                return TestRunner.RunTests();
            }
            finally
            {
                if (broker != null)
                {
                    broker.Stop();
                }
            }
        }

        static void WriteTrace(string format, params object[] args)
        {
            string message = args == null ? format : string.Format(format, args);
            Console.WriteLine(message);
        }
    }
}
