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

namespace PeerToPeer.Certificate
{
    using System;
    using System.Net.Security;
    using System.Security.Cryptography.X509Certificates;
    using Amqp;
    using Amqp.Listener;
    using Amqp.Sasl;

    class Program
    {
        static void Main(string[] args)
        {
            //Trace.TraceLevel = TraceLevel.Frame;
            //Trace.TraceListener = (l, f, a) => Console.WriteLine(DateTime.Now.ToString("[hh:mm:ss.fff]") + " " + string.Format(f, a));

            Address address = new Address("amqps://localhost:5671");

            // start a host with custom SSL and SASL settings
            Console.WriteLine("Starting server...");
            ContainerHost host = new ContainerHost(address);            
            var listener = host.Listeners[0];
            listener.SSL.Certificate = Test.Common.Extensions.GetCertificate("localhost");
            listener.SSL.ClientCertificateRequired = true;
            listener.SSL.RemoteCertificateValidationCallback = ValidateServerCertificate;
            listener.SASL.EnableExternalMechanism = true;
            host.Open();
            Console.WriteLine("Container host is listening on {0}:{1}", address.Host, address.Port);

            string messageProcessor = "message_processor";
            host.RegisterMessageProcessor(messageProcessor, new MessageProcessor());
            Console.WriteLine("Message processor is registered on {0}", messageProcessor);

            Console.WriteLine("Starting client...");
            ConnectionFactory factory = new ConnectionFactory();
            factory.SSL.ClientCertificates.Add(Test.Common.Extensions.GetCertificate("localhost"));
            factory.SSL.RemoteCertificateValidationCallback = ValidateServerCertificate;
            factory.SASL.Profile = SaslProfile.External;
            Console.WriteLine("Sending message...");
            Connection connection = factory.CreateAsync(address).Result;
            Session session = new Session(connection);
            SenderLink sender = new SenderLink(session, "certificate-example-sender", "message_processor");
            sender.Send(new Message("hello world"));
            sender.Close();
            session.Close();
            connection.Close();
            Console.WriteLine("client done");

            host.Close();
            Console.WriteLine("server stopped");
        }

        static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            Console.WriteLine("Received remote certificate. Subject: {0}, Policy errors: {1}", certificate.Subject, sslPolicyErrors);
            return true;
        }

        class MessageProcessor : IMessageProcessor
        {
            int IMessageProcessor.Credit
            {
                get { return 300; }
            }

            void IMessageProcessor.Process(MessageContext messageContext)
            {
                Console.WriteLine("Received a message.");
                messageContext.Complete();
            }
        }
    }
}
