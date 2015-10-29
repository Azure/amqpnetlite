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
        const byte Created = 0;
        const byte AttachSent = 1;
        const byte AttachReceived = 2;
        const byte DetachSent = 4;
        const byte DetachReceived = 8;
        const byte Closed = 0xFF;

        Client client;
        byte state;
        uint credit;
        uint deliveryCount;
        DescribedValue deliveryState;

        internal Sender(Client client, string address)
        {
            this.client = client;
            this.client.transport.WriteFrame(0, 0, 0x12, Attach(Client.Name + "-" + this.GetType().Name, address));
        }

        public void Send(object message)
        {
            this.client.Wait(o => ((Sender)o).credit == 0, this, 60000);

            lock (this)
            {
                this.credit--;
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
            this.state |= DetachSent;
            this.client.transport.WriteFrame(0, 0, 0x12, Detach(0x00u));
            this.client.Wait(o => (((Sender)o).state & DetachReceived) == 0, this, 60000);
        }

        internal void OnAttach(List attach)
        {
            this.state |= AttachReceived;
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
            this.state |= DetachReceived;
        }

        static List Attach(string name, string address)
        {
            return new List() { name, 0u, false, null, null, new DescribedValue(0x28ul, new List()),
                new DescribedValue(0x29ul, new List() { address })};
        }

        static List Detach(uint handle)
        {
            return new List { handle, true };
        }
    }
}