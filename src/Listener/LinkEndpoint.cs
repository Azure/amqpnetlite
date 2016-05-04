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
    using Amqp.Framing;

    /// <summary>
    /// The base class of an AMQP link endpoint.
    /// </summary>
    /// <remarks>
    /// A link endpoint represents a target or a source terminus of a node.
    /// A target link endpoint is a message sink and is also called receiving
    /// link endpoint. A source link endpoint is a message source and is also
    /// called sending link endpoint in this document.
    /// </remarks>
    public abstract class LinkEndpoint
    {
        /// <summary>
        /// Processes a received message.
        /// </summary>
        /// <param name="messageContext">Context of the received message.</param>
        /// <remarks>
        /// A receiving endpoint must implement this method to process messages.
        /// The endpoint should call messageContext.Complete to finish processing
        /// the message. An Accepted outcome or a Rejected outcome with the
        /// specified error will be sent to the client.
        /// A sending endpoint never receives messages and should not override this
        /// method.
        /// </remarks>
        public virtual void OnMessage(MessageContext messageContext)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Processes a received flow performative.
        /// </summary>
        /// <param name="flowContext">Context of the received flow performative.</param>
        /// <remarks>
        /// A sending endpoint should send messages per the requested message count.
        /// A receiving endpoint may receive a flow if the sender wants to exchange flow
        /// state or send custom properties.
        /// </remarks>
        public abstract void OnFlow(FlowContext flowContext);

        /// <summary>
        /// Processes a received disposition performative.
        /// </summary>
        /// <param name="dispositionContext">Context of the received disposition performative.</param>
        /// <remarks>
        /// The endpoint should handle the delivery state and the message settlement.
        /// The delivery state indicates the outcome of the delivery on the remote peer.
        ///   * Accepted: message is accepted and should be retied (e.g. removed from the queue or
        ///               deleted from the storage).
        ///   * Released: message is no longer acquired and should be made available for redelivery.
        ///   * Rejected: message cannot be processed and required action (e.g. redelivery and
        ///               deadlettering) should be taken.
        ///   * Modified: similar to Released except message can be modified by the outcome.
        /// After the outcome is handled, the endpoint should call dispositionContext.Complete (with
        /// an error if any). If the delivery is not settled (as indicated by dispositionContext.Settled
        /// flag), an outcome is sent to the remote peer and the delivery is settled.
        /// </remarks>
        public abstract void OnDisposition(DispositionContext dispositionContext);

        /// <summary>
        /// The method is called when the link is closed. Derived classes may override this method to
        /// perform necessary cleanup work.
        /// </summary>
        /// <param name="link">The link that is being closed.</param>
        /// <param name="error">The error, if any, that causes the link to be closed.</param>
        public virtual void OnLinkClosed(ListenerLink link, Error error)
        {
        }
    }
}
