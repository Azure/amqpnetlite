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
using Amqp.Framing;
using Microsoft.Phone.Controls;
using System.Windows.Threading;

namespace Device.Controller
{
    /// <summary>
    /// The controller runs on Windows Phone 8. The app name is iHome.
    /// Ensure the broker is running and change the IP address below.
    /// If using "amqps", ensure the broker certificate can be validated
    /// on the Phone (e.g. self-generated certificate needs to be imported
    /// to the phone).
    /// The app works with the test broker. Start it as follows:
    ///   TestAmqpBroker.exe amqp://localhost:5672 /creds:guest:guest
    /// Add "/trace:frame" to output frames for debugging if requried.
    /// An "X" displayed in the middle indicates a connection failure. 
    /// </summary>
    public partial class MainPage : PhoneApplicationPage
    {
        Controller controller;

        // Constructor
        public MainPage()
        {
            InitializeComponent();
            try
            {
                this.controller = new Controller(this);
            }
            catch
            {
                this.btnData.Content = "X";
                this.controller = null;
            }
        }

        sealed class Controller
        {
            MainPage parent;
            Connection connection;
            Session session;
            SenderLink sender;
            ReceiverLink receiver;

            public Controller(MainPage parent)
            {
                this.parent = parent;
                Address address = new Address("amqp://guest:guest@10.1.10.76:5672");
                this.connection = new Connection(address);
                this.session = new Session(connection);
                this.sender = new SenderLink(session, "send-link", "control");
                this.receiver = new ReceiverLink(session, "recv-link", "data");
                this.receiver.Start(50, this.OnMessage);
            }

            public void Control(int button)
            {
                Message message = new Message();
                message.ApplicationProperties = new ApplicationProperties();
                message.ApplicationProperties["button"] = button;
                this.sender.Send(message, null, null);
            }

            void OnMessage(ReceiverLink receiver, Message message)
            {
                int temperature = (int)message.ApplicationProperties["temperature"];
                this.parent.Dispatcher.BeginInvoke(() => this.parent.btnData.Content = temperature.ToString());
            }
        }

        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (this.controller != null)
            {
                this.controller.Control(38);
            }
        }

        private void Button_Click_1(object sender, System.Windows.RoutedEventArgs e)
        {
            if (this.controller != null)
            {
                this.controller.Control(40);
            }
        }
    }
}