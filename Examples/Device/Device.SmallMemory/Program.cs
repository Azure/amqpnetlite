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
using System.Threading;

namespace Device.SmallMemory
{
    public class Program
    {
        // this example program connects to an Azure IoT hub sends a couple of messages and waits for messages from


        // replace with IoT Hub name
        const string iotHubName = "<replace>";

        // replace with device name 
        const string device = "<replace>";

        // user/pass to be authenticated with Azure IoT hub
        // if using a shared access signature like SharedAccessSignature sr=myhub.azure-devices.net&sig=H4Rm2%2bjdBr84lq5KOddD9YpOSC8s7ZSe9SygErVuPe8%3d&se=1444444444&skn=iothubowner
        // user will be iothubowner and password the complete SAS string 
        const string authUser = "<replace>";
        const string authPassword = "<replace>";

        public static void Main()
        {
            Send();
        }

        static void Send()
        {
            const int nMsgs = 5;

            Client client = new Client(iotHubName + ".azure-devices.net", 5671, true, authUser + "@sas.root." + iotHubName, authPassword);

            int count = 0;
            ManualResetEvent done = new ManualResetEvent(false);
            Receiver receiver = client.GetReceiver("devices/"+ device + "/messages/deviceBound");
            receiver.Start(20, (r, m) =>
            {
                r.Accept(m);
                if (++count >= nMsgs) done.Set();
            });

            Thread.Sleep(5000);


            Sender sender = client.GetSender("devices/" + device + "/messages/events");
            for (int i = 0; i < nMsgs; i++)
            {
                sender.Send(new Message() { Body = Guid.NewGuid().ToString() });
                Thread.Sleep(1000);
            }

            done.WaitOne(30000, false);

            sender.Close();
            receiver.Close();
            client.Close();
        }
    }
}
