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
    using System.Threading;
    using Amqp.Framing;
    using Amqp.Types;

    public delegate void OnAttached(Link link, Target target, Source source);

    public abstract class Link : AmqpObject
    {
        enum State
        {
            Start,
            AttachSent,
            AttachReceived,
            Attached,
            DetachPipe,
            DetachSent,
            DetachReceived,
            End
        }

        readonly Session session;
        readonly string name;
        readonly uint handle;
        readonly OnAttached onAttached;
        State state;

        protected Link(Session session, string name, OnAttached onAttached)
        {
            this.session = session;
            this.name = name;
            this.onAttached = onAttached;
            this.handle = session.AddLink(this);
            this.state = State.Start;
        }

        public string Name
        {
            get { return this.name; }
        }

        public uint Handle
        {
            get { return this.handle; }
        }

        protected Session Session
        {
            get { return this.session; }
        }

        protected object ThisLock
        {
            get { return this; }
        }

        internal void OnAttach(uint remoteHandle, Attach attach)
        {
            lock (this.ThisLock)
            {
                if (this.state == State.AttachSent)
                {
                    this.state = State.Attached;
                }
                else if (this.state == State.DetachPipe)
                {
                    this.state = State.DetachSent;
                }
                else
                {
                    throw new AmqpException(ErrorCode.IllegalState,
                        Fx.Format(SRAmqp.AmqpIllegalOperationState, "OnAttach", this.state));
                }

                this.HandleAttach(attach);
            }

            if (this.onAttached != null && attach.Target != null && attach.Source != null)
            {
                this.onAttached(this, (Target)attach.Target, (Source)attach.Source);
            }
        }

        internal bool OnDetach(Detach detach)
        {
            lock (this.ThisLock)
            {
                if (this.state == State.DetachSent)
                {
                    this.state = State.End;
                }
                else if (this.state == State.Attached)
                {
                    this.SendDetach();
                    this.state = State.End;
                }
                else
                {
                    throw new AmqpException(ErrorCode.IllegalState,
                        Fx.Format(SRAmqp.AmqpIllegalOperationState, "OnDetach", this.state));
                }

                this.OnClose(detach.Error);
                this.NotifyClosed(detach.Error);
                return true;
            }
        }

        internal abstract void OnFlow(Flow flow);

        internal abstract void OnTransfer(Delivery delivery, Transfer transfer, ByteBuffer buffer);

        internal abstract void HandleAttach(Attach attach);

        internal abstract void OnDeliveryStateChanged(Delivery delivery);

        protected override bool OnClose(Error error)
        {
            lock (this.ThisLock)
            {
                if (this.state == State.End)
                {
                    return true;
                }
                else if (this.state == State.AttachSent)
                {
                    this.state = State.DetachPipe;
                }
                else if (this.state == State.Attached)
                {
                    this.state = State.DetachSent;
                }
                else if (this.state == State.DetachReceived)
                {
                    this.state = State.End;
                }
                else
                {
                    throw new AmqpException(ErrorCode.IllegalState,
                        Fx.Format(SRAmqp.AmqpIllegalOperationState, "Close", this.state));
                }

                this.SendDetach();
                return this.state == State.End;
            }
        }

        protected void SendFlow(uint deliveryCount, uint credit)
        {
            Flow flow = new Flow() { Handle = this.handle, DeliveryCount = deliveryCount, LinkCredit = credit };
            this.session.SendFlow(flow);
        }

        protected void SendAttach(bool role, uint initialDeliveryCount, Target target, Source source)
        {
            Fx.Assert(this.state == State.Start, "state must be Start");
            this.state = State.AttachSent;
            Attach attach = new Attach()
            {
                LinkName = this.name,
                Handle = this.handle,
                Role = role,
                Source = source,
                Target = target
            };

            if (!role)
            {
                attach.InitialDeliveryCount = initialDeliveryCount;
            }

            this.session.SendCommand(attach);
        }

        protected void ThrowIfDetaching(string operation)
        {
            if (this.state >= State.DetachPipe)
            {
                throw new AmqpException(ErrorCode.IllegalState,
                    Fx.Format(SRAmqp.AmqpIllegalOperationState, operation, this.state));
            }
        }

        void SendDetach()
        {
            Detach detach = new Detach() { Handle = this.handle, Closed = true };
            this.session.SendCommand(detach);
        }
    }
}