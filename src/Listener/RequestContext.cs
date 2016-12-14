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
    /// Provides the context to a request processor to process the received request.
    /// </summary>
    public class RequestContext : Context
    {
        readonly ListenerLink responseLink;

        internal RequestContext(ListenerLink requestLink, ListenerLink responseLink, Message request)
            : base(requestLink, request)
        {
            this.responseLink = responseLink;
        }

        /// <summary>
        /// Gets the response (sending) link associated with the request context.
        /// </summary>
        public ListenerLink ResponseLink
        {
            get { return this.responseLink; }
        }

        /// <summary>
        /// Completes the request and sends the response message to the peer.
        /// </summary>
        /// <param name="response">The response message to send.</param>
        public void Complete(Message response)
        {
            if (response.Properties == null)
            {
                response.Properties = new Properties();
            }

            response.Properties.CorrelationId = this.Message.Properties.MessageId;
            this.responseLink.SendMessage(response);
            this.Message.Dispose();
            this.State = ContextState.Completed;
        }

        internal override ContextState GetState()
        {
            return this.ResponseLink.IsDetaching ? ContextState.Aborted : base.GetState();
        }
    }
}
