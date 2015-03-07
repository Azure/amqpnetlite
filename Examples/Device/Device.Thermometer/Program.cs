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
using System.Threading;
using Amqp;
using Microsoft.SPOT;
using Microsoft.SPOT.Input;
using Microsoft.SPOT.Presentation;
using Microsoft.SPOT.Presentation.Controls;
using AmqpTrace = Amqp.Trace;

namespace Device.Thermometer
{
    /// The thermomeer runs on NETMF. It displays a number for the current
    /// temprature. Click up and down buttons to increase/decrease the number.
    /// Ensure the broker is running and change the host name below.
    /// If using "amqps", ensure SSL is working on the device.
    /// The app works with the test broker. Start it as follows:
    ///   TestAmqpBroker.exe amqp://localhost:5672 /creds:guest:guest
    /// Add "/trace:frame" to output frames for debugging if requried.
    public class Program : Application
    {
        string address = "amqp://guest:guest@localhost:5672";
        Text text;
        int temperature;
        AutoResetEvent changed;

        public static void Main()
        {
            AmqpTrace.TraceLevel = TraceLevel.Frame | TraceLevel.Verbose;
            AmqpTrace.TraceListener = Program.WriteTrace;
            Connection.DisableServerCertValidation = true;

            Program application = new Program();
            application.temperature = 68;
            application.changed = new AutoResetEvent(true);

            new Thread(application.Listen).Start();

            GPIOButtonInputProvider inputProvider = new GPIOButtonInputProvider(null);
            application.Run(application.CreateWindow());
        }

        private void Listen()
        {
            // real applicaiton needs to implement error handling and recovery
            Connection connection = new Connection(new Address(this.address));
            Session session = new Session(connection);
            SenderLink sender = new SenderLink(session, "send-link", "data");
            ReceiverLink receiver = new ReceiverLink(session, "receive-link", "control");
            receiver.Start(100, this.OnMessage);

            while (true)
            {
                this.changed.WaitOne();

                Message message = new Message();
                message.ApplicationProperties = new Amqp.Framing.ApplicationProperties();
                message.ApplicationProperties["temperature"] = this.temperature;
                sender.Send(message, null, null);
                AmqpTrace.WriteLine(TraceLevel.Information, "sent data to monitor");
            }
        }

        private void OnMessage(ReceiverLink receiver, Message message)
        {
            AmqpTrace.WriteLine(TraceLevel.Information, "received command from controller");
            int button = (int)message.ApplicationProperties["button"];
            this.OnAction(button);
        }

        private void OnAction(int button)
        {
            WriteTrace("receive action: {0}", button);
            if (button == 38)
            {
                this.temperature++;
            }
            else if (button == 40)
            {
                this.temperature--;
            }

            this.changed.Set();

            this.text.Dispatcher.Invoke(
                TimeSpan.MaxValue,
                o =>
                {
                    Program application = (Program)o;
                    ((Text)application.text).TextContent = application.temperature.ToString();
                    return null;
                },
                this);
        }

        static void WriteTrace(string format, params object[] args)
        {
            Debug.Print(Fx.Format(format, args));
        }

        private Window CreateWindow()
        {
            // Create a window object and set its size to the
            // size of the display.
            Window window = new Window();
            window.Height = SystemMetrics.ScreenHeight;
            window.Width = SystemMetrics.ScreenWidth;

            // Create a single text control.
            Text text = new Text();
            text.TextContent = this.temperature.ToString();
            text.Font = Resources.GetFont(Resources.FontResources.small);
            text.HorizontalAlignment = Microsoft.SPOT.Presentation.HorizontalAlignment.Center;
            text.VerticalAlignment = Microsoft.SPOT.Presentation.VerticalAlignment.Center;

            // Add the text control to the window.
            window.Child = text;

            // Connect the button handler to all of the buttons.
            window.AddHandler(Buttons.ButtonUpEvent, new RoutedEventHandler(OnButtonUp), false);

            // Set the window visibility to visible.
            window.Visibility = Visibility.Visible;

            // Attach the button focus to the window.
            Buttons.Focus(window);

            this.text = text;

            return window;
        }

        private void OnButtonUp(object sender, RoutedEventArgs evt)
        {
            ButtonEventArgs e = (ButtonEventArgs)evt;
            int button = (int)e.Button;
            this.OnAction(button);
        }
    }
}
