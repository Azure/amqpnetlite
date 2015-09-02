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
    using System;

    /// <summary>
    /// The base class of an AMQP link endpoint.
    /// </summary>
    public abstract class LinkEndpoint
    {
        /// <summary>
        /// Processes a received message. A receiving endpoint must implement this method to process messages.
        /// </summary>
        /// <param name="messageContext">Context of the received message.</param>
        public virtual void OnMessage(MessageContext messageContext)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Processes a received flow performative. A sending endpoint should send messages per the requested
        /// message count. A receiving endpoint may receive a flow if the sender may want to request for credit
        /// or send custom properties.
        /// </summary>
        /// <param name="flowContext">Context of the received flow performative.</param>
        public abstract void OnFlow(FlowContext flowContext);

        /// <summary>
        /// Processes a received disposition performative. The endpoint should check the delivery state and
        /// perform appropriate actions to the message.
        /// </summary>
        /// <param name="dispositionContext">Context of the received disposition performative.</param>
        public abstract void OnDisposition(DispositionContext dispositionContext);
    }
}
