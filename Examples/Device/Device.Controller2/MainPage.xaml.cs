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
using System.ComponentModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Device.Controller
{
    /// <summary>
    /// The controller runs on Windows Phone 8.1 The app name is iHome.
    /// Ensure the broker is running and change the IP address below as needed.
    /// If using "amqps", ensure the broker certificate can be validated
    /// on the Phone (e.g. self-generated certificate needs to be imported
    /// to the phone).
    /// The app works with the test broker. Start it as follows:
    ///   TestAmqpBroker.exe amqp://localhost:5672 /creds:guest:guest
    /// Add "/trace:frame" to output frames for debugging if requried.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        Controller controller;

        public MainPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Required;
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // TODO: Prepare page for display here.

            // TODO: If your application contains multiple pages, ensure that you are
            // handling the hardware Back button by registering for the
            // Windows.Phone.UI.Input.HardwareButtons.BackPressed event.
            // If you are using the NavigationHelper provided by some templates,
            // this event is handled for you.
        }

        private void Button_Up_Click(object sender, RoutedEventArgs e)
        {
            if (this.controller != null)
            {
                this.controller.Control(38);
            }
        }

        private void Button_Down_Click(object sender, RoutedEventArgs e)
        {
            if (this.controller != null)
            {
                this.controller.Control(40);
            }
        }

        sealed class Controller : INotifyPropertyChanged
        {
            Connection connection;
            Session session;
            SenderLink sender;
            ReceiverLink receiver;
            string message;

            public event PropertyChangedEventHandler PropertyChanged;

            public Controller()
            {
                this.Message = "?";
            }

            public void Control(int button)
            {
                try
                {
                    Message message = new Message();
                    message.ApplicationProperties = new ApplicationProperties();
                    message.ApplicationProperties["button"] = button;
                    this.sender.Send(message, null, null);
                }
                catch
                {
                    this.Message = "err:1";
                }
            }

            public string Message
            {
                get
                {
                    return this.message;
                }

                private set
                {
                    this.message = value;
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("Message"));
                    }
                }
            }

            public async void Start(string address)
            {
                ConnectionFactory factory = new ConnectionFactory();

                try
                {
                    this.connection = await factory.CreateAsync(new Address(address));
                    this.session = new Session(connection);
                    this.sender = new SenderLink(session, "send-link", "control");
                    this.receiver = new ReceiverLink(session, "recv-link", "data");
                    this.receiver.Start(50, this.OnMessage);
                }
                catch
                {
                    this.Message = "err:2";
                }
            }

            void OnMessage(IReceiverLink receiver, Message message)
            {
                int temperature = (int)message.ApplicationProperties["temperature"];
                this.Message = temperature.ToString();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.controller = new Controller();
            this.btnData.DataContext = this.controller;
            this.controller.Start(this.txtAddress.Text);
        }
    }
}
