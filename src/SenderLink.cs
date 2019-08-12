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

    /// <summary>
    /// The SenderLink represents a link that sends outgoing messages.
    /// </summary>
    public partial class SenderLink : Link
    {
        // flow control
        SequenceNumber deliveryCount;
        int credit;

        // outgoing queue
        SenderSettleMode settleMode;
        LinkedList outgoingList;
        bool writing;

        /// <summary>
        /// Initializes a sender link.
        /// </summary>
        /// <param name="session">The session within which to create the link.</param>
        /// <param name="name">The link name.</param>
        /// <param name="address">The node address.</param>
        public SenderLink(Session session, string name, string address)
            : this(session, name, new Target() { Address = address }, null)
        {
        }

        /// <summary>
        /// Initializes a sender link.
        /// </summary>
        /// <param name="session">The session within which to create the link.</param>
        /// <param name="name">The link name.</param>
        /// <param name="target">The target on attach that specifies the message target.</param>
        /// <param name="onAttached">The callback to invoke when an attach is received from peer.</param>
        public SenderLink(Session session, string name, Target target, OnAttached onAttached)
            : this(session, name, new Attach() { Source = new Source(), Target = target }, onAttached)
        {
        }

        /// <summary>
        /// Initializes a sender link.
        /// </summary>
        /// <param name="session">The session within which to create the link.</param>
        /// <param name="name">The link name.</param>
        /// <param name="attach">The attach frame to send for this link.</param>
        /// <param name="onAttached">The callback to invoke when an attach is received from peer.</param>
        public SenderLink(Session session, string name, Attach attach, OnAttached onAttached)
            : base(session, name, onAttached)
        {
            this.settleMode = attach.SndSettleMode;
            this.outgoingList = new LinkedList();
            this.SendAttach(false, this.deliveryCount, attach);
        }

        /// <summary>
        /// Sends a message and synchronously waits for an acknowledgement. Throws
        /// TimeoutException if ack is not received in 60 seconds.
        /// </summary>
        /// <param name="message">The message to send.</param>
        public void Send(Message message)
        {
            this.SendSync(message, AmqpObject.DefaultTimeout);
        }

        /// <summary>
        /// Sends a message and synchronously waits for an acknowledgement. Throws
        /// TimeoutException if ack is not received in the specified time.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="timeout">The time to wait for the acknowledgement.</param>
        public void Send(Message message, TimeSpan timeout)
        {
            this.SendSync(message, (int)(timeout.Ticks / 10000));
        }

        void SendSync(Message message, int waitMilliseconds)
        {
            ManualResetEvent acked = new ManualResetEvent(false);
            Outcome outcome = null;
            OutcomeCallback callback = (l, m, o, s) =>
            {
                outcome = o;
                acked.Set();
            };

            this.SendInternal(message, this.GetTxnState(), callback, acked, true);

            bool signaled = acked.WaitOne(waitMilliseconds);
            if (!signaled)
            {
                this.OnTimeout(message);
                throw new TimeoutException(Fx.Format(SRAmqp.AmqpTimeout, "send", waitMilliseconds, "message"));
            }

            if (outcome != null)
            {
                if (outcome.Descriptor.Code == Codec.Released.Code)
                {
                    Released released = (Released)outcome;
                    throw new AmqpException(ErrorCode.MessageReleased, null);
                }
                else if (outcome.Descriptor.Code == Codec.Rejected.Code)
                {
                    Rejected rejected = (Rejected)outcome;
                    throw new AmqpException(rejected.Error);
                }
            }
        }

        /// <summary>
        /// Sends a message asynchronously. If callback is null, the message is sent without
        /// requesting for an acknowledgement (best effort).
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="callback">The callback to invoke when acknowledgement is received.</param>
        /// <param name="state">The object that is passed back to the outcome callback.</param>
        public void Send(Message message, OutcomeCallback callback, object state)
        {
            DeliveryState deliveryState = this.GetTxnState();
            this.SendInternal(message, deliveryState, callback, state, false);
        }

        /// <summary>
        /// Sends a message asynchronously. If callback is null, the message is sent without
        /// requesting for an acknowledgement (best effort). This method is not transaction aware. If you need transaction support,
        /// use <see cref="Send(Amqp.Message,Amqp.OutcomeCallback,object)"/>.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="deliveryState">The transactional state of the message. If null, no transaction is used.</param>
        /// <param name="callback">The callback to invoke when acknowledgement is received.</param>
        /// <param name="state">The object that is passed back to the outcome callback.</param>
        public void Send(Message message, DeliveryState deliveryState, OutcomeCallback callback, object state)
        {
            this.SendInternal(message, deliveryState, callback, state, false);
        }

        DeliveryState GetTxnState()
        {
#if NETFX || NETFX40 || NETSTANDARD2_0
            return Amqp.Transactions.ResourceManager.GetTransactionalStateAsync(this).Result;
#else
            return null;
#endif
        }

        void SendInternal(Message message, DeliveryState deliveryState, OutcomeCallback callback, object state, bool sync)
        {
            const int reservedBytes = 40;
#if NETFX || NETFX40 || DOTNET
            var buffer = message.Encode(this.Session.Connection.BufferManager, reservedBytes);
#else
            var buffer = message.Encode(reservedBytes);
#endif
            if (buffer.Length < 1)
            {
                throw new ArgumentException("Cannot send an empty message.");
            }

            Delivery delivery = new Delivery()
            {
                Message = message,
                Buffer = buffer,
                ReservedBufferSize = reservedBytes,
                State = deliveryState,
                Link = this,
                OnOutcome = callback,
                UserToken = state,
                Settled = this.settleMode == SenderSettleMode.Settled || callback == null,
                Batchable = !sync
            };

            IHandler handler = this.Session.Connection.Handler;
            if (handler != null && handler.CanHandle(EventId.SendDelivery))
            {
                handler.Handle(Event.Create(EventId.SendDelivery, this.Session.Connection, this.Session, this, context: delivery));
            }

            lock (this.ThisLock)
            {
                this.ThrowIfDetaching("Send");

                if (this.credit <= 0 || this.writing)
                {
                    this.outgoingList.Add(delivery);
                    return;
                }

                this.credit--;
                this.deliveryCount++;
                this.writing = true;
            }

            this.WriteDelivery(delivery);
        }

        void OnTimeout(Message message)
        {
            lock (this.ThisLock)
            {
                this.outgoingList.Remove(message.Delivery);
            }

            if (message.Delivery.BytesTransfered > 0)
            {
                this.Session.DisposeDelivery(false, message.Delivery, new Released(), true);
            }
        }

        internal override void OnFlow(Flow flow)
        {
            Delivery delivery = null;
            lock (this.ThisLock)
            {
                this.credit = (flow.DeliveryCount + flow.LinkCredit) - this.deliveryCount;
                if (this.writing || this.credit <= 0 || this.outgoingList.First == null)
                {
                    return;
                }

                delivery = (Delivery)this.outgoingList.First;
                this.outgoingList.Remove(delivery);
                this.credit--;
                this.deliveryCount++;
                this.writing = true;
            }

            this.WriteDelivery(delivery);
        }

        internal override void OnTransfer(Delivery delivery, Transfer transfer, ByteBuffer buffer)
        {
            throw new InvalidOperationException();
        }

        internal override void OnDeliveryStateChanged(Delivery delivery)
        {
            // some broker may not settle the message when sending dispositions
            if (!delivery.Settled)
            {
                this.Session.DisposeDelivery(false, delivery, delivery.State, true);
            }

            if (delivery.OnOutcome != null)
            {
                Outcome outcome = delivery.State as Outcome;
#if NETFX || NETFX40 || DOTNET
                if (delivery.State != null && delivery.State is Amqp.Transactions.TransactionalState)
                {
                    outcome = ((Amqp.Transactions.TransactionalState)delivery.State).Outcome;
                }
#endif
                delivery.OnOutcome(this, delivery.Message, outcome, delivery.UserToken);
            }
        }

        /// <summary>
        /// Closes the sender link.
        /// </summary>
        /// <param name="error">The error for the closure.</param>
        /// <returns></returns>
        protected override bool OnClose(Error error)
        {
            lock (this.ThisLock)
            {
                bool wait = this.writing || (this.credit == 0 && this.outgoingList.First != null);
                if (this.CloseCalled && wait && !this.Session.IsClosed)
                {
                    return false;
                }
            }

            this.OnAbort(error);
            return base.OnClose(error);
        }

        /// <summary>
        /// Aborts the sender link.
        /// </summary>
        /// <param name="error">The error for the abort.</param>
        protected override void OnAbort(Error error)
        {
            Delivery toRelease;
            lock (this.ThisLock)
            {
                toRelease = (Delivery)this.outgoingList.Clear();
            }

            Delivery.ReleaseAll(toRelease, error);
            Delivery.ReleaseAll(this.Session.RemoveDeliveries(this), error);
        }

        void WriteDelivery(Delivery delivery)
        {
            while (delivery != null)
            {
                delivery.Handle = this.Handle;
                if (delivery.Tag == null)
                {
                    lock (this.ThisLock)
                    {
                        delivery.Tag = Delivery.GetDeliveryTag(this.deliveryCount);
                    }
                }

                try
                {
                    bool settled = delivery.Settled;
                    this.Session.SendDelivery(delivery);
                    if (settled && delivery.OnOutcome != null)
                    {
                        delivery.OnOutcome(this, delivery.Message, new Accepted(), delivery.UserToken);
                    }
                }
                catch
                {
                    lock (this.ThisLock)
                    {
                        this.writing = false;
                    }

                    throw;
                }

                bool shouldClose = false;
                lock (this.ThisLock)
                {
                    delivery = (Delivery)this.outgoingList.First;
                    if (delivery == null || this.credit == 0)
                    {
                        shouldClose = this.CloseCalled;
                        delivery = null;
                        this.writing = false;
                    }
                    else if (this.credit > 0)
                    {
                        this.outgoingList.Remove(delivery);
                        this.credit--;
                        this.deliveryCount++;
                    }
                }

                if (shouldClose)
                {
                    Error error = this.Error;
                    this.OnAbort(error);
                    if (base.OnClose(error))
                    {
                        this.NotifyClosed(error);
                    }
                }
            }
        }
    }
}