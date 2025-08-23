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

namespace Listener.IContainer
{
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using global::Amqp;
    using global::Amqp.Framing;
    using global::Amqp.Handler;
    using global::Amqp.Listener;
    using global::Amqp.Transactions;
    using global::Amqp.Types;
    using Test.Common;

    public interface INode : IHandler
    {
        string Name { get; }

        bool AttachLink(ListenerConnection connection, ListenerSession session, ListenerLink link, Attach attach);
    }

    public sealed class TestAmqpBroker : IContainer, IHandler
    {
        public const uint BatchFormat = 0x80013700;
        public const string RemoteOpenName = "$host.open";
        readonly X509Certificate2 certificate;
        readonly Dictionary<string, TransportProvider> customTransports;
        readonly Dictionary<string, TestQueue> queues;
        readonly ConnectionListener[] listeners;
        readonly TxnManager txnManager;
        readonly List<INode> nodes;
        bool implicitQueue;
        int dynamidId;

        public TestAmqpBroker(IList<string> endpoints, string userInfo, string certValue, string[] queues)
        {
            this.customTransports = new Dictionary<string, TransportProvider>(StringComparer.OrdinalIgnoreCase);
            this.txnManager = new TxnManager();
            this.nodes = new List<INode>();
            this.queues = new Dictionary<string, TestQueue>();
            if (queues != null)
            {
                foreach (string q in queues)
                {
                    this.queues.Add(q, new TestQueue(this, q));
                }
            }
            else
            {
                this.implicitQueue = true;
            }

            this.certificate = certValue == null ? null : Test.Common.Extensions.GetCertificate(certValue);

            string containerId = "AMQPNetLite-TestBroker-" + Guid.NewGuid().ToString().Substring(0, 8);
            this.listeners = new ConnectionListener[endpoints.Count];
            for (int i = 0; i < endpoints.Count; i++)
            {
                this.listeners[i] = new ConnectionListener(endpoints[i], this);
                this.listeners[i].ConfigureTest();
                this.listeners[i].HandlerFactory = CreateHandler;
                this.listeners[i].AMQP.MaxSessionsPerConnection = 1000;
                this.listeners[i].AMQP.ContainerId = containerId;
                this.listeners[i].AMQP.IdleTimeout = 4 * 60 * 1000;
                this.listeners[i].AMQP.MaxFrameSize = 64 * 1024;
                this.listeners[i].SASL.EnableAnonymousMechanism = true;
                if (userInfo != null)
                {
                    string[] a = userInfo.Split(':');
                    this.listeners[i].SASL.EnablePlainMechanism(
                        Uri.UnescapeDataString(a[0]),
                        a.Length == 1 ? string.Empty : Uri.UnescapeDataString(a[1]));
                }
            }
        }

        public IDictionary<string, TransportProvider> CustomTransports
        {
            get { return this.customTransports; }
        }

        public static Message Decode(Message message)
        {
            return Message.Decode(((BrokerMessage)message).Buffer);
        }

        public void Start()
        {
            foreach (var listener in this.listeners)
            {
                listener.Open();
            }
        }

        public void Stop()
        {
            foreach (var listener in this.listeners)
            {
                try
                {
                    listener.Close();
                }
                catch (Exception exception)
                {
                    Trace.WriteLine(TraceLevel.Error, exception.ToString());
                }
            }
        }

        public void AddNode(INode node)
        {
            this.nodes.Add(node);
        }

        public void AddQueue(string queue)
        {
            lock (this.queues)
            {
                this.queues.Add(queue, new TestQueue(this, queue));
            }
        }

        public void RemoveQueue(string queue)
        {
            lock (this.queues)
            {
                this.queues.Remove(queue);
            }
        }

        bool IHandler.CanHandle(EventId id)
        {
            return true;
        }

        void IHandler.Handle(Event protocolEvent)
        {
            foreach (var node in this.nodes)
            {
                if (node.CanHandle(protocolEvent.Id))
                {
                    node.Handle(protocolEvent);
                }
            }

            switch (protocolEvent.Id)
            {
                case EventId.ConnectionRemoteOpen:
                    protocolEvent.Connection.SetProperty(RemoteOpenName, protocolEvent.Context);
                    break;
                default:
                    break;
            }
        }

        X509Certificate2 IContainer.ServiceCertificate
        {
            get { return this.certificate; }
        }

        Message IContainer.CreateMessage(ByteBuffer buffer)
        {
            return new BrokerMessage(buffer);
        }

        Link IContainer.CreateLink(ListenerConnection connection, ListenerSession session, Attach attach)
        {
            return new ListenerLink(session, attach);
        }

        bool IContainer.AttachLink(ListenerConnection connection, ListenerSession session, Link link, Attach attach)
        {
            foreach (var node in this.nodes)
            {
                if (node.AttachLink(connection, session, (ListenerLink)link, attach))
                {
                    return true;
                }
            }

            Source source = attach.Source as Source;
            Target target = attach.Target as Target;
            bool dynamic = false;
            string address = null;
            if (attach.Role)
            {
                address = source.Address;
                dynamic = source.Dynamic;
            }
            else
            {
                if (target != null)
                {
                    address = target.Address;
                    dynamic = target.Dynamic;
                }
                else if (attach.Target is Coordinator)
                {
                    this.txnManager.AddCoordinator((ListenerLink)link);
                    return true;
                }
            }

            if (dynamic)
            {
                address = string.Format("$dynamic.{0}", Interlocked.Increment(ref this.dynamidId));
                if (attach.Role)
                {
                    source.Dynamic = false;
                    source.Address = address;
                }
                else
                {
                    target.Address = address;
                    target.Dynamic = false;
                }
            }

            TestQueue queue;
            lock (this.queues)
            {
                if (!this.queues.TryGetValue(address, out queue))
                {
                    if (dynamic || (this.implicitQueue && !link.Name.StartsWith("$explicit:")))
                    {
                        queue = new TestQueue(this, address, !dynamic);
                        this.queues.Add(address, queue);
                    }
                    else
                    {
                        throw new AmqpException(ErrorCode.NotFound, string.Format("Node '{0}' not found", address));
                    }
                }
            }

            if (attach.Role)
            {
                queue.CreateConsumer((ListenerLink)link);
            }
            else
            {
                queue.CreatePublisher((ListenerLink)link);
            }

            return true;
        }

        IHandler CreateHandler(ConnectionListener listener)
        {
            return this;
        }

        sealed class BrokerMessage : Message
        {
            ByteBuffer buffer;
            int messageOffset;
            uint failedCount;

            public BrokerMessage(ByteBuffer buffer)
            {
                this.buffer = buffer;
                this.messageOffset = buffer.Capacity - buffer.Length - buffer.Size;
            }

            public ByteBuffer Buffer
            {
                get
                {
                    this.buffer.Seek(this.messageOffset);
                    this.CheckModified(this.buffer);
                    return this.buffer;
                }
            }

            public object LockedBy { get; set; }

            public LinkedListNode<BrokerMessage> Node { get; set; }

            public void Unlock()
            {
                this.LockedBy = null;
            }

            public void Modify(bool failed, Fields annotations)
            {
                if (failed)
                {
                    this.failedCount++;
                }
                if (annotations != null)
                {
                    if (this.MessageAnnotations == null) this.MessageAnnotations = new MessageAnnotations();
                    this.Merge(annotations, this.MessageAnnotations.Map);
                }
                this.LockedBy = null;
            }

            void CheckModified(ByteBuffer oldBuf)
            {
                if (this.failedCount == 0 && this.MessageAnnotations == null)
                {
                    return;
                }
                ByteBuffer newBuf = new ByteBuffer(oldBuf.Size, true);
                Header header = new Header();
                MessageAnnotations annotations = this.MessageAnnotations;
                int offset = oldBuf.Offset;
                while (oldBuf.Length > 0)
                {
                    offset = oldBuf.Offset;
                    var described = (RestrictedDescribed)Encoder.ReadDescribed(oldBuf, Encoder.ReadFormatCode(buffer));
                    if (described.Descriptor.Code == 0x70UL)
                    {
                        header = (Header)described;
                        this.WriteHeader(ref header, newBuf);
                    }
                    else if (described.Descriptor.Code == 0x71UL)
                    {
                        this.WriteHeader(ref header, newBuf);
                        AmqpBitConverter.WriteBytes(newBuf, oldBuf.Buffer, offset, oldBuf.Offset - offset);
                    }
                    else if (described.Descriptor.Code == 0x72UL)
                    {
                        this.WriteHeader(ref header, newBuf);
                        this.WriteMessageAnnotations(ref annotations, (MessageAnnotations)described, newBuf);
                    }
                    else
                    {
                        this.WriteHeader(ref header, newBuf);
                        this.WriteMessageAnnotations(ref annotations, null, newBuf);
                        AmqpBitConverter.WriteBytes(newBuf, oldBuf.Buffer, offset, oldBuf.WritePos - offset);
                        break;
                    }
                }
                this.buffer = newBuf;
                this.messageOffset = 0;
            }

            void WriteHeader(ref Header header, ByteBuffer buffer)
            {
                if (header != null)
                {
                    header.DeliveryCount += header.DeliveryCount + this.failedCount;
                    header.Encode(buffer);
                    header = null;
                }
            }

            void WriteMessageAnnotations(ref MessageAnnotations annotations, MessageAnnotations current, ByteBuffer buffer)
            {
                if (annotations != null && current != null)
                {
                    this.Merge(current.Map, annotations.Map);
                    annotations.Encode(buffer);
                    annotations = null;
                }
            }

            void Merge(Map source, Map dest)
            {
                foreach (var kvp in source)
                {
                    dest[kvp.Key] = kvp.Value;
                }
            }
        }

        sealed class TestQueue
        {
            readonly TestAmqpBroker broker;
            readonly string address;
            readonly bool isImplicit;
            readonly HashSet<Connection> connections;
            readonly LinkedList<BrokerMessage> messages;
            readonly LinkedList<Consumer> waiters;
            readonly Dictionary<int, Publisher> publishers;
            readonly Dictionary<int, Consumer> consumers;
            readonly object syncRoot;
            int currentId;

            public TestQueue(TestAmqpBroker broker, string address, bool isImplicit = false)
            {
                this.broker = broker;
                this.address = address;
                this.isImplicit = isImplicit;
                this.connections = isImplicit ? new HashSet<Connection>() : null;
                this.messages = new LinkedList<BrokerMessage>();
                this.waiters = new LinkedList<Consumer>();
                this.publishers = new Dictionary<int, Publisher>();
                this.consumers = new Dictionary<int, Consumer>();
                this.syncRoot = new object();
            }

            public void CreatePublisher(ListenerLink link)
            {
                int id = Interlocked.Increment(ref this.currentId);
                Publisher publisher = new Publisher(this, link, id);
                lock (this.syncRoot)
                {
                    this.publishers.Add(id, publisher);
                    this.OnClientConnected(link);
                }
            }

            public void CreateConsumer(ListenerLink link)
            {
                int id = Interlocked.Increment(ref this.currentId);
                Consumer consumer = new Consumer(this, link, id);
                lock (this.syncRoot)
                {
                    this.consumers.Add(id, consumer);
                    this.OnClientConnected(link);
                }
            }

            void OnClientConnected(Link link)
            {
                if (this.isImplicit && !this.connections.Contains(link.Session.Connection))
                {
                    this.connections.Add(link.Session.Connection);
                    link.Session.Connection.Closed += OnConnectionClosed;
                }
            }

            void OnConnectionClosed(IAmqpObject sender, Error error)
            {
                if (this.isImplicit)
                {
                    lock (this.syncRoot)
                    {
                        this.connections.Remove((Connection)sender);
                        if (this.connections.Count == 0)
                        {
                            this.broker.RemoveQueue(this.address);
                        }
                    }
                }
            }

            Consumer GetConsumerWithLock(Consumer exclude)
            {
                var node = this.waiters.First;

                while (node != null)
                {
                    var current = node;
                    var consumer = current.Value;
                    node = node.Next;

                    if (consumer.Credit == 0)
                    {
                        this.waiters.Remove(current);
                    }
                    else if (consumer != exclude)
                    {
                        consumer.Credit--;
                        if (consumer.Credit == 0)
                        {
                            this.waiters.Remove(current);
                        }
                        return consumer;
                    }
                }

                return null;
            }

            void Enqueue(BrokerMessage message)
            {
                LinkedListNode<BrokerMessage> node = null;
                if (message.Format == BatchFormat)
                {
                    var batch = Message.Decode(message.Buffer);
                    var dataList = batch.BodySection as DataList;
                    if (dataList != null)
                    {
                        for(int i = 0; i < dataList.Count; i++)
                        {
                            var msg = new BrokerMessage(dataList[i].Buffer);
                            lock (this.syncRoot)
                            {
                                msg.Node = this.messages.AddLast(msg);
                                if (node == null)
                                {
                                    node = msg.Node;
                                }
                            }
                        }
                    }
                    else
                    {
                        var data = batch.BodySection as Data;
                        if (data != null)
                        {
                            var msg = new BrokerMessage(data.Buffer);
                            lock (this.syncRoot)
                            {
                                node = msg.Node = this.messages.AddLast(msg);
                            }
                        }
                        else
                        {
                            // Ignore it for now
                            return;
                        }
                    }
                }
                else
                {
                    // clone the message as the incoming one is associated with a delivery already
                    BrokerMessage clone = new BrokerMessage(message.Buffer);
                    lock (this.syncRoot)
                    {
                        node = clone.Node = this.messages.AddLast(clone);
                    }
                }

                this.Deliver(node);
            }

            void Deliver(LinkedListNode<BrokerMessage> node)
            {
                Consumer consumer = null;
                BrokerMessage message = null;
                while (node != null)
                {
                    lock (this.syncRoot)
                    {
                        if (consumer == null || consumer.Credit == 0)
                        {
                            consumer = this.GetConsumerWithLock(null);
                            if (consumer == null)
                            {
                                return;
                            }
                        }

                        if (node.List == null)
                        {
                            node = this.messages.First;
                            continue;
                        }

                        LinkedListNode<BrokerMessage> next = node.Next;
                        message = node.Value;
                        if (message.LockedBy == null)
                        {
                            if (consumer.SettleOnSend)
                            {
                                this.messages.Remove(node);
                            }
                            else
                            {
                                message.LockedBy = consumer;
                            }
                        }
                        else
                        {
                            message = null;
                        }

                        node = next;
                    }

                    if (consumer != null && message != null)
                    {
                        consumer.Signal(message);
                    }
                }
            }

            void Dequeue(Consumer consumer, int credit, bool drain)
            {
                List<BrokerMessage> messageList = new List<BrokerMessage>();
                lock (this.syncRoot)
                {
                    consumer.Credit += credit;

                    var current = this.messages.First;
                    while (current != null)
                    {
                        if (current.Value.LockedBy == null)
                        {
                            messageList.Add(current.Value);
                            if (consumer.SettleOnSend)
                            {
                                var temp = current;
                                current = current.Next;
                                this.messages.Remove(temp);
                            }
                            else
                            {
                                current.Value.LockedBy = consumer;
                                current = current.Next;
                            }

                            consumer.Credit--;
                            if (consumer.Credit == 0)
                            {
                                break;
                            }
                        }
                        else
                        {
                            current = current.Next;
                        }
                    }

                    if (drain)
                    {
                        consumer.Credit = 0;
                    }
                    else if (consumer.Credit > 0)
                    {
                        this.waiters.AddLast(consumer);
                    }
                }

                foreach (var m in messageList)
                {
                    consumer.Signal(m);
                }
            }

            void Dequeue(BrokerMessage message)
            {
                lock (this.syncRoot)
                {
                    this.messages.Remove(message.Node);
                }
            }

            void Unlock(BrokerMessage message, Consumer exclude)
            {
                Consumer consumer = null;
                lock (this.syncRoot)
                {
                    message.Unlock();
                    consumer = this.GetConsumerWithLock(exclude);
                    if (consumer != null)
                    {
                        if (consumer.SettleOnSend)
                        {
                            this.messages.Remove(message.Node);
                        }
                        else
                        {
                            message.LockedBy = consumer;
                        }
                    }
                }

                if (consumer != null)
                {
                    consumer.Signal(message);
                }
            }

            void OnPublisherClosed(int id, Publisher publisher)
            {
                lock (this.syncRoot)
                {
                    this.publishers.Remove(id);
                }
            }

            void OnConsumerClosed(int id, Consumer consumer)
            {
                lock (this.syncRoot)
                {
                    this.consumers.Remove(id);
                    this.waiters.Remove(consumer);
                    var node = this.messages.First;
                    while (node != null)
                    {
                        var temp = node;
                        node = node.Next;
                        if (temp.Value.LockedBy == consumer)
                        {
                            this.Unlock(temp.Value, consumer);
                        }
                    }
                }
            }

            sealed class Publisher
            {
                static Action<ListenerLink, Message, DeliveryState, object> onMessage = OnMessage;
                static Action<Message, bool, object> onDischarge = OnDischarge;
                readonly TestQueue queue;
                readonly ListenerLink link;
                readonly int id;

                public Publisher(TestQueue queue, ListenerLink link, int id)
                {
                    this.queue = queue;
                    this.link = link;
                    this.id = id;

                    link.Closed += this.OnLinkClosed;
                    link.InitializeReceiver(200, onMessage, this);
                }

                void OnLinkClosed(IAmqpObject sender, Error error)
                {
                    this.queue.OnPublisherClosed(this.id, this);
                }

                static void OnMessage(ListenerLink link, Message message, DeliveryState deliveryState, object state)
                {
                    var thisPtr = (Publisher)state;
                    string errorCondition = null;
                    if (message.ApplicationProperties != null &&
                        (errorCondition = (string)message.ApplicationProperties["errorcondition"]) != null)
                    {
                        link.DisposeMessage(
                            message,
                            new Rejected() { Error = new Error(errorCondition) { Description = "message was rejected" } },
                            true);
                    }
                    else
                    {
                        var txnState = deliveryState as TransactionalState;
                        if (txnState != null)
                        {
                            Transaction txn = thisPtr.queue.broker.txnManager.GetTransaction(txnState.TxnId);
                            txn.AddOperation(message, onDischarge, thisPtr);
                            txnState.Outcome = new Accepted();
                        }
                        else
                        {
                            thisPtr.queue.Enqueue((BrokerMessage)message);
                            deliveryState = new Accepted();
                        }

                        thisPtr.link.DisposeMessage(message, deliveryState, true);
                    }
                }

                static void OnDischarge(Message message, bool fail, object state)
                {
                    if (!fail)
                    {
                        var thisPtr = (Publisher)state;
                        thisPtr.queue.Enqueue((BrokerMessage)message);
                    }
                }
            }

            sealed class Consumer
            {
                static readonly Action<int, Fields, object> onCredit = OnCredit;
                static readonly Action<Message, DeliveryState, bool, object> onDispose = OnDispose;
                static readonly Action<Message, bool, object> onDischarge = OnDischarge;
                readonly TestQueue queue;
                readonly ListenerLink link;
                readonly int id;

                public Consumer(TestQueue queue, ListenerLink link, int id)
                {
                    this.queue = queue;
                    this.link = link;
                    this.id = id;

                    link.Closed += this.OnLinkClosed;
                    link.InitializeSender(onCredit, onDispose, this);
                }

                public bool SettleOnSend { get { return this.link.SettleOnSend; } }

                public int Credit { get; set; }

                public void Signal(BrokerMessage message)
                {
                    this.link.SendMessage(message, message.Buffer);
                }

                void OnLinkClosed(IAmqpObject sender, Error error)
                {
                    this.Credit = 0;
                    this.queue.OnConsumerClosed(this.id, this);
                }

                static void OnCredit(int credit, Fields properties, object state)
                {
                    var thisPtr = (Consumer)state;
                    thisPtr.queue.Dequeue(thisPtr, credit, thisPtr.link.IsDraining);
                    if (thisPtr.link.IsDraining)
                    {
                        thisPtr.Credit = 0;
                        thisPtr.link.CompleteDrain();
                    }
                }

                static void OnDispose(Message message, DeliveryState deliveryState, bool settled, object state)
                {
                    var thisPtr = (Consumer)state;
                    if (deliveryState is TransactionalState)
                    {
                        Transaction txn = thisPtr.queue.broker.txnManager.GetTransaction(((TransactionalState)deliveryState).TxnId);
                        txn.AddOperation(message, onDischarge, thisPtr);
                    }
                    else
                    {
                        if (deliveryState is Released)
                        {
                            thisPtr.queue.Unlock((BrokerMessage)message, null);
                        }
                        else if (deliveryState is Modified)
                        {
                            Modified modified = (Modified)deliveryState;
                            ((BrokerMessage)message).Modify(modified.DeliveryFailed, modified.MessageAnnotations);
                            thisPtr.queue.Unlock((BrokerMessage)message, modified.UndeliverableHere ? thisPtr : null);
                        }
                        else
                        {
                            thisPtr.queue.Dequeue((BrokerMessage)message);
                        }
                    }
                }

                static void OnDischarge(Message message, bool fail, object state)
                {
                    if (!fail)
                    {
                        var thisPtr = (Consumer)state;
                        thisPtr.queue.Dequeue((BrokerMessage)message);
                    }
                }
            }
        }

        sealed class TxnOperation
        {
            public Message Message;

            public Action<Message, bool, object> Callback;

            public object State;
        }

        sealed class Transaction
        {
            readonly Queue<TxnOperation> operations;

            public Transaction()
            {
                this.operations = new Queue<TxnOperation>();
            }

            public int Id { get; set; }

            public void AddOperation(Message message, Action<Message, bool, object> callback, object state)
            {
                var op = new TxnOperation() { Message = message, Callback = callback, State = state };
                lock (this.operations)
                {
                    this.operations.Enqueue(op);
                }
            }

            public void Discharge(bool fail)
            {
                foreach (var op in this.operations)
                {
                    op.Callback(op.Message, fail, op.State);
                }
            }
        }

        sealed class TxnManager
        {
            readonly HashSet<ListenerLink> coordinators;
            readonly Dictionary<int, Transaction> transactions;
            int id;

            public TxnManager()
            {
                this.coordinators = new HashSet<ListenerLink>();
                this.transactions = new Dictionary<int, Transaction>();
            }

            public void AddCoordinator(ListenerLink link)
            {
                lock (this.coordinators)
                {
                    this.coordinators.Add(link);
                }

                link.Closed += (o, e) => this.RemoveCoordinator((ListenerLink)o);
                link.InitializeReceiver(100, OnMessage, this);
            }

            public Transaction GetTransaction(byte[] txnId)
            {
                int id = BitConverter.ToInt32(txnId, 0);
                return this.transactions[id];
            }

            void RemoveCoordinator(ListenerLink link)
            {
                lock (this.coordinators)
                {
                    this.coordinators.Remove(link);
                }
            }

            static void OnMessage(ListenerLink link, Message message, DeliveryState deliveryState, object state)
            {
                var thisPtr = (TxnManager)state;
                object body;
                try
                {
                    body = Message.Decode(((BrokerMessage)message).Buffer).Body;
                }
                catch (Exception exception)
                {
                    Trace.WriteLine(TraceLevel.Error, exception.Message);
                    link.DisposeMessage(
                        message,
                        new Rejected() { Error = new Error(ErrorCode.DecodeError) { Description = "Cannot decode txn message" } },
                        true);

                    return;
                }

                if (body is Declare)
                {
                    int txnId = thisPtr.CreateTransaction();
                    var outcome = new Declared() { TxnId = BitConverter.GetBytes(txnId) };
                    link.DisposeMessage(message, outcome, true);
                }
                else if (body is Discharge)
                {
                    Discharge discharge = (Discharge)body;
                    int txnId = BitConverter.ToInt32(discharge.TxnId, 0);
                    Transaction txn;
                    if (thisPtr.transactions.TryGetValue(txnId, out txn))
                    {
                        lock (thisPtr.transactions)
                        {
                            thisPtr.transactions.Remove(txnId);
                        }

                        txn.Discharge(discharge.Fail);
                        link.DisposeMessage(message, new Accepted(), true);
                    }
                    else
                    {
                        link.DisposeMessage(
                            message, 
                            new Rejected() { Error = new Error(ErrorCode.NotFound) { Description = "Transaction not found" } },
                            true);
                    }
                }
                else
                {
                    link.DisposeMessage(
                        message,
                        new Rejected() { Error = new Error(ErrorCode.NotImplemented) { Description = "Unsupported message body" } },
                        true);
                }
            }

            int CreateTransaction()
            {
                Transaction txn = new Transaction() { Id = Interlocked.Increment(ref this.id) };
                lock (this.transactions)
                {
                    this.transactions.Add(txn.Id, txn);
                    return txn.Id;
                }
            }
        }
    }
}
