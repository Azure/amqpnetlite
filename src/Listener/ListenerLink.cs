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

namespace Amqp.Listener
{
    using System;
    using Amqp.Framing;
    using Amqp.Handler;
    using Amqp.Types;

    /// <summary>
    /// The listener link provides non-blocking methods that can be used by brokers/listener
    /// applications.
    /// </summary>
    public class ListenerLink : Link
    {
        bool role;
        object state;
        SequenceNumber deliveryCount;
        uint credit;

        // caller can initialize the link for an endpoint, a sender or a receiver
        // based on its needs.

        // link endpoint
        LinkEndpoint linkEndpoint;

        // send
        Action<int, Fields, object> onCredit;
        Action<Message, DeliveryState, bool, object> onDispose;

        // receive
        bool autoRestore;
        int restored;
        Delivery deliveryCurrent;
        Action<ListenerLink, Message, DeliveryState, object> onMessage;

        /// <summary>
        /// Initializes a listener link object.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="attach">The received attach frame.</param>
        public ListenerLink(ListenerSession session, Attach attach)
            : base(session, attach.LinkName, null)
        {
            this.role = !attach.Role;
            this.SettleOnSend = attach.SndSettleMode == SenderSettleMode.Settled;
        }

        /// <summary>
        /// Gets the sender (false) or receiver (true) role of the link.
        /// </summary>
        public bool Role
        {
            get { return this.role; }
        }

        /// <summary>
        /// Gets the settled flag. If it is true, messages are sent settled.
        /// </summary>
        public bool SettleOnSend
        {
            get; internal set;
        }

        /// <summary>
        /// Gets the user state attached to the link when it is initialized.
        /// </summary>
        public object State
        {
            get { return this.state; }
        }

        internal uint Credit
        {
            get { return this.credit; }
        }

        /// <summary>
        /// Initializes the receiver state for the link.
        /// </summary>
        /// <param name="credit">The link credit to send to the peer.</param>
        /// <param name="onMessage">The callback to be invoked for received messages.</param>
        /// <param name="state">The user state attached to the link.</param>
        public void InitializeReceiver(uint credit, Action<ListenerLink, Message, DeliveryState, object> onMessage, object state)
        {
            ThrowIfNotNull(this.linkEndpoint, "endpoint");
            ThrowIfNotNull(this.onMessage, "receiver");
            this.credit = credit;
            this.autoRestore = true;
            this.onMessage = onMessage;
            this.state = state;
        }

        /// <summary>
        /// Initializes the sender state for the link.
        /// </summary>
        /// <param name="onCredit">The callback to be invoked when delivery limit changes (by received flow performatives).</param>
        /// <param name="onDispose">The callback to be invoked when disposition is received.</param>
        /// <param name="state">The user state attached to the link.</param>
        public void InitializeSender(Action<int, Fields, object> onCredit, Action<Message, DeliveryState, bool, object> onDispose, object state)
        {
            ThrowIfNotNull(this.linkEndpoint, "endpoint");
            ThrowIfNotNull(this.onCredit, "sender");
            ThrowIfNotNull(this.onDispose, "sender");
            this.onCredit = onCredit;
            this.onDispose = onDispose;
            this.state = state;
        }

        /// <summary>
        /// Sends a message. This call is non-blocking and it does not wait for acknowledgments.
        /// </summary>
        /// <param name="message"></param>
        public void SendMessage(Message message)
        {
            this.SendMessage(message, null);
        }

        /// <summary>
        /// Sends a message with an optional buffer as the message payload.
        /// </summary>
        /// <param name="message">The message to be sent.</param>
        /// <param name="buffer">The serialized buffer of the message. It is null, the message is serialized.</param>
        public void SendMessage(Message message, ByteBuffer buffer)
        {
            this.SendMessageInternal(message, buffer, null);
        }

        /// <summary>
        /// Sends a disposition for the message.
        /// </summary>
        /// <param name="message">The message to be disposed (a disposition performative will be sent for this message).</param>
        /// <param name="deliveryState">The delivery state to set on disposition.</param>
        /// <param name="settled">The settled flag on disposition.</param>
        public void DisposeMessage(Message message, DeliveryState deliveryState, bool settled)
        {
            if (settled && this.autoRestore)
            {
                lock (this.ThisLock)
                {
                    if (this.restored++ >= this.credit / 2)
                    {
                        this.restored = 0;
                        this.SendFlow(this.deliveryCount, this.credit, false);
                    }
                }
            }

            Delivery delivery = message.Delivery;
            if (delivery == null || delivery.Settled || delivery.Link != this)
            {
                return;
            }

            this.Session.DisposeDelivery(this.role, delivery, deliveryState, settled);
        }

        /// <summary>
        /// Completes the link attach request. This should be called when the IContainer.AttachLink implementation returns false
        /// and the asynchronous processing completes. 
        /// </summary>
        /// <param name="attach">The attach to send back.</param>
        /// <param name="error">The error, if any, for the link.</param>
        public void CompleteAttach(Attach attach, Error error)
        {
            if (error != null)
            {
                this.SendAttach(this.role, attach.InitialDeliveryCount, new Attach() { Target = null, Source = null });
            }
            else
            {
                if (!this.role)
                {
                    this.deliveryCount = attach.InitialDeliveryCount;
                }

                this.SendAttach(this.role, attach.InitialDeliveryCount, attach);
            }

            base.OnAttach(attach.Handle, attach);

            if (error != null)
            {
                this.CloseInternal(0, error);
            }
            else
            {
                if (this.role)
                {
                    this.SendFlow(this.deliveryCount, this.credit, false);
                }
                else
                {
                    // flow could be processed while attach is in progress
                    if (this.credit > 0)
                    {
                        if (this.linkEndpoint != null)
                        {
                            this.linkEndpoint.OnFlow(new FlowContext(this, (int)this.credit, null));
                        }
                        else if (this.onCredit != null)
                        {
                            this.onCredit((int)this.credit, null, this.state);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Sets a credit on the link. A flow is sent to the peer to update link flow control state.
        /// </summary>
        /// <param name="credit">The new link credit.</param>
        /// <param name="drain">Sets the drain flag in the flow performative.</param>
        /// <param name="autoRestore">If true, link credit is auto-restored when a message is accepted/rejected
        /// by the caller. If false, caller is responsible for manage link credits.</param>
        public void SetCredit(int credit, bool drain, bool autoRestore = true)
        {
            this.ThrowIfDetaching("set-credit");
            lock (this.ThisLock)
            {
                this.credit = (uint)credit;
                this.autoRestore = autoRestore;
                this.restored = 0;
                this.SendFlow(this.deliveryCount, this.credit, drain);
            }
        }

        internal void SafeAddClosed(ClosedCallback callback)
        {
            this.Closed += callback;
            if (this.IsDetaching)
            {
                callback(this, this.Error);
            }
        }

        internal void InitializeLinkEndpoint(LinkEndpoint linkEndpoint, uint credit)
        {
            ThrowIfNotNull(this.linkEndpoint, "endpoint");
            ThrowIfNotNull(this.onMessage, "receiver");
            ThrowIfNotNull(this.onCredit, "sender");
            ThrowIfNotNull(this.onDispose, "sender");
            this.linkEndpoint = linkEndpoint;
            if (this.role)
            {
                this.credit = credit;
                this.autoRestore = true;
            }
        }

        internal uint SendMessageInternal(Message message, ByteBuffer buffer, object userToken)
        {
            if (this.role)
            {
                throw new AmqpException(ErrorCode.NotAllowed, "Cannot send a message over a receiving link.");
            }

            this.ThrowIfDetaching("Send");
            uint tag;
            uint remainingCredit;
            lock (this.ThisLock)
            {
                tag = this.deliveryCount++;
                remainingCredit = --this.credit;
            }

            try
            {
                Delivery delivery = new Delivery()
                {
                    Handle = this.Handle,
                    Message = message,
                    Buffer = buffer ?? message.Encode(),
                    Link = this,
                    Settled = this.SettleOnSend,
                    OnOutcome = (a, b, c, d) => b.Delivery.OnStateChange(c),
                    UserToken = userToken
                };

                IHandler handler = this.Session.Connection.Handler;
                if (handler != null && handler.CanHandle(EventId.SendDelivery))
                {
                    handler.Handle(Event.Create(EventId.SendDelivery, this.Session.Connection, this.Session, this, context: delivery));
                }

                if (delivery.Tag == null)
                {
                    delivery.Tag = Delivery.GetDeliveryTag(tag);
                }

                this.Session.SendDelivery(delivery);

                return remainingCredit;
            }
            catch
            {
                lock (this.ThisLock)
                {
                    this.credit++;
                    this.deliveryCount--;
                }

                throw;
            }
        }

        internal override void OnAttach(uint remoteHandle, Attach attach)
        {
            if (role)
            {
                this.deliveryCount = attach.InitialDeliveryCount;
            }

            var container = ((ListenerConnection)this.Session.Connection).Listener.Container;

            Error error = null;

            try
            {
                bool done = container.AttachLink((ListenerConnection)this.Session.Connection, (ListenerSession)this.Session, this, attach);
                if (!done)
                {
                    return;
                }
            }
            catch (AmqpException amqpException)
            {
                error = amqpException.Error;
            }
            catch (Exception exception)
            {
                Trace.WriteLine(TraceLevel.Error, "Exception occurred attaching link: {0}", exception);
                error = new Error(ErrorCode.InternalError) { Description = exception.Message };
            }

            this.CompleteAttach(attach, error);
        }

        internal override void OnFlow(Flow flow)
        {
            int delta = 0;
            lock (this.ThisLock)
            {
                if (!this.role)
                {
                    var theirLimit = (SequenceNumber)(flow.DeliveryCount + flow.LinkCredit);
                    var myLimit = this.deliveryCount + (SequenceNumber)this.credit;
                    delta = theirLimit - myLimit;
                    if (delta <= 0)
                    {
                        // peer reduced credit
                        this.credit = 0;
                    }
                    else
                    {
                        this.credit += (uint)delta;
                    }
                }
            }
 
            if (this.linkEndpoint != null)
            {
                this.linkEndpoint.OnFlow(new FlowContext(this, delta, flow.Properties));
            }
            else if (delta != 0 && this.onCredit != null)
            {
                this.onCredit(delta, flow.Properties, this.state);
            }
        }

        internal override void OnDeliveryStateChanged(Delivery delivery)
        {
            if (this.onDispose != null)
            {
                this.onDispose(delivery.Message, delivery.State, delivery.Settled, this.state);
            }
            else if (this.linkEndpoint != null)
            {
                this.linkEndpoint.OnDisposition(new DispositionContext(this, delivery.Message, delivery.State, delivery.Settled));
            }
        }

        internal override void OnTransfer(Delivery delivery, Transfer transfer, ByteBuffer buffer)
        {
            if (delivery != null)
            {
                buffer.AddReference();
                delivery.Buffer = buffer;
                this.deliveryCount++;
            }
            else
            {
                delivery = this.deliveryCurrent;
                AmqpBitConverter.WriteBytes(delivery.Buffer, buffer.Buffer, buffer.Offset, buffer.Length);
            }

            if (!transfer.More)
            {
                this.DeliverMessage(delivery);
            }
            else
            {
                this.deliveryCurrent = delivery;
            }
        }

        /// <summary>
        /// Closes the link.
        /// </summary>
        /// <param name="error">The error</param>
        /// <returns></returns>
        protected override bool OnClose(Error error)
        {
            try
            {
                return base.OnClose(error);
            }
            finally
            {
                this.PerformCleanup(error);
            }
        }

        /// <summary>
        /// Aborts the link.
        /// </summary>
        /// <param name="error">The error.</param>
        protected override void OnAbort(Error error)
        {
            this.PerformCleanup(error);
        }

        static void ThrowIfNotNull(object obj, string name)
        {
            if (obj != null)
            {
                throw new InvalidOperationException("The " + name + " has been already initialized for this link.");
            }
        }

        void DeliverMessage(Delivery delivery)
        {
            var container = ((ListenerConnection)this.Session.Connection).Listener.Container;
            delivery.Message = container.CreateMessage(delivery.Buffer);

            IHandler handler = this.Session.Connection.Handler;
            if (handler != null && handler.CanHandle(EventId.SendDelivery))
            {
                handler.Handle(Event.Create(EventId.ReceiveDelivery, this.Session.Connection, this.Session, this, context: delivery));
            }

            if (this.onMessage != null)
            {
                this.onMessage(this, delivery.Message, delivery.State, this.state);
            }
            else if (this.linkEndpoint != null)
            {
                this.linkEndpoint.OnMessage(new MessageContext(this, delivery.Message));
            }
        }

        void PerformCleanup(Error error)
        {
            // notify upper layers first so they can handle released deliveries correctly
            this.NotifyClosed(error);

            Delivery pending = this.Session.RemoveDeliveries(this);
            while (pending != null)
            {
                pending.State = new Released();
                pending.Settled = true;
                this.OnDeliveryStateChanged(pending);
                pending = (Delivery)pending.Next;
            }

            if (this.linkEndpoint != null)
            {
                this.linkEndpoint.OnLinkClosed(this, error);
            }
        }
    }
}
