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
using Microsoft.SPOT;
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

            new LinkTests().TestMethod_BasicSendReceive();
            //RunTests();
        }

        static void WriteTrace(string format, params object[] args)
        {
            Debug.Print(Fx.Format(format, args));
        }

        static void OnMessage(ReceiverLink receiver, Message message)
        {
            AmqpTrace.WriteLine(TraceLevel.Information, "receive: {0}", message.ApplicationProperties["action"]);
        }

        static void RunTests()
        {
            Debug.Print("Running all unit tests");
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
                    Debug.Print("Running tests of " + type.Name);

                    object instance = type.GetConstructor(new Type[0]).Invoke(new object[0]);
                    if (testInitialize != null)
                    {
                        Debug.Print("Running TestInitialize");
                        testInitialize.Invoke(instance, null);
                    }

                    for (int i = 0; i < count; i++)
                    {
                        string testName = testMethods[i].Name.Substring(11);
                        Debug.Print("Running test " + testName);

                        try
                        {
                            testMethods[i].Invoke(instance, null);
                            ++passed;
                            Debug.Print("Test " + testName + " passed.");
                        }
                        catch (Exception exception)
                        {
                            ++failed;
                            Debug.Print("Test " + testName + " failed.");
                            Debug.Print(exception.ToString());
                        }
                    }

                    if (testCleanup != null)
                    {
                        Debug.Print("Running TestCleanup");
                        testCleanup.Invoke(instance, null);
                    }
                }
            }

            Debug.Print("Test result: passed: " + passed + ", failed: " + failed);
        }
    }
}
