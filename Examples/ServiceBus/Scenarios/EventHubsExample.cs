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
    using System;
    using System.Text;
    using Amqp;
    using Amqp.Framing;
    using Amqp.Types;

    class EventHubsExample : Example
    {
        public override void Run()
        {
            this.SendToEventHub();

            this.SendToEventHubWithPartitionKey();

            this.SendToEventHubPartition("1");

            this.SendToEventHubPublisher("device0001");

            string[] partitions = this.GetEventHubPartitions();

            var messages = this.ReceiveMessagesWithoutFilter(partitions[0]);

            var middle = messages[messages.Length / 2];
            var offset = (string)middle.MessageAnnotations[new Symbol("x-opt-offset")];
            var enqueueTime = (DateTime)middle.MessageAnnotations[new Symbol("x-opt-enqueued-time")];

            this.ReceiveMessagesWithExclusiveOffsetFilter(partitions[0], offset);

            this.ReceiveMessagesWithInclusiveOffsetFilter(partitions[0], offset);
            
            this.ReceiveMessagesWithTimestampFilter(partitions[0], Amqp.Types.Encoder.DateTimeToTimestamp(enqueueTime));
        }

        void SendToEventHub()
        {
            this.SendMessages("send to event hub without partition key", this.Entity, 10, false);
        }

        void SendToEventHubWithPartitionKey()
        {
            this.SendMessages("send to event hub with partition key", this.Entity, 10, true);
        }

        void SendToEventHubPartition(string partitionId)
        {
            this.SendMessages("send to event hub partition", this.Entity + "/Partitions/" + partitionId, 10, false);
        }

        void SendToEventHubPublisher(string publisher)
        {
            this.SendMessages("send to event hub publisher", this.Entity + "/Publishers/" + publisher, 10, false);
        }

        string[] GetEventHubPartitions()
        {
            Trace.WriteLine(TraceLevel.Information, "Retrieving partitions...");
            Trace.WriteLine(TraceLevel.Information, "Establishing a connection...");
            Connection connection = new Connection(this.GetAddress());

            Trace.WriteLine(TraceLevel.Information, "Creating a session...");
            Session session = new Session(connection);

            // create a pair of links for request/response
            Trace.WriteLine(TraceLevel.Information, "Creating a request and a response link...");
            string clientNode = "client-temp-node";
            SenderLink sender = new SenderLink(session, "mgmt-sender", "$management");
            ReceiverLink receiver = new ReceiverLink(
                session,
                "mgmt-receiver",
                new Attach()
                {
                    Source = new Source() { Address = "$management" },
                    Target = new Target() { Address = clientNode }
                },
                null);

            Message request = new Message();
            request.Properties = new Properties() { MessageId = "request1", ReplyTo = clientNode };
            request.ApplicationProperties = new ApplicationProperties();
            request.ApplicationProperties["operation"] = "READ";
            request.ApplicationProperties["name"] = this.Entity;
            request.ApplicationProperties["type"] = "com.microsoft:eventhub";
            sender.Send(request, null, null);

            Message response = receiver.Receive();
            if (response == null)
            {
                throw new Exception("No response was received.");
            }

            receiver.Accept(response);
            receiver.Close();
            sender.Close();
            connection.Close();

            Trace.WriteLine(TraceLevel.Information, "Partition info: {0}", response.Body.ToString());
            string[] partitions = (string[])((Map)response.Body)["partition_ids"];

            return partitions;
        }

        Message[] ReceiveMessagesWithoutFilter(string partitionId)
        {
            return this.ReceiveMessages("receive without filter", 2, null, partitionId);
        }

        Message[] ReceiveMessagesWithExclusiveOffsetFilter(string partitionId, string offset)
        {
            return this.ReceiveMessages("receive from offset checkpoint exclusively", 2, "amqp.annotation.x-opt-offset > '" + offset + "'", partitionId);
        }

        Message[] ReceiveMessagesWithInclusiveOffsetFilter(string partitionId, string offset)
        {
            return this.ReceiveMessages("receive from offset checkpoint inclusively", 2, "amqp.annotation.x-opt-offset >= '" + offset + "'", partitionId);
        }

        Message[] ReceiveMessagesWithTimestampFilter(string partitionId, long timestamp)
        {
            return this.ReceiveMessages("receive from timestamp checkpoint", 2, "amqp.annotation.x-opt-enqueuedtimeutc > " + timestamp, partitionId);
        }

        void SendMessages(string scenario, string node, int count, bool partitionKey)
        {
            Trace.WriteLine(TraceLevel.Information, "Running scenario '{0}'...", scenario);
            Trace.WriteLine(TraceLevel.Information, "  node: '{0}', message count: {1}, set partition key: {2}", node, count, partitionKey);

            Trace.WriteLine(TraceLevel.Information, "Establishing a connection...");
            Connection connection = new Connection(this.GetAddress());

            Trace.WriteLine(TraceLevel.Information, "Creating a session...");
            Session session = new Session(connection);

            Trace.WriteLine(TraceLevel.Information, "Creating a sender link...");
            SenderLink sender = new SenderLink(session, "sender-" + scenario, node);

            Trace.WriteLine(TraceLevel.Information, "Sending {0} messages...", count);
            for (int i = 0; i < count; i++)
            {
                Message message = new Message()
                {
                    BodySection = new Data() { Binary = Encoding.UTF8.GetBytes("msg" + i) }
                };

                message.Properties = new Properties() { GroupId = scenario };
                if (partitionKey)
                {
                    message.MessageAnnotations = new MessageAnnotations();
                    message.MessageAnnotations[new Symbol("x-opt-partition-key")] = "pk:" + i;
                }

                sender.Send(message);
            }

            Trace.WriteLine(TraceLevel.Information, "Finished sending. Shutting down...");
            Trace.WriteLine(TraceLevel.Information, "");

            sender.Close();
            session.Close();
            connection.Close();
        }

        Message[] ReceiveMessages(string scenario, int count, string filter, string partition)
        {
            Trace.WriteLine(TraceLevel.Information, "Running scenario '{0}', filter '{1}'...", scenario, filter);

            Trace.WriteLine(TraceLevel.Information, "Establishing a connection...");
            Connection connection = new Connection(this.GetAddress());

            Trace.WriteLine(TraceLevel.Information, "Creating a session...");
            Session session = new Session(connection);

            Trace.WriteLine(TraceLevel.Information, "Creating a receiver link on partition {0}...", partition);
            string partitionAddress = this.Entity + "/ConsumerGroups/$default/Partitions/" + partition; ;
            Map filters = new Map();
            if (filter != null)
            {
                filters.Add(new Symbol("apache.org:selector-filter:string"),
                    new DescribedValue(new Symbol("apache.org:selector-filter:string"), filter));
            }

            ReceiverLink receiver = new ReceiverLink(
                session,
                "receiver-" + partition,
                new Source() { Address = partitionAddress, FilterSet = filters },
                null);

            Message[] messages = new Message[count];
            for (int i = 0; i < count; i++)
            {
                Message message = receiver.Receive(30000);
                if (message == null)
                {
                    break;
                }

                receiver.Accept(message);
                Trace.WriteLine(
                    TraceLevel.Information,
                    "Received a message. sn: {0}, offset: '{1}', enqueue time: {2}",
                    (long)message.MessageAnnotations[new Symbol("x-opt-sequence-number")],
                    (string)message.MessageAnnotations[new Symbol("x-opt-offset")],
                    (DateTime)message.MessageAnnotations[new Symbol("x-opt-enqueued-time")]);

                messages[i] = message;
            }

            receiver.Close();
            session.Close();
            connection.Close();

            return messages;
        }
    }
}
