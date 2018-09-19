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

namespace ServiceBus.Scenarios
{
    using System.Text;
    using Amqp;
    using Amqp.Framing;
    using Amqp.Types;

    /// <summary>
    /// This example assumes a sessionful queue is precreated.
    /// If a topic is used, update the address in Source of the ReceiverLink
    /// to be "[topic-name]/Subscriptions/[subscription-name]".
    /// Refer to ServiceBus.Topic example for more details.
    /// </summary>
    class MessageSessionExample : Example
    {
        public override void Run()
        {
            string[] priorites = new string[] { "low", "medium", "high" };

            // send messages with different session IDs
            this.SendMessages(100, priorites);

            // receive messages from a specific session
            this.ReceiveMessages(10, priorites[2]);

            // receive messages from any available session
            this.ReceiveMessages(10, null);
        }

        void SendMessages(int count, string[] sessionIds)
        {
            Trace.WriteLine(TraceLevel.Information, "Establishing a connection...");
            Connection connection = new Connection(this.GetAddress());

            Trace.WriteLine(TraceLevel.Information, "Creating a session...");
            Session session = new Session(connection);

            Trace.WriteLine(TraceLevel.Information, "Creating a sender link...");
            SenderLink sender = new SenderLink(session, "sessionful-sender-link", this.Entity);

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

        void ReceiveMessages(int count, string sessionId)
        {
            Trace.WriteLine(TraceLevel.Information, "Establishing a connection...");
            Connection connection = new Connection(this.GetAddress());

            Trace.WriteLine(TraceLevel.Information, "Creating a session...");
            Session session = new Session(connection);

            Trace.WriteLine(TraceLevel.Information, "Accepting a message session '{0}'...", sessionId ?? "<any>");
            Map filters = new Map();
            filters.Add(new Symbol("com.microsoft:session-filter"), sessionId);

            ReceiverLink receiver = new ReceiverLink(
                session,
                "sessionful-receiver-link",
                new Source() { Address = this.Entity, FilterSet = filters },
                null);

            for (int i = 0; i < count; i++)
            {
                Message message = receiver.Receive();
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
