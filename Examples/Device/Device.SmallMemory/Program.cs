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
using Microsoft.SPOT;
using System.Threading;

namespace Device.SmallMemory
{
    public class Program
    {
        public static void Main()
        {
            Send();
        }

        static void Send()
        {
            const int nMsgs = 800;

            Client client = new Client("localhost", 5672, false, "guest", "guest");

            int count = 0;
            ManualResetEvent done = new ManualResetEvent(false);
            Receiver receiver = client.GetReceiver("q1");
            receiver.Start(20, (r, m) =>
            {
                r.Accept(m);
                if (++count >= nMsgs) done.Set();
            });

            Sender sender = client.GetSender("q1");
            for (int i = 0; i < nMsgs; i++)
            {
                sender.Send(new Message() { Body = "hello" + i });
            }

            done.WaitOne(30000, false);

            sender.Close();
            receiver.Close();
            client.Close();
        }
    }
}