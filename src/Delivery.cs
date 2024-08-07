﻿//  ------------------------------------------------------------------------------------
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
    using Amqp.Handler;

    sealed class Delivery : IDelivery, INode
    {
        const byte InProgressMask = 0x01;
        Message message;
        byte flags;

        public ByteBuffer Buffer;

        public int ReservedBufferSize;

        public uint Handle;

        public byte[] Tag { get; set; }

        public SequenceNumber DeliveryId;

        public int BytesTransfered;

        public DeliveryState State { get; set; }

        public OutcomeCallback OnOutcome;

        public object UserToken { get; set; }

        public bool Settled { get; set; }

        public bool Batchable { get; set; }

        public Link Link;

        public INode Previous { get; set; }

        public INode Next { get; set; }

        public Message Message
        {
            get { return this.message; }
            set
            {
                if (value.Delivery != null)
                {
                    value.Delivery.message = null;
                }

                this.message = value;
                this.message.Delivery = this;
            }
        }

        public bool InProgress
        {
            get { return (this.flags & InProgressMask) > 0; }
            set
            {
                if (value)
                {
                    this.flags |= InProgressMask;
                }
                else
                {
                    this.flags &= byte.MaxValue ^ InProgressMask;
                }
            }
        }

        public static void ReleaseAll(Delivery delivery, Error error)
        {
            Outcome outcome;
            if (error == null)
            {
                outcome = new Released();
            }
            else
            {
                outcome = new Rejected() { Error = error };
            }

            while (delivery != null)
            {
                if (delivery.OnOutcome != null)
                {
                    delivery.OnOutcome(delivery.Link, delivery.Message, outcome, delivery.UserToken);
                }

                delivery.Buffer.ReleaseReference();
                delivery = (Delivery)delivery.Next;
            }
        }

        public static byte[] GetDeliveryTag(uint tag)
        {
            byte[] buffer = new byte[FixedWidth.UInt];
            AmqpBitConverter.WriteInt(buffer, 0, (int)tag);
            return buffer;
        }

        public void OnStateChange(DeliveryState state)
        {
            this.State = state;
            this.Link.OnDeliveryStateChanged(this);
        }

        public void Dispose()
        {
            this.Buffer.ReleaseReference();
            this.message = null;
        }
    }
}