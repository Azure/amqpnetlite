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

namespace ServiceBus.EventHub
{
    using System;
    using System.Text;
    using Amqp;
    using Amqp.Framing;
    using Amqp.Types;

    class Program
    {
        // update the following with valid Service Bus namespace and SAS key info
        const string sbNamespace = "contoso.servicebus.windows.net";
        const string keyName = "key1";
        const string keyValue = "5znwNTZDYC39dqhFOTDtnaikd1hiuRa4XaAj3Y9kJhQ=";
        const string entity = "eh1";

        static void Main(string[] args)
        {
            Trace.TraceLevel = TraceLevel.Information;
            Trace.TraceListener = (f, a) => Console.WriteLine(DateTime.Now.ToString("[hh:ss.fff]") + " " + string.Format(f, a));

            try
            {
                SendMessages("send to event hub without partition key", entity, 10, false);

                SendMessages("send to event hub with partition key", entity, 10, true);

                SendMessages("send to event hub partition", entity + "/Partitions/1", 10, false);

                SendMessages("send to event hub publisher", entity + "/Publishers/device0001", 10, false);

                string[] partitions = GetPartitions();

                var checkpoint = ReceiveMessages("receive without filter", 2, null, partitions[0]);

                ReceiveMessages("receive from offset checkpoint", int.MaxValue, "amqp.annotation.x-opt-offset > '" + checkpoint.Item1 + "'", partitions[0]);

                long ticksOffset = (long)checkpoint.Item2.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
                ReceiveMessages("receive from time checkpoint", int.MaxValue, "amqp.annotation.x-opt-enqueuedtimeutc > " + ticksOffset, partitions[0]);
            }
            catch (Exception e)
            {
                Trace.WriteLine(TraceLevel.Error, e.ToString());
            }
        }

        static void SendMessages(string scenario, string node, int count, bool partitionKey)
        {
            Trace.WriteLine(TraceLevel.Information, "Running scenario '{0}'...", scenario);
            Trace.WriteLine(TraceLevel.Information, "  node: '{0}', message count: {1}, set partition key: {2}", node, count, partitionKey);

            Trace.WriteLine(TraceLevel.Information, "Establishing a connection...");
            Address address = new Address(sbNamespace, 5671, keyName, keyValue);
            Connection connection = new Connection(address);

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

        static string[] GetPartitions()
        {
            Trace.WriteLine(TraceLevel.Information, "Retrieving partitions...");
            Trace.WriteLine(TraceLevel.Information, "Establishing a connection...");
            Address address = new Address(sbNamespace, 5671, keyName, keyValue);
            Connection connection = new Connection(address);

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
            request.ApplicationProperties["name"] = entity;
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

            Trace.WriteLine(TraceLevel.Information, "Partition info {0}", response.Body.ToString());
            string[] partitions = (string[])((Map)response.Body)["partition_ids"];
            Trace.WriteLine(TraceLevel.Information, "Partitions {0}", string.Join(",", partitions));
            Trace.WriteLine(TraceLevel.Information, "");

            return partitions;
        }

        static Tuple<string, DateTime> ReceiveMessages(string scenario, int count, string filter, string partition)
        {
            Trace.WriteLine(TraceLevel.Information, "Running scenario '{0}', filter '{1}'...", scenario, filter);

            Trace.WriteLine(TraceLevel.Information, "Establishing a connection...");
            Address address = new Address(sbNamespace, 5671, keyName, keyValue);
            Connection connection = new Connection(address);

            Trace.WriteLine(TraceLevel.Information, "Creating a session...");
            Session session = new Session(connection);

            Trace.WriteLine(TraceLevel.Information, "Creating a receiver link on partition {0}...", partition);
            string partitionAddress = entity + "/ConsumerGroups/$default/Partitions/" + partition; ;
            Map filters = new Map();
            if (filter != null)
            {
                filters.Add(new Symbol("apache.org:selector-filter:string"),
                    new DescribedValue(new Symbol("apache.org:selector-filter:string"), filter));
            }

            string lastOffset = "-1";
            long lastSeqNumber = -1;
            DateTime lastEnqueueTime = DateTime.MinValue;

            ReceiverLink receiver = new ReceiverLink(
                session,
                "receiver-" + partition,
                new Source() { Address = partitionAddress, FilterSet = filters },
                null);

            int i;
            for (i = 0; i < count; i++)
            {
                Message message = receiver.Receive(30000);
                if (message == null)
                {
                    break;
                }

                receiver.Accept(message);
                lastOffset = (string)message.MessageAnnotations[new Symbol("x-opt-offset")];
                lastSeqNumber = (long)message.MessageAnnotations[new Symbol("x-opt-sequence-number")];
                lastEnqueueTime = (DateTime)message.MessageAnnotations[new Symbol("x-opt-enqueued-time")];
            }

            receiver.Close();
            Trace.WriteLine(TraceLevel.Information, "Received {0} messages on partition {1}", i, partition);
            Trace.WriteLine(TraceLevel.Information, "  offset: '{0}', sn: {1}, enqueue time: {2}", lastOffset, lastSeqNumber, lastEnqueueTime.ToString("hh:ss.fff"));
            Trace.WriteLine(TraceLevel.Information, "");

            session.Close();
            connection.Close();

            return Tuple.Create(lastOffset, lastEnqueueTime);
        }
    }
}
