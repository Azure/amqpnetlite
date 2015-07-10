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

namespace ServiceBus.MessageSession
{
    using System;
    using System.Text;
    using Amqp;
    using Amqp.Framing;
    using Amqp.Types;

    class Program
    {
        // update the following with valid Service Bus namespace and SAS key info
        // the queue should be created with RequiresSession=true
        const string sbNamespace = "contoso.servicebus.windows.net";
        const string keyName = "key1";
        const string keyValue = "5znwNTZDYC39dqhFOTDtnaikd1hiuRa4XaAj3Y9kJhQ=";
        const string entity = "sessionQueue";

        static void Main(string[] args)
        {
            Trace.TraceLevel = TraceLevel.Information;
            Trace.TraceListener = (f, a) => Console.WriteLine(DateTime.Now.ToString("[hh:ss.fff]") + " " + string.Format(f, a));

            string[] priorites = new string[] { "low", "medium", "high" };
            try
            {
                // send messages with differnt session IDs
                SendMessages(entity, 100, priorites);

                // receive messages from a specific session
                ReceiveMessages(entity, 10, priorites[2]);

                // receive messages from any available session
                ReceiveMessages(entity, 10, null);
            }
            catch (Exception e)
            {
                Trace.WriteLine(TraceLevel.Error, e.ToString());
            }
        }

        static void SendMessages(string node, int count, string[] sessionIds)
        {
            Trace.WriteLine(TraceLevel.Information, "Establishing a connection...");
            Address address = new Address(sbNamespace, 5671, keyName, keyValue);
            Connection connection = new Connection(address);

            Trace.WriteLine(TraceLevel.Information, "Creating a session...");
            Session session = new Session(connection);

            Trace.WriteLine(TraceLevel.Information, "Creating a sender link...");
            SenderLink sender = new SenderLink(session, "sessionful-sender-link", node);

            Trace.WriteLine(TraceLevel.Information, "Sending {0} messages...", count);
            for (int i = 0; i < count; i++)
            {
                Message message = new Message();
                message.Properties = new Properties() { GroupId = sessionIds[i % sessionIds.Length] };
                message.BodySection = new Data() { Binary = Encoding.UTF8.GetBytes("msg" + i) };
                sender.Send(message);
            }

            Trace.WriteLine(TraceLevel.Information, "Finished sending. Shutting down...");
            Trace.WriteLine(TraceLevel.Information, "");

            sender.Close();
            session.Close();
            connection.Close();
        }

        static void ReceiveMessages(string node, int count, string sessionId)
        {
            Trace.WriteLine(TraceLevel.Information, "Establishing a connection...");
            Address address = new Address(sbNamespace, 5671, keyName, keyValue);
            Connection connection = new Connection(address);

            Trace.WriteLine(TraceLevel.Information, "Creating a session...");
            Session session = new Session(connection);

            Trace.WriteLine(TraceLevel.Information, "Accepting a message session '{0}'...", sessionId ?? "<any>");
            Map filters = new Map();
            filters.Add(new Symbol("com.microsoft:session-filter"), sessionId);

            ReceiverLink receiver = new ReceiverLink(
                session,
                "sessionful-receiver-link",
                new Source() { Address = node, FilterSet = filters },
                null);

            for (int i = 0; i < count; i++)
            {
                Message message = receiver.Receive(30000);
                if (message == null)
                {
                    break;
                }

                if (i == 0)
                {
                    Trace.WriteLine(TraceLevel.Information, "Received message from session '{0}'", message.Properties.GroupId);
                }

                receiver.Accept(message);
            }

            Trace.WriteLine(TraceLevel.Information, "Finished receiving. Shutting down...");
            Trace.WriteLine(TraceLevel.Information, "");

            receiver.Close();
            session.Close();
            connection.Close();
        }
    }
}
