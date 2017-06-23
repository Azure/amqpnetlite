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
        int totalCredit;
        int credit;
        int restored;

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
        /// <param name="credit">The link credit to issue.</param>
        /// <param name="onMessage">If specified, the callback to invoke when messages are received.
        /// If not specified, call Receive method to get the messages.</param>
        public void Start(int credit, MessageCallback onMessage = null)
        {
            this.onMessage = onMessage;
            this.SetCredit(credit, true);
        }

        /// <summary>
        /// Sets a credit on the link. A flow is sent to the peer to update link flow control state.
        /// </summary>
        /// <param name="credit">The new link credit.</param>
        /// <param name="autoRestore">If true, link credit is auto-restored when a message is accepted
        /// or rejected by the caller. If false, caller is responsible for manage link credits.</param>
        public void SetCredit(int credit, bool autoRestore = true)
        {
            uint dc;
            lock (this.ThisLock)
            {
                if (this.IsDetaching)
                {
                    return;
                }

                this.totalCredit = autoRestore ? credit : 0;
                this.credit = credit;
                this.restored = 0;
                dc = this.deliveryCount;
            }

            this.SendFlow(dc, (uint)credit, false);
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
        public Message Receive(TimeSpan timeout)
        {
            return this.ReceiveInternal(null, (int)(timeout.Ticks / 10000));
        }

        /// <summary>
        /// Accepts a message. It sends an accepted outcome to the peer.
        /// </summary>
        /// <param name="message">The message to accept.</param>
        public void Accept(Message message)
        {
            this.ThrowIfDetaching("Accept");
            this.DisposeMessage(message, new Accepted());
        }

        /// <summary>
        /// Releases a message. It sends a released outcome to the peer.
        /// </summary>
        /// <param name="message">The message to release.</param>
        public void Release(Message message)
        {
            this.ThrowIfDetaching("Release");
            this.DisposeMessage(message, new Released());
        }

        /// <summary>
        /// Rejects a message. It sends a rejected outcome to the peer.
        /// </summary>
        /// <param name="message">The message to reject.</param>
        /// <param name="error">The error, if any, for the rejection.</param>
        public void Reject(Message message, Error error = null)
        {
            this.ThrowIfDetaching("Reject");
            this.DisposeMessage(message, new Rejected() { Error = error });
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
                });
        }

        internal override void OnFlow(Flow flow)
        {
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
                        this.OnDeliverMessage();
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
                this.OnDeliverMessage();
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

        void OnDeliverMessage()
        {
            if (this.totalCredit > 0 &&
                Interlocked.Increment(ref this.restored) >= (this.totalCredit / 2))
            {
                this.SetCredit(this.totalCredit, true);
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
                    this.OnDeliverMessage();
                    return first.Message;
                }

                if (timeout > 0)
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

            Message message = null;
            if (timeout > 0)
            {
                message = waiter.Wait(timeout);
                if (this.Error != null)
                {
                    throw new AmqpException(this.Error);
                }
            }

            return message;
        }
        
        void DisposeMessage(Message message, Outcome outcome)
        {
            Delivery delivery = message.Delivery;
            if (delivery == null || delivery.Settled)
            {
                return;
            }

            DeliveryState state = outcome;
            bool settled = true;
#if NETFX || NETFX40
            var txnState = Amqp.Transactions.ResourceManager.GetTransactionalStateAsync(this).Result;
            if (txnState != null)
            {
                txnState.Outcome = outcome;
                state = txnState;
                settled = false;
            }
#endif
            this.Session.DisposeDelivery(true, delivery, state, settled);
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
            this.credit--;
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
                    if (old == 1)
                    {
                        this.timer.Dispose();
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
                thisPtr.Signal(null);
            }
        }
#endif
    }
}