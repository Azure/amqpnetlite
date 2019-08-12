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

namespace Amqp
{
    using System;
    using System.Threading;
    using Amqp.Framing;
    using Amqp.Handler;
    using Amqp.Types;

    /// <summary>
    /// The ReceiverLink class represents a link that accepts incoming messages.
    /// </summary>
    public partial class ReceiverLink : Link
    {
#if NETFX || NETFX40 || DOTNET
        const int DefaultCredit = 200;
#else
        const int DefaultCredit = 20;
#endif
        // flow control
        SequenceNumber deliveryCount;
        int totalCredit;          // total credit set by app or the default
        bool drain;               // a drain cycle is in progress
        int pending;              // queued or being processed by application
        int credit;               // remaining credit
        int restored;             // processed by the application
        int flowThreshold;        // auto restore threshold for a flow

        // received messages queue
        LinkedList receivedMessages;
        Delivery deliveryCurrent;

        // pending receivers
        LinkedList waiterList;
        MessageCallback onMessage;

        /// <summary>
        /// Initializes a receiver link.
        /// </summary>
        /// <param name="session">The session within which to create the link.</param>
        /// <param name="name">The link name.</param>
        /// <param name="address">The node address.</param>
        public ReceiverLink(Session session, string name, string address)
            : this(session, name, new Source() { Address = address }, null)
        {
        }

        /// <summary>
        /// Initializes a receiver link.
        /// </summary>
        /// <param name="session">The session within which to create the link.</param>
        /// <param name="name">The link name.</param>
        /// <param name="source">The source on attach that specifies the message source.</param>
        /// <param name="onAttached">The callback to invoke when an attach is received from peer.</param>
        public ReceiverLink(Session session, string name, Source source, OnAttached onAttached)
            : this(session, name, new Attach() { Source = source, Target = new Target() }, onAttached)
        {
        }

        /// <summary>
        /// Initializes a receiver link.
        /// </summary>
        /// <param name="session">The session within which to create the link.</param>
        /// <param name="name">The link name.</param>
        /// <param name="attach">The attach frame to send for this link.</param>
        /// <param name="onAttached">The callback to invoke when an attach is received from peer.</param>
        public ReceiverLink(Session session, string name, Attach attach, OnAttached onAttached)
            : base(session, name, onAttached)
        {
            this.totalCredit = -1;
            this.receivedMessages = new LinkedList();
            this.waiterList = new LinkedList();
            this.SendAttach(true, 0, attach);
        }

        /// <summary>
        /// Starts the message pump.
        /// </summary>
        /// <param name="credit">The link credit to issue. See <seealso cref="SetCredit(int, bool)"/> for more details.</param>
        /// <param name="onMessage">If specified, the callback to invoke when messages are received.
        /// If not specified, call Receive method to get the messages.</param>
        public void Start(int credit, MessageCallback onMessage = null)
        {
            this.onMessage = onMessage;
            this.SetCredit(credit, true);
        }

        /// <summary>
        /// Sets a credit on the link. The credit controls how many messages the peer can send.
        /// </summary>
        /// <param name="credit">The new link credit.</param>
        /// <param name="autoRestore">If true, this method is the same as SetCredit(credit, CreditMode.Auto);
        /// if false, it is the same as SetCredit(credit, CreditMode.Manual).</param>
        public void SetCredit(int credit, bool autoRestore = true)
        {
            this.SetCredit(credit, autoRestore ? CreditMode.Auto : CreditMode.Manual); 
        }

        /// <summary>
        /// Sets a credit on the link and the credit management mode.
        /// </summary>
        /// <param name="credit">The new link credit.</param>
        /// <param name="creditMode">The credit management mode.</param>
        /// <param name="flowThreshold">If credit mode is Auto, it is the threshold of restored
        /// credits that trigers a flow; ignored otherwise.</param>
        /// <remarks>
        /// The receiver link has a default link credit (200). If the default value is not optimal,
        /// application should call this method once after the receiver link is created.
        /// In Auto credit mode, the <paramref name="credit"/> parameter defines the total credits
        /// of the link which is also the total number messages the remote peer can send. 
        /// The link keeps track of acknowledged messages and triggers a flow
        /// when a threshold is reached. The default threshold is half of <see cref="credit"/>. Application
        /// acknowledges a message by calling <see cref="Accept(Message)"/>, <see cref="Reject(Message, Error)"/>,
        /// <see cref="Release(Message)"/> or <see cref="Modify(Message, bool, bool, Fields)"/> method.
        /// In Manual credit mode, the <paramref name="credit"/> parameter defines the extra credits
        /// of the link which is the additional messages the remote peer can send.
        /// Please note the following.
        /// 1. In Auto mode, calling this method multiple times with different credits is allowed but not recommended.
        ///    Application may do this if, for example, it needs to control local queue depth based on resource usage.
        ///    If credit is reduced, the link maintains a buffer so incoming messages are still allowed.
        /// 2. The creditMode should not be changed after it is initially set.
        /// 3. To stop a receiver link, set <paramref name="credit"/> to 0. However application should expect
        ///    in-flight messages to come as a result of the previous credit. It is recommended to use the
        ///    Drain mode if the application wishes to stop the messages after a given credit is used.
        /// 4. In drain credit mode, if a drain cycle is still in progress, the call simply returns without
        ///    sending a flow. Application is expected to keep calling <see cref="Receive()"/> in a loop
        ///    until all messages are received or a null message is returned.
        /// 5. In manual credit mode, application is responsible for keeping track of processed messages
        ///    and issue more credits when certain conditions are met. 
        /// </remarks>
        public void SetCredit(int credit, CreditMode creditMode, int flowThreshold = -1)
        {
            lock (this.ThisLock)
            {
                if (this.IsDetaching)
                {
                    return;
                }

                if (this.totalCredit < 0)
                {
                    this.totalCredit = 0;
                }

                var sendFlow = false;
                if (creditMode == CreditMode.Drain)
                {
                    if (!this.drain)
                    {
                        // start a drain cycle.
                        this.pending = 0;
                        this.restored = 0;
                        this.drain = true;
                        this.credit = credit;
                        this.flowThreshold = -1;
                        sendFlow = true;
                    }
                }
                else if (creditMode == CreditMode.Manual)
                {
                    this.drain = false;
                    this.pending = 0;
                    this.restored = 0;
                    this.flowThreshold = -1;
                    this.credit += credit;
                    sendFlow = true;
                }
                else
                {
                    this.drain = false;
                    this.flowThreshold = flowThreshold >= 0 ? flowThreshold : credit / 2;
                    // Only change remaining credit if total credit was increased, to allow
                    // accepting incoming messages. If total credit is reduced, only update 
                    // total so credit will be later auto-restored to the new limit.
                    int delta = credit - this.totalCredit + this.restored;
                    if (delta > 0)
                    {
                        this.credit += delta;
                        this.restored = 0;
                        sendFlow = true;
                    }
                }

                this.totalCredit = credit;
                if (sendFlow)
                {
                    this.SendFlow(this.deliveryCount, (uint)this.credit, this.drain);
                }
            }
        }

        /// <summary>
        /// Receives a message. The call is blocked until a message is available or after a default wait time.
        /// </summary>
        /// <returns>A Message object if available; otherwise a null value.</returns>
        public Message Receive()
        {
            return this.ReceiveInternal(null, AmqpObject.DefaultTimeout);
        }

        /// <summary>
        /// Receives a message. The call is blocked until a message is available or the timeout duration expires.
        /// </summary>
        /// <param name="timeout">The time to wait for a message.</param>
        /// <returns>A Message object if available; otherwise a null value.</returns>
        /// <remarks>
        /// Use TimeSpan.MaxValue or Timeout.InfiniteTimeSpan to wait infinitely. If TimeSpan.Zero is supplied,
        /// the call returns immediately.
        /// </remarks>
        public Message Receive(TimeSpan timeout)
        {
            int waitTime = timeout == TimeSpan.MaxValue ? -1 : (int)(timeout.Ticks / 10000);
#if NETFX || DOTNET || NETFX_CORE || UWP
            if (timeout == Timeout.InfiniteTimeSpan)
            {
                waitTime = -1;
            }
#endif
            return this.ReceiveInternal(null, waitTime);
        }

        /// <summary>
        /// Accepts a message. It sends an accepted outcome to the peer.
        /// </summary>
        /// <param name="message">The message to accept.</param>
        public void Accept(Message message)
        {
            this.ThrowIfDetaching("Accept");
            this.DisposeMessage(message, new Accepted(), null);
        }

        /// <summary>
        /// Releases a message. It sends a released outcome to the peer.
        /// </summary>
        /// <param name="message">The message to release.</param>
        public void Release(Message message)
        {
            this.ThrowIfDetaching("Release");
            this.DisposeMessage(message, new Released(), null);
        }

        /// <summary>
        /// Rejects a message. It sends a rejected outcome to the peer.
        /// </summary>
        /// <param name="message">The message to reject.</param>
        /// <param name="error">The error, if any, for the rejection.</param>
        public void Reject(Message message, Error error = null)
        {
            this.ThrowIfDetaching("Reject");
            this.DisposeMessage(message, new Rejected() { Error = error }, null);
        }

        /// <summary>
        /// Modifies a message. It sends a modified outcome to the peer.
        /// </summary>
        /// <param name="message">The message to modify.</param>
        /// <param name="deliveryFailed">If set, the message's delivery-count is incremented.</param>
        /// <param name="undeliverableHere">Indicates if the message should not be redelivered to this endpoint.</param>
        /// <param name="messageAnnotations">Annotations to be combined with the current message annotations.</param>
        public void Modify(Message message, bool deliveryFailed, bool undeliverableHere = false, Fields messageAnnotations = null)
        {
            this.ThrowIfDetaching("Modify");
            this.DisposeMessage(message, new Modified()
                {
                    DeliveryFailed = deliveryFailed,
                    UndeliverableHere = undeliverableHere,
                    MessageAnnotations = messageAnnotations
                },
                null);
        }

        /// <summary>
        /// Completes a received message. It settles the delivery and sends
        /// a disposition with the delivery state to the remote peer.
        /// </summary>
        /// <param name="message">The message to complete.</param>
        /// <param name="deliveryState">An <see cref="Outcome"/> or a TransactionalState.</param>
        /// <remarks>This method is not transaction aware. It should be used to bypass
        /// transaction context look up when transactions are not used at all, or
        /// to manage AMQP transactions directly by providing a TransactionalState to
        /// <paramref name="deliveryState"/>.</remarks>
        public void Complete(Message message, DeliveryState deliveryState)
        {
            this.DisposeMessage(message, null, deliveryState);
        }

        internal override void OnFlow(Flow flow)
        {
            lock (this.ThisLock)
            {
                if (this.drain)
                {
                    this.drain = flow.Drain;
                    this.deliveryCount = flow.DeliveryCount;
                    this.credit = Math.Min(0, (int)flow.LinkCredit);
                }
            }
        }

        internal override void OnTransfer(Delivery delivery, Transfer transfer, ByteBuffer buffer)
        {
            if (delivery == null)
            {
                delivery = this.deliveryCurrent;
                AmqpBitConverter.WriteBytes(delivery.Buffer, buffer.Buffer, buffer.Offset, buffer.Length);
            }
            else
            {
                buffer.AddReference();
                delivery.Buffer = buffer;
                lock (this.ThisLock)
                {
                    this.OnDelivery(transfer.DeliveryId);
                }
            }

            if (!transfer.More)
            {
                this.deliveryCurrent = null;
                delivery.Message = Message.Decode(delivery.Buffer);

                IHandler handler = this.Session.Connection.Handler;
                if (handler != null && handler.CanHandle(EventId.ReceiveDelivery))
                {
                    handler.Handle(Event.Create(EventId.ReceiveDelivery, this.Session.Connection, this.Session, this, context: delivery));
                }

                Waiter waiter;
                MessageCallback callback = this.onMessage;
                lock (this.ThisLock)
                {
                    waiter = (Waiter)this.waiterList.First;
                    if (waiter != null)
                    {
                        this.waiterList.Remove(waiter);
                    }
                    else if (callback == null)
                    {
                        this.receivedMessages.Add(new MessageNode() { Message = delivery.Message });
                        return;
                    }
                }

                while (waiter != null)
                {
                    if (waiter.Signal(delivery.Message))
                    {
                        return;
                    }

                    lock (this.ThisLock)
                    {
                        waiter = (Waiter)this.waiterList.First;
                        if (waiter != null)
                        {
                            this.waiterList.Remove(waiter);
                        }
                        else if (callback == null)
                        {
                            this.receivedMessages.Add(new MessageNode() { Message = delivery.Message });
                            return;
                        }
                    }
                }

                Fx.Assert(waiter == null, "waiter must be null now");
                Fx.Assert(callback != null, "callback must not be null now");
                callback(this, delivery.Message);
            }
            else
            {
                this.deliveryCurrent = delivery;
            }
        }

        internal override void OnAttach(uint remoteHandle, Attach attach)
        {
            base.OnAttach(remoteHandle, attach);
            this.deliveryCount = attach.InitialDeliveryCount;
        }

        internal override void OnDeliveryStateChanged(Delivery delivery)
        {
        }

        /// <summary>
        /// Closes the receiver link.
        /// </summary>
        /// <param name="error">The error for the closure.</param>
        /// <returns></returns>
        protected override bool OnClose(Error error)
        {
            this.OnAbort(error);

            return base.OnClose(error);
        }

        /// <summary>
        /// Aborts the receiver link.
        /// </summary>
        /// <param name="error">The error for the abort.</param>
        protected override void OnAbort(Error error)
        {
            Waiter waiter;
            lock (this.ThisLock)
            {
                waiter = (Waiter)this.waiterList.Clear();
            }

            while (waiter != null)
            {
                waiter.Signal(null);
                waiter = (Waiter)waiter.Next;
            }
        }

        internal Message ReceiveInternal(MessageCallback callback, int timeout = 60000)
        {
            Waiter waiter = null;
            lock (this.ThisLock)
            {
                this.ThrowIfDetaching("Receive");
                MessageNode first = (MessageNode)this.receivedMessages.First;
                if (first != null)
                {
                    this.receivedMessages.Remove(first);
                    return first.Message;
                }

                if (timeout != 0)
                {
#if NETFX || NETFX40 || DOTNET || NETFX_CORE || WINDOWS_STORE || WINDOWS_PHONE
                    waiter = callback == null ? (Waiter)new SyncWaiter() : new AsyncWaiter(this, callback);
#else
                    waiter = new SyncWaiter();
#endif
                    this.waiterList.Add(waiter);
                }
            }

            // send credit after waiter creation to avoid race condition
            if (this.totalCredit < 0)
            {
                this.SetCredit(DefaultCredit, true);
            }

            if (timeout == 0)
            {
                return null;
            }

            Message message = null;
            message = waiter.Wait(timeout);
            if (this.Error != null)
            {
                throw new AmqpException(this.Error);
            }

            return message;
        }

        // deliveryState overwrites outcome
        void DisposeMessage(Message message, Outcome outcome, DeliveryState deliveryState)
        {
            Delivery delivery = message.Delivery;
            if (delivery == null || delivery.Link != this)
            {
                throw new InvalidOperationException("Message was not received by this link.");
            }

            if (!delivery.Settled)
            {
                DeliveryState state = outcome != null ? outcome : deliveryState;
                bool settled = true;
#if NETFX || NETFX40 || NETSTANDARD2_0
                if (outcome != null)
                {
                    var txnState = Amqp.Transactions.ResourceManager.GetTransactionalStateAsync(this).Result;
                    if (txnState != null)
                    {
                        txnState.Outcome = outcome;
                        state = txnState;
                        settled = false;
                    }
                }
#endif
                this.Session.DisposeDelivery(true, delivery, state, settled);
            }

            lock (this.ThisLock)
            {
                this.restored++;
                this.pending--;
                if (this.flowThreshold >= 0 && this.restored >= this.flowThreshold)
                {
                    // total credit may be reduced. restore to what is allowed
                    int delta = Math.Min(this.restored, this.totalCredit - this.credit - this.pending);
                    if (delta > 0)
                    {
                        this.credit += delta;
                        this.SendFlow(this.deliveryCount, (uint)this.credit, false);
                    }

                    this.restored = 0;
                }
            }
        }

        void OnDelivery(SequenceNumber deliveryId)
        {
            // called with lock held
            if (this.credit <= 0)
            {
                throw new AmqpException(ErrorCode.TransferLimitExceeded,
                    Fx.Format(SRAmqp.DeliveryLimitExceeded, deliveryId));
            }

            this.deliveryCount++;
            this.pending++;
            this.credit--;
            if (this.drain && this.credit == 0)
            {
                this.drain = false;
            }
        }

        sealed class MessageNode : INode
        {
            public Message Message { get; set; }

            public INode Previous { get; set; }

            public INode Next { get; set; }
        }

        abstract class Waiter : INode
        {
            public INode Previous { get; set; }

            public INode Next { get; set; }

            public abstract Message Wait(int timeout);

            public abstract bool Signal(Message message);
        }

        sealed class SyncWaiter : Waiter
        {
            readonly ManualResetEvent signal;
            Message message;
            bool expired;

            public SyncWaiter()
            {
                this.signal = new ManualResetEvent(false);
            }

            public override Message Wait(int timeout)
            {
                this.signal.WaitOne(timeout);
                lock (this)
                {
                    this.expired = this.message == null;
                    return this.message;
                }
            }

            public override bool Signal(Message message)
            {
                bool signaled = false;
                lock (this)
                {
                    if (!this.expired)
                    {
                        this.message = message;
                        signaled = true;
                    }
                }

                this.signal.Set();
                return signaled;
            }
        }

#if NETFX || NETFX40 || DOTNET || NETFX_CORE || WINDOWS_STORE || WINDOWS_PHONE
        sealed class AsyncWaiter : Waiter
        {
            readonly static TimerCallback onTimer = OnTimer;
            readonly ReceiverLink link;
            readonly MessageCallback callback;
            Timer timer;
            int state;  // 0: created, 1: waiting, 2: signaled

            public AsyncWaiter(ReceiverLink link, MessageCallback callback)
            {
                this.link = link;
                this.callback = callback;
            }

            public override Message Wait(int timeout)
            {
                this.timer = new Timer(onTimer, this, timeout, -1);
                if (Interlocked.CompareExchange(ref this.state, 1, 0) != 0)
                {
                    // already signaled
                    this.timer.Dispose();
                }

                return null;
            }

            public override bool Signal(Message message)
            {
                int old = Interlocked.Exchange(ref this.state, 2);
                if (old != 2)
                {
                    Timer temp = this.timer;
                    if (temp != null)
                    {
                        temp.Dispose();
                    }

                    this.callback(this.link, message);
                    return true;
                }
                else
                {
                    return false;
                }
            }

            static void OnTimer(object state)
            {
                var thisPtr = (AsyncWaiter)state;
                lock (thisPtr.link.ThisLock)
                {
                    thisPtr.link.waiterList.Remove(thisPtr);
                }

                thisPtr.Signal(null);
            }
        }
#endif
    }
}