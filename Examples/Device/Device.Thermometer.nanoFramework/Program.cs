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
using System;
using System.Diagnostics;
using System.Threading;
using Windows.Devices.Gpio;
using AmqpTrace = Amqp.Trace;

namespace Device.Thermometer
{
    /// The thermometer runs on a nanoFramework device.
    /// It was tested with an STM32F769I-DISCO board.
    /// Click user button to send a random temperature value to the broker.
    /// Ensure the broker is running and change the host name below.
    /// If using "amqps", ensure SSL is working on the device.
    /// The app works with the test broker (see test\TestAmqpBroker). Start it as follows:
    ///   TestAmqpBroker.exe amqp://192.168.1.9:5672 /creds:guest:guest
    /// Add "/trace:frame" to output frames for debugging if required.
    public class Program
    {
        ////////////////////////////////////////////////////////////////////////
        // update the IP bellow to the one of the TestAmqpBroker
        private static string address = "amqp://guest:guest@192.168.1.9:5672";
        ////////////////////////////////////////////////////////////////////////

        private static int temperature;
        private static AutoResetEvent changed;

        private static GpioPin _userButton;

        public static void Main()
        {
            // setup and connect network
            NetworkHelpers.SetupAndConnectNetwork(true);

            // setup user button
            // F769I-DISCO -> USER_BUTTON is @ PA0 -> (0 * 16) + 0 = 0
            _userButton = GpioController.GetDefault().OpenPin(0);
            _userButton.SetDriveMode(GpioPinDriveMode.Input);
            _userButton.ValueChanged += UserButton_ValueChanged;

            AmqpTrace.TraceLevel = TraceLevel.Frame | TraceLevel.Verbose;
            AmqpTrace.TraceListener = WriteTrace;
            Connection.DisableServerCertValidation = true;

            temperature = 68;
            changed = new AutoResetEvent(true);

            // wait for network and valid system date time
            NetworkHelpers.IpAddressAvailable.WaitOne();
            NetworkHelpers.DateTimeAvailable.WaitOne();

            new Thread(Listen).Start();

            Thread.Sleep(Timeout.Infinite);
        }

        private static void Listen()
        {
            // real application needs to implement error handling and recovery
            Connection connection = new Connection(new Address(address));
            Session session = new Session(connection);
            SenderLink sender = new SenderLink(session, "send-link", "data");
            ReceiverLink receiver = new ReceiverLink(session, "receive-link", "control");
            receiver.Start(100, OnMessage);

            while (true)
            {
                changed.WaitOne();

                Message message = new Message();
                message.ApplicationProperties = new Amqp.Framing.ApplicationProperties();
                message.ApplicationProperties["temperature"] = temperature;
                sender.Send(message, null, null);
                AmqpTrace.WriteLine(TraceLevel.Information, "sent data to monitor");
            }
        }
        private static void OnMessage(IReceiverLink receiver, Message message)
        {
            AmqpTrace.WriteLine(TraceLevel.Information, "received command from controller");
            int button = (int)message.ApplicationProperties["button"];
            OnAction(button);
        }

        private static void OnAction(int button)
        {
            WriteTrace(TraceLevel.Information, "receive action: {0}", button);
            if (button == 38)
            {
                temperature++;
            }
            else if (button == 40)
            {
                temperature--;
            }

            changed.Set();
        }

        private static void UserButton_ValueChanged(object sender, GpioPinValueChangedEventArgs e)
        {
            WriteTrace(TraceLevel.Information, "User button pressed, generating random temperature value");

            // generate a random temperature value
            var random = new Random();
            temperature = random.Next(50);

            changed.Set();
        }

        static void WriteTrace(TraceLevel level, string format, params object[] args)
        {
            Debug.WriteLine(Fx.Format(format, args));
        }
    }
}
