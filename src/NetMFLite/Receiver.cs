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
    using Amqp.Types;

    public delegate void OnMessage(Receiver receiver, Message message);

    public class Receiver
    {
        internal uint remoteHandle;
        Client client;
        OnMessage onMessage;
        byte state;
        uint credit;
        byte restored;
        byte flowThreshold;
        uint deliveryCount;
        bool deliveryReceived;
        uint lastDeliveryId;
        ByteBuffer messageBuffer;

        internal Receiver(Client client, string address)
        {
            this.client = client;
            this.remoteHandle = uint.MaxValue;
            this.lastDeliveryId = uint.MaxValue;
            this.state |= Client.AttachSent;
            this.client.transport.WriteFrame(0, 0, 0x12, Client.Attach(Client.Name + "-" + this.GetType().Name, 1u, true, address, null));
        }

        public void Start(uint credit, OnMessage onMessage)
        {
            Fx.AssertAndThrow(1000, this.state > 0);
            this.credit = credit;
            this.flowThreshold = this.credit > 512u ? byte.MaxValue : (byte)(this.credit / 2);
            this.onMessage = onMessage;
            this.client.SendFlow(1u, this.deliveryCount, credit);
        }

        public void Accept(Message message)
        {
            Fx.AssertAndThrow(1000, this.state > 0);
            if (!message.settled)
            {
                this.client.transport.WriteFrame(0, 0, 0x15ul, new List() { true, message.deliveryId, null, true, new DescribedValue(0x24ul, new List()) });
            }

            if (this.credit < uint.MaxValue)
            {
                lock (this)
                {
                    this.credit++;
                    if (++this.restored >= this.flowThreshold)
                    {
                        this.restored = 0;
                        this.client.SendFlow(1u, this.deliveryCount, this.credit);
                    }
                }
            }
        }

        public void Close()
        {
            this.state |= Client.DetachSent;
            this.client.transport.WriteFrame(0, 0, 0x16, Client.Detach(0x01u));
            this.client.Wait(o => (((Receiver)o).state & Client.DetachReceived) == 0, this, 60000);
            this.state = 0;
            this.client.receiver = null;
            this.client.inWindow = 0;
            this.client.nextIncomingId = 0;
        }

        internal void OnAttach(List attach)
        {
            this.deliveryCount = (uint)attach[9];
            this.remoteHandle = (uint)attach[1];
            this.state |= Client.AttachReceived;
        }

        internal void OnTransfer(List transfer, ByteBuffer payload)
        {
            for (int i = transfer.Count; i < 11; i++)
            {
                transfer.Add(null);
            }

            bool more = transfer[5] != null && true.Equals(transfer[5]);
            if (transfer[1] == null || (this.deliveryReceived && this.lastDeliveryId.Equals(transfer[1])))
            {
                AmqpBitConverter.WriteBytes(this.messageBuffer, payload.Buffer, payload.Offset, payload.Length);
            }
            else
            {
                lock (this)
                {
                    Fx.AssertAndThrow(1000, this.credit > 0);
                    this.deliveryCount++;
                    if (this.credit < uint.MaxValue)
                    {
                        this.credit--;
                    }
                }

                this.lastDeliveryId = (uint)transfer[1];
                this.deliveryReceived = true;
                if (this.messageBuffer == null)
                {
                    if (more)
                    {
                        this.messageBuffer = new ByteBuffer(payload.Length * 2, true);
                        AmqpBitConverter.WriteBytes(this.messageBuffer, payload.Buffer, payload.Offset, payload.Length);
                    }
                    else
                    {
                        this.messageBuffer = payload;
                    }
                }
            }

            if (!more)    // more
            {
                Message message = Message.Decode(this.messageBuffer);
                this.messageBuffer = null;
                message.deliveryId = this.lastDeliveryId;
                message.settled = transfer[4] != null && true.Equals(transfer[4]);
                this.onMessage(this, message);
            }
        }

        internal void OnDetach(List detach)
        {
            this.state |= Client.DetachReceived;
        }
    }
}