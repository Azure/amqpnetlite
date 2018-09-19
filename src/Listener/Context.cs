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
    using Amqp.Framing;

    /// <summary>
    /// The base class of request context.
    /// </summary>
    public abstract class Context
    {
        internal static Accepted Accepted = new Accepted();
        ContextState state;

        /// <summary>
        /// Initializes a context object.
        /// </summary>
        /// <param name="link">The link where the message was received.</param>
        /// <param name="message">The received message.</param>
        protected Context(ListenerLink link, Message message)
        {
            this.Link = link;
            this.Message = message;
            this.state = ContextState.Active;
        }

        /// <summary>
        /// Gets the receiving link associated with the context.
        /// </summary>
        public ListenerLink Link
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the messages associated with the context.
        /// </summary>
        public Message Message
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the state of the context.
        /// </summary>
        public ContextState State
        {
            get
            {
                return this.GetState();
            }

            internal set
            {
                this.state = value;
            }
        }

        /// <summary>
        /// Disposes the request. If required, a disposition frame is sent to
        /// the peer to acknowledge the message.
        /// </summary>
        /// <param name="deliveryState">The delivery state to send.</param>
        protected void Dispose(DeliveryState deliveryState)
        {
            this.Link.DisposeMessage(this.Message, deliveryState, true);
            this.Message.Dispose();
            this.state = ContextState.Completed;
        }

        internal virtual ContextState GetState()
        {
            return this.Link.IsDetaching ? ContextState.Aborted : this.state;
        }
    }
}
