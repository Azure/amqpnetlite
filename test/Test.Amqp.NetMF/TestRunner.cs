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
using System.Reflection;
using Amqp;
using AmqpTrace = Amqp.Trace;

namespace Test.Amqp
{
    public class TestRunner
    {
        public static int RunTests()
        {
#if DOTNET
            Assembly assembly = typeof(TestRunner).Assembly();
#else
            Assembly assembly = typeof(TestRunner).Assembly;
#endif
            AmqpTrace.WriteLine(TraceLevel.Information, "Running all unit tests in {0}", assembly.FullName);
            Type[] types = assembly.GetTypes();
            int passed = 0;
            int failed = 0;

            AmqpTrace.WriteLine(TraceLevel.Information, "Results\t\tTest");
            AmqpTrace.WriteLine(TraceLevel.Information, "-------\t\t--------");

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
                    object instance = type.GetConstructor(new Type[0]).Invoke(new object[0]);
                    if (testInitialize != null)
                    {
                        testInitialize.Invoke(instance, null);
                    }

                    for (int i = 0; i < count; i++)
                    {
                        string testName = type.Name + "." + testMethods[i].Name;

                        try
                        {
                            testMethods[i].Invoke(instance, null);
                            ++passed;
                            AmqpTrace.WriteLine(TraceLevel.Information, "Passed\t\t{0}", testName);
                        }
                        catch (Exception exception)
                        {
                            ++failed;
                            AmqpTrace.WriteLine(TraceLevel.Information, "Failed\t\t{0}", testName);
                            AmqpTrace.WriteLine(TraceLevel.Information, exception.ToString());
                        }
                    }

                    if (testCleanup != null)
                    {
                        testCleanup.Invoke(instance, null);
                    }
                }
            }

            AmqpTrace.WriteLine(TraceLevel.Information, "{0}/{1} test(s) Passed, {2} Failed", passed, passed + failed, failed);

            return failed;
        }
    }
}
