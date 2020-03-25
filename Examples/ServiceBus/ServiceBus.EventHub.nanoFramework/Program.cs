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
using nanoFramework.Networking;
using ServiceBus.Scenarios;
using System;
using System.Diagnostics;
using AmqpTrace = Amqp.Trace;

namespace ServiceBus.EventHub
{
    public class Program
    {
        public static void Main()
        {
            // setup and connect network
            NetworkHelpers.SetupAndConnectNetwork(true);

            AmqpTrace.TraceLevel = TraceLevel.Information;
            AmqpTrace.TraceListener = (l, f, a) => Debug.WriteLine(Fx.Format(f, a));
            Connection.DisableServerCertValidation = true;

            // wait for network and valid system date time
            NetworkHelpers.IpAddressAvailable.WaitOne();
            NetworkHelpers.DateTimeAvailable.WaitOne();

            try
            {
                new EventHubsExample().Run();
            }
            catch (Exception e)
            {
                AmqpTrace.WriteLine(TraceLevel.Error, e.ToString());
            }
        }
    }
}