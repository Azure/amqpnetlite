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
    /// A sender link.
    /// </summary>
    public class Sender : Link
    {
        const int defaultTimeout = 60000;

        Client client;
        uint credit;
        uint deliveryCount;
        uint deliveryId;
        DescribedValue deliveryState;

        internal Sender(Client client, string name, string address)
        {
            this.Role = false;
            this.Name = name;
            this.client = client;
        }

        /// <summary>
        /// Sends a message.
        /// </summary>
        /// <param name="message">The message.</param>
        public void Send(Message message)
        {
            this.Send(message, defaultTimeout);
        }

        /// <summary>
        /// Sends a message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="timeout">The timeout in seconds.</param>
        public void Send(Message message, int timeout)
        {
            Fx.AssertAndThrow(ErrorCode.SenderSendInvalidState, this.State < 0xff);
            this.client.Wait(o => ((Sender)o).credit == 0, this, 60000);

            lock (this)
            {
                if (this.credit < uint.MaxValue)
                {
                    this.credit--;
                }
            }

            this.deliveryState = null;
            this.deliveryId = this.client.Send(this, message, timeout == 0);
            this.deliveryCount++;

            this.client.Wait(o => ((Sender)o).deliveryState == null, this, timeout);
            if (!object.Equals(this.deliveryState.Descriptor, 0x24ul))
            {
                throw new Exception(this.deliveryState.Value.ToString());
            }
        }

        /// <summary>
        /// Closes the sender.
        /// </summary>
        public void Close()
        {
            this.client.CloseLink(this);
        }

        internal override void OnAttach(List attach)
        {
        }

        internal override void OnFlow(List flow)
        {
            lock (this)
            {
                uint dc = flow[5] == null ? uint.MaxValue : (uint)flow[5];
                uint lc = flow[6] == null ? uint.MaxValue : (uint)flow[6];
                this.credit = lc < uint.MaxValue ? dc + lc - this.credit : lc;
            }
        }

        internal override void OnDisposition(uint first, uint last, DescribedValue deliveryState)
        {
            if (this.deliveryId >= first && this.deliveryId <= last)
            {
                this.deliveryState = deliveryState;
            }
        }
    }
}