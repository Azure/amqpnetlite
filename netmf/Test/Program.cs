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
using Amqp;
#if NETMF && !NANOFRAMEWORK_1_0
using Microsoft.SPOT;
#elif NANOFRAMEWORK_1_0
using System.Diagnostics;
#endif
#if COMPACT_FRAMEWORK
using System.Diagnostics;
#endif
using AmqpTrace = Amqp.Trace;

namespace Test.Amqp
{
    public class Program
    {
        public static int Main()
        {
            AmqpTrace.TraceLevel = TraceLevel.Output;
            AmqpTrace.TraceListener = Program.WriteTrace;
            Connection.DisableServerCertValidation = true;

            return TestRunner.RunTests();
        }

        static void WriteTrace(TraceLevel level, string format, params object[] args)
        {
            string message = args == null ? format : Fx.Format(format, args);
#if NETMF && !NANOFRAMEWORK_1_0
            Debug.Print(message);
#elif NANOFRAMEWORK_1_0
            Debug.WriteLine(message);
#elif COMPACT_FRAMEWORK
            Debug.WriteLine(message);
#endif
        }
    }
}
