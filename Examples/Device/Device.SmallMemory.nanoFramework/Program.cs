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
using Amqp.Types;
using nanoFramework.Networking;
using System;
using System.Diagnostics;
using System.Threading;

namespace Device.SmallMemory
{
    public class Program
    {
        // this example program connects to an Azure IoT hub sends a couple of messages and waits for messages from
        // it was tested with an STM32F769I-DISCO board.

        // replace with IoT Hub name
        const string iotHubName = "<replace>";

        // replace with device name 
        const string device = "<replace>";

        // user/pass to be authenticated with Azure IoT hub
        // if using a shared access signature like SharedAccessSignature sr=myhub.azure-devices.net&sig=H4Rm2%2bjdBr84lq5KOddD9YpOSC8s7ZSe9SygErVuPe8%3d&se=1444444444&skn=userNameHere
        // user will be userNameHere and password the complete SAS string 
        const string iotUser = "<replace>";
        const string sasToken = "<replace>";

        public static void Main()
        {
            // setup and connect network
            NetworkHelpers.SetupAndConnectNetwork(true);

            // wait for network and valid system date time
            NetworkHelpers.IpAddressAvailable.WaitOne();
            NetworkHelpers.DateTimeAvailable.WaitOne();

            Send();
        }

        static void Send()
        {
            const int nMsgs = 50;

            // Map IotHub settings to AMQP protocol settings
            string hostName = iotHubName + ".azure-devices.net";
            int port = 5671;
            string userName = iotUser + "@sas.root." + iotHubName;
            string password = sasToken;
            string senderAddress = "devices/" + device + "/messages/events";
            string receiverAddress = "devices/" + device + "/messages/deviceBound";

            Client client = new Client();
            client.OnError += client_OnError;
            client.Connect(hostName, port, true, userName, password);

            int count = 0;
            ManualResetEvent done = new ManualResetEvent(false);
            Receiver receiver = client.CreateReceiver(receiverAddress);
            receiver.Start(20, (r, m) =>
            {
                r.Accept(m);
                if (++count >= nMsgs) done.Set();
            });

            Thread.Sleep(1000);

            Sender[] senders = new Sender[5];
            for (int i = 0; i < senders.Length; i++)
            {
                senders[i] = client.CreateSender(senderAddress);
            }

            for (int i = 0; i < nMsgs; i++)
            {
                senders[i % senders.Length].Send(new Message() { Body = Guid.NewGuid().ToString() });
            }

            done.WaitOne(120000, false);

            client.Close();
        }

        static void client_OnError(Client client, Link link, Symbol error)
        {
            Debug.WriteLine((link != null ? "Link" : "Client") + " was closed due to error " + (error ?? "[unknown]"));
        }
    }
}
