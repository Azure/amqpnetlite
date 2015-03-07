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
    using Amqp.Types;

    public class ListenerLink : Link
    {
        bool role;
        uint credit;
        object state;

        // send
        Action<int, object> onCredit;
        Action<Message, DeliveryState, bool, object> onDispose;

        // receive
        SequenceNumber deliveryCount;
        int delivered;
        Delivery deliveryCurrent;
        Action<ListenerLink, Message, DeliveryState, object> onMessage;

        public ListenerLink(ListenerSession session, Attach attach)
            : base(session, attach.LinkName, null)
        {
            this.role = !attach.Role;
            this.SettleOnSend = attach.SndSettleMode == SenderSettleMode.Settled;
        }

        public bool Role
        {
            get { return this.role; }
        }

        public bool SettleOnSend
        {
            get; internal set;
        }

        public object State
        {
            get { return this.state; }
        }

        public void InitializeReceiver(uint credit, Action<ListenerLink, Message, DeliveryState, object> onMessage, object state)
        {
            this.credit = credit;
            this.onMessage = onMessage;
            this.state = state;
        }

        public void InitializeSender(Action<int, object> onCredit, Action<Message, DeliveryState, bool, object> onDispose, object state)
        {
            this.onCredit = onCredit;
            this.onDispose = onDispose;
            this.state = state;
        }

        public void SendMessage(Message message, ByteBuffer buffer)
        {
            Delivery delivery = new Delivery()
            {
                Handle = this.Handle,
                Message = message,
                Buffer = buffer,
                Link = this,
                Settled = this.SettleOnSend
            };

            this.Session.SendDelivery(delivery);
            this.deliveryCount++;
        }

        public void DisposeMessage(Message message, DeliveryState deliveryState, bool settled)
        {
            Delivery delivery = message.Delivery;
            if (delivery == null || delivery.Settled)
            {
                return;
            }

            this.Session.DisposeDelivery(this.role, delivery, deliveryState, settled);
        }

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
                    this.SendFlow(this.deliveryCount, credit);
                }
            }
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
            if (this.onCredit != null)
            {
                var theirLimit = (SequenceNumber)(flow.DeliveryCount + flow.LinkCredit);
                var myLimit = (SequenceNumber)((uint)this.deliveryCount + this.credit);
                int delta = theirLimit - myLimit;
                if (delta > 0)
                {
                    this.onCredit(delta, this.state);
                }
            }
        }

        internal override void OnDeliveryStateChanged(Delivery delivery)
        {
            if (this.onDispose != null)
            {
                this.onDispose(delivery.Message, delivery.State, delivery.Settled, this.state);
            }
        }

        internal override void OnTransfer(Delivery delivery, Transfer transfer, ByteBuffer buffer)
        {
            if (delivery != null)
            {
                this.deliveryCount++;
                if (!transfer.More)
                {
                    // single transfer message - the most common case
                    delivery.Buffer = buffer;
                    this.DeliverMessage(delivery);
                }
                else
                {
                    delivery.Buffer = new ByteBuffer(buffer.Length * 2, true);
                    AmqpBitConverter.WriteBytes(delivery.Buffer, buffer.Buffer, buffer.Offset, buffer.Length);
                    this.deliveryCurrent = delivery;
                }
            }
            else
            {
                delivery = this.deliveryCurrent;
                if (!transfer.More)
                {
                    AmqpBitConverter.WriteBytes(delivery.Buffer, buffer.Buffer, buffer.Offset, buffer.Length);
                    this.deliveryCurrent = null;
                    this.DeliverMessage(delivery);
                }
                else
                {
                    AmqpBitConverter.WriteBytes(delivery.Buffer, buffer.Buffer, buffer.Offset, buffer.Length);
                }
            }
        }

        protected override void OnAbort(Error error)
        {
        }

        void DeliverMessage(Delivery delivery)
        {
            var container = ((ListenerConnection)this.Session.Connection).Listener.Container;
            delivery.Message = container.CreateMessage(delivery.Buffer);
            this.onMessage(this, delivery.Message, delivery.State, this.state);
            if (this.delivered++ >= this.credit / 2)
            {
                this.SendFlow(this.deliveryCount, this.credit);
            }
        }
    }
}
