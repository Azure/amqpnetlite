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
    using System.Threading;
    using Amqp.Framing;
    using Amqp.Types;

    /// <summary>
    /// The listener link provides non-blocking methods that can be used by brokers/listener
    /// applications.
    /// </summary>
    public class ListenerLink : Link
    {
        bool role;
        object state;

        // caller can initialize the link for an endpoint, a sender or a receiver
        // based on its needs.

        // link endpoint
        LinkEndpoint linkEndpoint;

        // send
        Action<int, object> onCredit;
        Action<Message, DeliveryState, bool, object> onDispose;

        // receive
        SequenceNumber deliveryCount;
        uint credit;
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
        public void InitializeSender(Action<int, object> onCredit, Action<Message, DeliveryState, bool, object> onDispose, object state)
        {
            ThrowIfNotNull(this.linkEndpoint, "endpoint");
            ThrowIfNotNull(this.onCredit, "sender");
            ThrowIfNotNull(this.onDispose, "sender");
            this.onCredit = onCredit;
            this.onDispose = onDispose;
            this.state = state;
        }

        /// <summary>
        /// Sends a message. This call is non-blocking and it does not wait for acknowledgements.
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
            if (this.role)
            {
                throw new AmqpException(ErrorCode.NotAllowed, "Cannot send a message over a receiving link.");
            }

            Delivery delivery = new Delivery()
            {
                Handle = this.Handle,
                Message = message,
                Buffer = buffer ?? message.Encode(),
                Link = this,
                Settled = this.SettleOnSend,
                Tag = Delivery.GetDeliveryTag(this.deliveryCount)
            };

            this.Session.SendDelivery(delivery);
            this.deliveryCount++;
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
            if (delivery == null || delivery.Settled)
            {
                return;
            }

            this.Session.DisposeDelivery(this.role, delivery, deliveryState, settled);
        }

        /// <summary>
        /// Completes the link attach request. This should be called when the IContainer.AttachLink implementation returns false
        /// and the asynchrounous processing completes. 
        /// </summary>
        /// <param name="attach">The attach to send back.</param>
        /// <param name="error">The error, if any, for the link.</param>
        public void CompleteAttach(Attach attach, Error error)
        {
            if (error != null)
            {
                this.SendAttach(!attach.Role, attach.InitialDeliveryCount, new Attach() { Target = null, Source = null });
            }
            else
            {
                this.SendAttach(!attach.Role, attach.InitialDeliveryCount, new Attach() { Target = attach.Target, Source = attach.Source });
            }

            base.OnAttach(attach.Handle, attach);

            if (error != null)
            {
                this.Close(0, error);
            }
            else
            {
                if (this.credit > 0)
                {
                    this.SendFlow(this.deliveryCount, credit, false);
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

        internal void InitializeLinkEndpoint(LinkEndpoint linkEndpoint, uint credit)
        {
            ThrowIfNotNull(this.linkEndpoint, "endpoint");
            ThrowIfNotNull(this.onMessage, "receiver");
            ThrowIfNotNull(this.onCredit, "sender");
            ThrowIfNotNull(this.onDispose, "sender");
            this.credit = credit;
            this.autoRestore = true;
            this.linkEndpoint = linkEndpoint;
        }

        internal override void OnAttach(uint remoteHandle, Attach attach)
        {
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
                error = new Error() { Condition = ErrorCode.InternalError, Description = exception.Message };
            }

            this.CompleteAttach(attach, error);
        }

        internal override void OnFlow(Flow flow)
        {
            var theirLimit = (SequenceNumber)(flow.DeliveryCount + flow.LinkCredit);
            var myLimit = (SequenceNumber)((uint)this.deliveryCount + this.credit);
            int delta = theirLimit - myLimit;
            if (this.linkEndpoint != null)
            {
                this.linkEndpoint.OnFlow(new FlowContext(this, delta, flow.Properties));
            }
            else if (delta > 0 && this.onCredit != null)
            {
                this.onCredit(delta, this.state);
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
                if (this.linkEndpoint != null)
                {
                    this.linkEndpoint.OnLinkClosed(this, error);
                }                
            }
        }

        /// <summary>
        /// Aborts the link.
        /// </summary>
        /// <param name="error">The error.</param>
        protected override void OnAbort(Error error)
        {
            if (this.linkEndpoint != null)
            {
                this.linkEndpoint.OnLinkClosed(this, error);
            }
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
            if (this.onMessage != null)
            {
                this.onMessage(this, delivery.Message, delivery.State, this.state);
            }
            else if (this.linkEndpoint != null)
            {
                this.linkEndpoint.OnMessage(new MessageContext(this, delivery.Message));
            }
        }
    }
}
