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
#if NETMF
using Microsoft.SPOT;
#endif
#if COMPACT_FRAMEWORK
using System.Diagnostics;
#endif
using System;
using System.Reflection;
using AmqpTrace = Amqp.Trace;

namespace Test.Amqp
{
    public class Program
    {
        public static void Main()
        {
            AmqpTrace.TraceLevel = TraceLevel.Frame;
            AmqpTrace.TraceListener = Program.WriteTrace;
            Connection.DisableServerCertValidation = true;

            RunTests();
            //new UtilityTests().TestMethod_Address();
        }

        static void WriteTrace(string format, params object[] args)
        {
            string message = args == null ? format : Fx.Format(format, args);
#if NETMF
            Debug.Print(message);
#elif COMPACT_FRAMEWORK
            Debug.WriteLine(message);
#endif
        }

        static void OnMessage(ReceiverLink receiver, Message message)
        {
            AmqpTrace.WriteLine(TraceLevel.Information, "receive: {0}", message.ApplicationProperties["action"]);
        }

        static void RunTests()
        {
            WriteTrace("Running all unit tests");
            Type[] types = Assembly.GetExecutingAssembly().GetTypes();
            int passed = 0;
            int failed = 0;

            foreach (var type in types)
            {
                MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
                MethodInfo[] testMethods = new MethodInfo[methods.Length];
                MethodInfo testInitialize = null;
                MethodInfo testCleanup = null;
                int count = 0;

                foreach (var method in methods)
                {
                    if (method.Name.Equals("TestInitialize"))
                    {
                        testInitialize = method;
                    }
                    else if (method.Name.Equals("TestCleanup"))
                    {
                        testCleanup = method;
                    }
                    else if (method.Name.Length > 11 && method.Name.Substring(0, 11).Equals("TestMethod_"))
                    {
                        testMethods[count++] = method;
                    }
                }

                if (count > 0)
                {
                    WriteTrace("Running tests of " + type.Name);

                    object instance = type.GetConstructor(new Type[0]).Invoke(new object[0]);
                    if (testInitialize != null)
                    {
                        WriteTrace("Running TestInitialize");
                        testInitialize.Invoke(instance, null);
                    }

                    for (int i = 0; i < count; i++)
                    {
                        string testName = testMethods[i].Name.Substring(11);
                        WriteTrace("Running test " + testName);

                        try
                        {
                            testMethods[i].Invoke(instance, null);
                            ++passed;
                            WriteTrace("Test " + testName + " passed.");
                        }
                        catch (Exception exception)
                        {
                            ++failed;
                            WriteTrace("Test " + testName + " failed.");
                            WriteTrace(exception.ToString());
                        }
                    }

                    if (testCleanup != null)
                    {
                        WriteTrace("Running TestCleanup");
                        testCleanup.Invoke(instance, null);
                    }
                }
            }

            WriteTrace("Test result: passed: " + passed + ", failed: " + failed);
        }
    }
}
