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
    using Amqp.Framing;
    using Amqp.Types;

    public delegate void OutcomeCallback(Message message, Outcome outcome, object state);

    public sealed class SenderLink : Link
    {
        bool closeIssued;

        // flow control
        SequenceNumber deliveryCount;
        int credit;

        // outgoing queue
        LinkedList outgoingList;
        bool writing;

        public SenderLink(Session session, string name, string adderss)
            : base(session, name)
        {
            this.outgoingList = new LinkedList();
            this.SendAttach(adderss);
        }

        public void Send(Message message, OutcomeCallback callback, object state)
        {
            Delivery delivery = new Delivery()
            {
                Message = message,
                Buffer = message.Encode(),
                OnOutcome = callback,
                UserToken = state,
                Settled = callback == null
            };

            lock (this.ThisLock)
            {
                if (this.credit <= 0 || this.writing)
                {
                    this.outgoingList.Add(delivery);
                    return;
                }

                delivery.Tag = GetDeliveryTag(this.deliveryCount);
                this.credit--;
                this.deliveryCount++;
                this.writing = true;
            }

            this.WriteDelivery(delivery);
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
                delivery.Tag = GetDeliveryTag(this.deliveryCount);
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

        internal override void UpdateAttach(Attach attach, string address)
        {
            ((Target)attach.Target).Address = address;
            attach.Role = false;
            attach.InitialDeliveryCount = this.deliveryCount;
        }

        internal override void HandleAttach(Attach attach)
        {
        }

        protected override bool OnClose(Error error)
        {
            lock (this.ThisLock)
            {
                if (this.writing)
                {
                    this.closeIssued = true;
                    return false;
                }
            }

            return base.OnClose(error);
        }

        static byte[] GetDeliveryTag(uint tag)
        {
            byte[] buffer = new byte[FixedWidth.UInt];
            AmqpBitConverter.WriteInt(buffer, 0, (int)tag);
            return buffer;
        }

        void WriteDelivery(Delivery delivery)
        {
            while (delivery != null)
            {
                delivery.Handle = this.Handle;
                this.Session.SendDelivery(delivery);

                lock (this.ThisLock)
                {
                    delivery = (Delivery)this.outgoingList.First;
                    if (delivery == null)
                    {
                        this.writing = false;
                        if (this.closeIssued)
                        {
                            base.OnClose(null);
                        }
                    }
                    else if (this.credit > 0)
                    {
                        this.outgoingList.Remove(delivery);
                        delivery.Tag = GetDeliveryTag(this.deliveryCount);
                        this.credit--;
                        this.deliveryCount++;
                    }
                    else
                    {
                        delivery = null;
                        this.writing = false;
                    }
                }
            }
        }
    }
}