//  ------------------------------------------------------------------------------------
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
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test.Amqp
{
#if (NETFX || DOTNET) && !BUILD_SCRIPT
    [TestClass]
    public class TestSetup
    {
        static Listener.IContainer.TestAmqpBroker broker;

        [AssemblyInitialize]
        public static void Initialize(TestContext context)
        {
            string address = Environment.GetEnvironmentVariable(TestTarget.envVarName);
            if (address == null)
            {
                if (Process.GetProcessesByName("TestAmqpBroker").Length == 0)
                {
                    string[] addresses = new[]
                    {
                    "amqp://localhost:5672",
                    "amqps://localhost:5671",
#if NETFX
                    "ws://localhost:18080"
#endif
                };
                    broker = new Listener.IContainer.TestAmqpBroker(addresses, "guest:guest", "localhost", null);
                    broker.Start();
                }
            }
        }

        [AssemblyCleanup()]
        public static void AssemblyCleanup()
        {
            broker?.Stop();
        }
    }
#endif
}