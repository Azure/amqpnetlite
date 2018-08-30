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
    /// The AMQP link endpoint for a message target. Use this class to manage links
    /// attached to an IMessageProcessor.
    /// </summary>
    public class TargetLinkEndpoint : LinkEndpoint
    {
        readonly IMessageProcessor messageProcessor;
        readonly ListenerLink link;

        /// <summary>
        /// Initializes a TargetLinkEndpoint object
        /// </summary>
        /// <param name="messageProcessor">The associated message processor.</param>
        /// <param name="link">The listener link.</param>
        public TargetLinkEndpoint(IMessageProcessor messageProcessor, ListenerLink link)
        {
            this.messageProcessor = messageProcessor;
            this.link = link;
        }

        /// <summary>
        /// Notifies the message processor to process a received message.
        /// </summary>
        /// <param name="messageContext">Context of the received message.</param>
        public override void OnMessage(MessageContext messageContext)
        {
            this.messageProcessor.Process(messageContext);
        }

        /// <summary>
        /// Processes a received flow performative.
        /// </summary>
        /// <param name="flowContext">Context of the received flow performative.</param>
        public override void OnFlow(FlowContext flowContext)
        {
        }

        /// <summary>
        /// Processes a received disposition performative.
        /// </summary>
        /// <param name="dispositionContext">Context of the received disposition performative.</param>
        public override void OnDisposition(DispositionContext dispositionContext)
        {
        }
    }
}
