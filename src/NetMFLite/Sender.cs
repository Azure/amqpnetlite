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

    public class Sender
    {
        internal uint remoteHandle;
        Client client;
        byte state;
        uint credit;
        uint deliveryCount;
        DescribedValue deliveryState;

        internal Sender(Client client, string address)
        {
            this.state |= Client.AttachSent;
            this.remoteHandle = uint.MaxValue;
            this.client = client;
            this.client.SendAttach(Client.Name + "-" + this.GetType().Name, 0u, false, null, address);
        }

        public void Send(Message message)
        {
            Fx.AssertAndThrow(ErrorCode.SenderSendInvalidState, this.state > 0);
            this.client.Wait(o => ((Sender)o).credit == 0, this, 60000);

            lock (this)
            {
                if (this.credit < uint.MaxValue)
                {
                    this.credit--;
                }
            }

            this.deliveryState = null;
            this.client.Send(message, this.deliveryCount, false);
            this.deliveryCount++;

            this.client.Wait(o => ((Sender)o).deliveryState == null, this, 60000);
            if (!object.Equals(this.deliveryState.Descriptor, 0x24ul))
            {
                throw new Exception(this.deliveryState.Value.ToString());
            }
        }

        public void Close()
        {
            this.state |= Client.DetachSent;
            this.client.transport.WriteFrame(0, 0, 0x16, Client.Detach(0x00u));
            this.client.Wait(o => (((Sender)o).state & Client.DetachReceived) == 0, this, 60000);
            this.state = 0;
            this.client.sender = null;
            this.client.outWindow = 0;
            this.client.nextOutgoingId = 0;
        }

        internal void OnAttach(List attach)
        {
            this.remoteHandle = (uint)attach[1];
            this.state |= Client.AttachReceived;
        }

        internal void OnFlow(List flow)
        {
            lock (this)
            {
                uint dc = flow[5] == null ? uint.MaxValue : (uint)flow[5];
                uint lc = flow[6] == null ? uint.MaxValue : (uint)flow[6];
                this.credit = lc < uint.MaxValue ? dc + lc - this.credit : lc;
            }
        }

        internal void OnDisposition(List disposition)
        {
            this.deliveryState = disposition[4] as DescribedValue;
        }

        internal void OnDetach(List detach)
        {
            this.state |= Client.DetachReceived;
        }
    }
}