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

    /// <summary>
    /// The delegate to be invoked when a message is received.
    /// </summary>
    /// <param name="receiver">The receiver.</param>
    /// <param name="message">The received message.</param>
    public delegate void OnMessage(Receiver receiver, Message message);

    /// <summary>
    /// A receiver link.
    /// </summary>
    public class Receiver : Link
    {
        Client client;
        OnMessage onMessage;
        uint credit;
        byte restored;
        byte flowThreshold;
        uint deliveryCount;
        bool deliveryReceived;
        uint lastDeliveryId;
        ByteBuffer messageBuffer;

        internal Receiver(Client client, string name, string address)
        {
            this.Role = true;
            this.Name = name;
            this.client = client;
            this.lastDeliveryId = uint.MaxValue;
        }

        /// <summary>
        /// Starts the receiver link.
        /// </summary>
        /// <param name="credit">The link credit.</param>
        /// <param name="onMessage">The message callback.</param>
        public void Start(uint credit, OnMessage onMessage)
        {
            Fx.AssertAndThrow(ErrorCode.ReceiverStartInvalidState, this.State < 0xff);
            this.credit = credit;
            this.flowThreshold = this.credit > 512u ? byte.MaxValue : (byte)(this.credit / 2);
            this.onMessage = onMessage;
            this.client.SendFlow(this.Handle, this.deliveryCount, credit);
        }

        /// <summary>
        /// Accepts a received message.
        /// </summary>
        /// <param name="message">The received message.</param>
        public void Accept(Message message)
        {
            Fx.AssertAndThrow(ErrorCode.ReceiverAcceptInvalidState, this.State < 0xff);
            if (!message.settled)
            {
                this.client.SendDisposition(true, message.deliveryId, true, new DescribedValue(0x24ul, new List()));
            }

            if (this.credit < uint.MaxValue)
            {
                lock (this)
                {
                    this.credit++;
                    if (++this.restored >= this.flowThreshold)
                    {
                        this.restored = 0;
                        this.client.SendFlow(this.Handle, this.deliveryCount, this.credit);
                    }
                }
            }
        }

        /// <summary>
        /// Closes the receiver.
        /// </summary>
        public void Close()
        {
            if (this.State < 0xff)
            {
                this.client.CloseLink(this);
            }
        }

        internal override void OnAttach(List attach)
        {
            this.deliveryCount = (uint)attach[9];
        }

        internal override void OnFlow(List fields)
        {
        }

        internal override void OnDisposition(uint first, uint last, DescribedValue state)
        {
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
                    Fx.AssertAndThrow(ErrorCode.InvalidCreditOnTransfer, this.credit > 0);
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
    }
}