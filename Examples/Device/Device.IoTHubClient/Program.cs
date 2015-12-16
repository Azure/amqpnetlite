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
using AmqpTrace = Amqp.Trace;

namespace Device.IoTHubClient
{
    public class Program
    {
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
        }

        private void Listen()
        {
            // real application needs to implement error handling and recovery
            Connection connection = new Connection(new Address("eclo-one.azure-devices.net", 5671, "iothubowner@sas.root.eclo-one", "SharedAccessSignature sr=eclo-one.azure-devices.net&sig=H4Rm2%2bjdBr84lq5KOddD9YpOSC8s7ZSe9SygErVuPe8%3d&se=1449823061&skn=iothubowner"));
            Session session = new Session(connection);
            SenderLink sender = new SenderLink(session, "send-link", "devices/Lince1/messages/events");
            ReceiverLink receiver = new ReceiverLink(session, "receive-link", "devices/Lince1/messages/deviceBound");
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
            AmqpTrace.WriteLine(TraceLevel.Information, "received command");
            this.OnAction(message);
        }

        private void OnAction(Message message)
        {
            WriteTrace("receive action: {0}", message.DeliveryTag);
            //if (button == 38)
            //{
            //    this.temperature++;
            //}
            //else if (button == 40)
            //{
            //    this.temperature--;
            //}

            this.changed.Set();

            //this.text.Dispatcher.Invoke(
            //    TimeSpan.MaxValue,
            //    o =>
            //    {
            //        Program application = (Program)o;
            //        ((Text)application.text).TextContent = application.temperature.ToString();
            //        return null;
            //    },
            //    this);
        }

        static void WriteTrace(string format, params object[] args)
        {
            Debug.Print(Fx.Format(format, args));
        }

    }
}
