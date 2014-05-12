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
    using Amqp.Framing;
    using Amqp.Types;
    using System.Threading;

    public delegate void MessageCallback(ReceiverLink receiver, Message message);

    public sealed class ReceiverLink : Link
    {
        // flow control
        SequenceNumber deliveryCount;
        int credit;

        // received messages queue
        LinkedList deliveryList;
        Delivery deliveryCurrent;

        // pending receivers
        LinkedList waiterList;
        MessageCallback onMessage;

        public ReceiverLink(Session session, string name, string adderss)
            : base(session, name)
        {
            this.deliveryList = new LinkedList();
            this.waiterList = new LinkedList();
            this.SendAttach(adderss);
        }

        public void Start(int credit, MessageCallback onMessage = null)
        {
            this.onMessage = onMessage;
            this.SetCredit(credit);
        }

        public void SetCredit(int credit)
        {
            uint dc;
            lock (this.ThisLock)
            {
                this.credit = credit;
                dc = this.deliveryCount;
            }

            this.SendFlow(dc, (uint)credit);
        }

        public Message Receive(int timeout = 60000)
        {
            Waiter waiter = null;

            lock (this.ThisLock)
            {
                Delivery first = (Delivery)this.deliveryList.First;
                if (first != null)
                {
                    this.deliveryList.Remove(first);
                    return first.Message;
                }

                if (timeout == 0)
                {
                    return null;
                }

                waiter = new Waiter();
                this.waiterList.Add(waiter);
            }

            return waiter.Wait(timeout);
        }

        public void Accept(Message message)
        {
            this.DisposeMessage(message, new Accepted());
        }

        public void Release(Message message)
        {
            this.DisposeMessage(message, new Released());
        }

        public void Reject(Message message, Error error = null)
        {
            this.DisposeMessage(message, new Rejected() { Error = error });
        }

        internal override void OnFlow(Flow flow)
        {
        }

        internal override void OnTransfer(Delivery delivery, Transfer transfer, ByteBuffer buffer)
        {
            if (!transfer.More)
            {
                Waiter waiter;
                MessageCallback callback;
                lock (this.ThisLock)
                {
                    if (delivery == null)
                    {
                        // multi-transfer delivery
                        delivery = this.deliveryCurrent;
                        this.deliveryCurrent = null;
                        Fx.Assert(delivery != null, "Must have a delivery in the queue");
                        AmqpBitConverter.WriteBytes(delivery.Buffer, buffer.Buffer, buffer.Offset, buffer.Length);
                        delivery.Message = new Message().Decode(delivery.Buffer);
                        delivery.Buffer = null;
                    }
                    else
                    {
                        // single tranfer delivery
                        this.OnDelivery(transfer.DeliveryId);
                        delivery.Message = new Message().Decode(buffer);
                    }

                    callback = this.onMessage;
                    waiter = (Waiter)this.waiterList.First;
                    if (waiter != null)
                    {
                        this.waiterList.Remove(waiter);
                    }

                    if (waiter == null && callback == null)
                    {
                        this.deliveryList.Add(delivery);
                    }
                }

                if (waiter != null)
                {
                    waiter.Signal(delivery.Message);
                }
                else if (callback != null)
                {
                    callback(this, delivery.Message);
                }
            }
            else
            {
                lock (this.ThisLock)
                {
                    if (delivery == null)
                    {
                        delivery = this.deliveryCurrent;
                        Fx.Assert(delivery != null, "Must have a current delivery");
                        AmqpBitConverter.WriteBytes(delivery.Buffer, buffer.Buffer, buffer.Offset, buffer.Length);
                    }
                    else
                    {
                        this.OnDelivery(transfer.DeliveryId);
                        delivery.Buffer = new ByteBuffer(buffer.Length * 2, true);
                        AmqpBitConverter.WriteBytes(delivery.Buffer, buffer.Buffer, buffer.Offset, buffer.Length);
                        this.deliveryCurrent = delivery;
                    }
                }
            }
        }

        internal override void UpdateAttach(Attach attach, string address)
        {
            ((Source)attach.Source).Address = address;
            attach.Role = true;
        }

        internal override void HandleAttach(Attach attach)
        {
            this.deliveryCount = attach.InitialDeliveryCount;
        }

        void DisposeMessage(Message message, Outcome outcome)
        {
            Delivery delivery = message.Delivery;
            if (delivery == null || delivery.Settled)
            {
                return;
            }

            this.Session.DisposeDelivery(true, delivery, outcome, true);
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

        sealed class Waiter : INode
        {
            readonly ManualResetEvent signal;
            Message message;
            bool expired;

            public INode Previous { get; set; }

            public INode Next { get; set; }

            public Waiter()
            {
                this.signal = new ManualResetEvent(false);
            }

            public Message Wait(int timeout)
            {
                this.signal.WaitOne(timeout, false);
                lock (this)
                {
                    this.expired = this.message == null;
                    return this.message;
                }
            }

            public void Signal(Message message)
            {
                lock (this)
                {
                    this.message = message;
                }

                this.signal.Set();
            }
        }
    }
}