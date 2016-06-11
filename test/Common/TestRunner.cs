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
            Assembly assembly = GetAssembly();
            AmqpTrace.WriteLine(TraceLevel.Output, "Running all unit tests in {0}", assembly.FullName);
            Type[] types = assembly.GetTypes();
            int passed = 0;
            int failed = 0;

            AmqpTrace.WriteLine(TraceLevel.Output, "Results\t\tTest");
            AmqpTrace.WriteLine(TraceLevel.Output, "-------\t\t--------");

            foreach (var type in types)
            {
                MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
                MethodInfo[] testMethods = new MethodInfo[methods.Length];
                MethodInfo testClassInitialize = null;
                MethodInfo testClassCleanup = null;
                MethodInfo testInitialize = null;
                MethodInfo testCleanup = null;
                int count = 0;

                foreach (var method in methods)
                {
                    if (method.Name == "ClassInitialize")
                    {
                        testClassInitialize = method;
                    }
                    else if (method.Name == "ClassCleanup")
                    {
                        testClassCleanup = method;
                    }
                    else if (method.Name == "TestInitialize")
                    {
                        testInitialize = method;
                    }
                    else if (method.Name == "TestCleanup")
                    {
                        testCleanup = method;
                    }
                    else if (IsTestMethod(method))
                    {
                        testMethods[count++] = method;
                    }
                }

                if (count > 0)
                {
                    object instance = type.GetConstructor(new Type[0]).Invoke(new object[0]);

                    if (testClassInitialize != null)
                    {
                        try
                        {
                            testClassInitialize.Invoke(instance, null);
                        }
                        catch(Exception exception)
                        {
                            failed += count;
                            AmqpTrace.WriteLine(TraceLevel.Output, exception.ToString());
                            continue;
                        }
                    }

                    for (int i = 0; i < count; i++)
                    {
                        string testName = type.Name + "." + testMethods[i].Name;

                        try
                        {
                            if (testInitialize != null)
                            {
                                testInitialize.Invoke(instance, null);
                            }

                            testMethods[i].Invoke(instance, null);

                            if (testCleanup != null)
                            {
                                testCleanup.Invoke(instance, null);
                            }

                            passed++;
                            AmqpTrace.WriteLine(TraceLevel.Output, "Passed\t\t{0}", testName);
                        }
                        catch (Exception exception)
                        {
                            failed++;
                            AmqpTrace.WriteLine(TraceLevel.Output, "Failed\t\t{0}", testName);
                            AmqpTrace.WriteLine(TraceLevel.Output, exception.ToString());
                        }
                    }

                    if (testClassCleanup != null)
                    {
                        try
                        {
                            testClassCleanup.Invoke(instance, null);
                        }
                        catch(Exception exception)
                        {
                            AmqpTrace.WriteLine(TraceLevel.Output, exception.ToString());
                            continue;
                        }
                    }
                }
            }

            AmqpTrace.WriteLine(TraceLevel.Output, "{0}/{1} test(s) Passed, {2} Failed", passed, passed + failed, failed);

            return failed;
        }

        static Assembly GetAssembly()
        {
#if DOTNET
            return typeof(TestRunner).Assembly();
#else
            return typeof(TestRunner).Assembly;
#endif
        }

        static bool IsTestMethod(MethodInfo mi)
        {
#if DOTNET
            return mi.GetCustomAttribute<Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute>(true) != null;
#else
            return mi.Name.Length > 11 && mi.Name.Substring(0, 11) == "TestMethod_";
#endif
        }
    }
}
