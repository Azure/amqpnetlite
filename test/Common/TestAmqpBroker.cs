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
    using global::Amqp.Listener;
    using global::Amqp.Transactions;
    using global::Amqp.Types;

    public sealed class TestAmqpBroker : IContainer
    {
        readonly X509Certificate2 certificate;
        readonly Dictionary<string, TestQueue> queues;
        readonly ConnectionListener[] listeners;
        readonly TxnManager txnManager;
        bool implicitQueue;
        int dynamidId;

        public TestAmqpBroker(IList<string> endpoints, string userInfo, string certValue, string[] queues)
        {
            this.txnManager = new TxnManager();
            this.queues = new Dictionary<string, TestQueue>();
            if (queues != null)
            {
                foreach (string q in queues)
                {
                    this.queues.Add(q, new TestQueue(this));
                }
            }
            else
            {
                this.implicitQueue = true;
            }

            this.certificate = certValue == null ? null : GetCertificate(certValue);

            string containerId = "TestAmqpBroker:" + Guid.NewGuid().ToString().Substring(0, 8);
            this.listeners = new ConnectionListener[endpoints.Count];
            for (int i = 0; i < endpoints.Count; i++)
            {
                this.listeners[i] = new ConnectionListener(endpoints[i], this);
                this.listeners[i].AMQP.MaxSessionsPerConnection = 1000;
                this.listeners[i].AMQP.ContainerId = containerId;
                this.listeners[i].AMQP.IdleTimeout = 4 * 60 * 1000;
                this.listeners[i].AMQP.MaxFrameSize = 64 * 1024;
                if (userInfo != null)
                {
                    string[] a = userInfo.Split(':');
                    this.listeners[i].SASL.EnablePlainMechanism(
                        Uri.UnescapeDataString(a[0]),
                        a.Length == 1 ? string.Empty : Uri.UnescapeDataString(a[1]));
                }
            }
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

        public void AddQueue(string queue)
        {
            lock (this.queues)
            {
                this.queues.Add(queue, new TestQueue(this));
            }
        }

        public void RemoveQueue(string queue)
        {
            lock (this.queues)
            {
                this.queues.Remove(queue);
            }
        }

        static X509Certificate2 GetCertificate(string certFindValue)
        {
            StoreLocation[] locations = new StoreLocation[] { StoreLocation.LocalMachine, StoreLocation.CurrentUser };
            foreach (StoreLocation location in locations)
            {
                X509Store store = new X509Store(StoreName.My, location);
                store.Open(OpenFlags.OpenExistingOnly);

                X509Certificate2Collection collection = store.Certificates.Find(
                    X509FindType.FindBySubjectName,
                    certFindValue,
                    false);

                if (collection.Count == 0)
                {
                    collection = store.Certificates.Find(
                        X509FindType.FindByThumbprint,
                        certFindValue,
                        false);
                }

#if DOTNET
                store.Dispose();
#else
                store.Close();
#endif
                if (collection.Count > 0)
                {
                    return collection[0];
                }
            }

            throw new ArgumentException("No certificate can be found using the find value " + certFindValue);
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
                    if (dynamic || this.implicitQueue)
                    {
                        queue = new TestQueue(this);
                        this.queues.Add(address, queue);
                        connection.Closed += (o, e) => this.RemoveQueue(address);
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

        sealed class BrokerMessage : Message
        {
            ByteBuffer buffer;
            int messageOffset;

            public BrokerMessage(ByteBuffer buffer)
            {
                this.buffer = buffer;
                this.messageOffset = buffer.Offset;
            }

            public ByteBuffer Buffer
            {
                get
                {
                    this.buffer.Seek(this.messageOffset);
                    return this.buffer;
                }
            }

            public object LockedBy { get; set; }

            public LinkedListNode<BrokerMessage> Node { get; set; }

            public void Unlock()
            {
                this.LockedBy = null;
            }
        }

        sealed class TestQueue
        {
            readonly TestAmqpBroker broker;
            readonly LinkedList<BrokerMessage> messages;
            readonly Queue<Consumer> waiters;
            readonly Dictionary<int, Publisher> publishers;
            readonly Dictionary<int, Consumer> consumers;
            readonly object syncRoot;
            int currentId;

            public TestQueue(TestAmqpBroker broker)
            {
                this.broker = broker;
                this.messages = new LinkedList<BrokerMessage>();
                this.waiters = new Queue<Consumer>();
                this.publishers = new Dictionary<int, Publisher>();
                this.consumers = new Dictionary<int, Consumer>();
                this.syncRoot = this.waiters;
            }

            public void CreatePublisher(ListenerLink link)
            {
                int id = Interlocked.Increment(ref this.currentId);
                Publisher publisher = new Publisher(this, link, id);
                lock (this.publishers)
                {
                    this.publishers.Add(id, publisher);
                }
            }

            public void CreateConsumer(ListenerLink link)
            {
                int id = Interlocked.Increment(ref this.currentId);
                Consumer consumer = new Consumer(this, link, id);
                lock (this.consumers)
                {
                    this.consumers.Add(id, consumer);
                }
            }

            Consumer GetConsumerWithLock()
            {
                Consumer consumer = null;
                while (this.waiters.Count > 0)
                {
                    consumer = this.waiters.Peek();
                    if (consumer.Credit > 0)
                    {
                        consumer.Credit--;
                        if (consumer.Credit == 0)
                        {
                            this.waiters.Dequeue();
                        }

                        break;
                    }
                    else
                    {
                        this.waiters.Dequeue();
                        consumer = null;
                    }
                }

                return consumer;
            }

            void Enqueue(BrokerMessage message)
            {
                // clone the message as the incoming one is associated with a delivery already
                BrokerMessage clone = new BrokerMessage(message.Buffer);
                Consumer consumer = null;
                lock (this.syncRoot)
                {
                    consumer = this.GetConsumerWithLock();
                    if (consumer == null)
                    {
                        clone.Node = this.messages.AddLast(clone);
                    }
                    else
                    {
                        if (!consumer.SettleOnSend)
                        {
                            clone.LockedBy = consumer;
                            clone.Node = this.messages.AddLast(clone);
                        }
                    }
                }

                if (consumer != null)
                {
                    consumer.Signal(clone);
                }
            }

            void Dequeue(Consumer consumer, int credit)
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

                    if (consumer.Credit > 0)
                    {
                        this.waiters.Enqueue(consumer);
                    }
                }

                foreach (var m in messageList)
                {
                    consumer.Signal(m);
                }
            }

            public void Dequeue(BrokerMessage message)
            {
                lock (this.syncRoot)
                {
                    this.messages.Remove(message.Node);
                }
            }

            public void Unlock(BrokerMessage message)
            {
                Consumer consumer = null;
                lock (this.syncRoot)
                {
                    message.Unlock();
                    consumer = this.GetConsumerWithLock();
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

            void OnConsumerClosed(int id, Consumer consumer)
            {
                lock (this.syncRoot)
                {
                    this.consumers.Remove(id);
                    var node = this.messages.First;
                    while (node != null)
                    {
                        var temp = node;
                        node = node.Next;
                        if (temp.Value.LockedBy == consumer)
                        {
                            this.Unlock(temp.Value);
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

                void OnLinkClosed(AmqpObject sender, Error error)
                {
                    lock (this.queue.publishers)
                    {
                        this.queue.publishers.Remove(this.id);
                    }
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
                            new Rejected() { Error = new Error() { Condition = errorCondition, Description = "message was rejected" } },
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
                static readonly Action<int, object> onCredit = OnCredit;
                static readonly Action<Message, DeliveryState, bool, object> onDispose = OnDispose;
                static readonly Action<Message, bool, object> onDischarge = OnDischarge;
                readonly TestQueue queue;
                readonly ListenerLink link;
                readonly int id;
                int tag;

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

                ArraySegment<byte> GetNextTag()
                {
                    return new ArraySegment<byte>(BitConverter.GetBytes(Interlocked.Increment(ref this.tag)));
                }

                void OnLinkClosed(AmqpObject sender, Error error)
                {
                    this.Credit = 0;
                    this.queue.OnConsumerClosed(this.id, this);
                }

                static void OnCredit(int credit, object state)
                {
                    var thisPtr = (Consumer)state;
                    thisPtr.queue.Dequeue(thisPtr, credit);
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
                            thisPtr.queue.Unlock((BrokerMessage)message);
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
                        new Rejected() { Error = new Error() { Condition = ErrorCode.DecodeError, Description = "Cannot decode txn message" } },
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
                            new Rejected() { Error = new Error() { Condition = ErrorCode.NotFound, Description = "Transaction not found" } },
                            true);
                    }
                }
                else
                {
                    link.DisposeMessage(
                        message,
                        new Rejected() { Error = new Error() { Condition = ErrorCode.NotImplemented, Description = "Unsupported message body" } },
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
