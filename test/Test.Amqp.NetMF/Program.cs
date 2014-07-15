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
            AmqpTrace.TraceLevel = TraceLevel.Frame | TraceLevel.Verbose;
            AmqpTrace.TraceListener = Program.WriteTrace;
            Connection.DisableServerCertValidation = true;

            RunTests();
            //new UtilityTests().TestMethod_Address();
        }

        static void WriteTrace(string format, params object[] args)
        {
#if NETMF
            Debug.Print(Fx.Format(format, args));
#elif COMPACT_FRAMEWORK
            Debug.WriteLine(Fx.Format(format, args));
#endif
        }

        static void OnMessage(ReceiverLink receiver, Message message)
        {
            AmqpTrace.WriteLine(TraceLevel.Information, "receive: {0}", message.ApplicationProperties["action"]);
        }

        static void RunTests()
        {
#if NETMF
            Debug.Print("Running all unit tests");
#elif COMPACT_FRAMEWORK
            Debug.WriteLine("Running all unit tests");
#endif
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
#if NETMF
                    Debug.Print("Running tests of " + type.Name);
#elif COMPACT_FRAMEWORK
                    Debug.WriteLine("Running tests of " + type.Name);
#endif

                    object instance = type.GetConstructor(new Type[0]).Invoke(new object[0]);
                    if (testInitialize != null)
                    {
#if NETMF
                        Debug.Print("Running TestInitialize");
#elif COMPACT_FRAMEWORK
                        Debug.WriteLine("Running TestInitialize");
#endif
                        testInitialize.Invoke(instance, null);
                    }

                    for (int i = 0; i < count; i++)
                    {
                        string testName = testMethods[i].Name.Substring(11);
#if NETMF
                        Debug.Print("Running test " + testName);
#elif COMPACT_FRAMEWORK
                        Debug.WriteLine("Running test " + testName);
#endif

                        try
                        {
                            testMethods[i].Invoke(instance, null);
                            ++passed;
#if NETMF
                            Debug.Print("Test " + testName + " passed.");
#elif COMPACT_FRAMEWORK
                            Debug.WriteLine("Test " + testName + " passed.");
#endif
                        }
                        catch (Exception exception)
                        {
                            ++failed;
#if NETMF
                            Debug.Print("Test " + testName + " failed.");
                            Debug.Print(exception.ToString());
#elif COMPACT_FRAMEWORK
                            Debug.WriteLine("Test " + testName + " failed.");
                            Debug.WriteLine(exception.ToString());
#endif
                        }
                    }

                    if (testCleanup != null)
                    {
#if NETMF
                        Debug.Print("Running TestCleanup");
#elif COMPACT_FRAMEWORK
                        Debug.WriteLine("Running TestCleanup");
#endif
                        testCleanup.Invoke(instance, null);
                    }
                }
            }
#if NETMF
            Debug.Print("Test result: passed: " + passed + ", failed: " + failed);
#elif COMPACT_FRAMEWORK
            Debug.WriteLine("Test result: passed: " + passed + ", failed: " + failed);
#endif
        }
    }
}
