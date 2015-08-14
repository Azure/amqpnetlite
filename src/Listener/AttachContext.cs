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
    /// Provides the context to an attach processor to process the received performative.
    /// </summary>
    public class AttachContext
    {
        internal AttachContext(ListenerLink link, Attach attach)
        {
            this.Link = link;
            this.Attach = attach;
        }

        /// <summary>
        /// Gets the link associated with the context.
        /// </summary>
        public ListenerLink Link
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the attach performative associated with the context.
        /// </summary>
        public Attach Attach
        {
            get;
            private set;
        }

        /// <summary>
        /// Completes the processing of the attach performative with success.
        /// </summary>
        /// <param name="linkEndpoint">The attached link endpoint.</param>
        /// <param name="initialCredit">The initial credit to send to peer for a receiving link endpoint. It is ignored for a sending endpoint.</param>
        public void Complete(LinkEndpoint linkEndpoint, int initialCredit)
        {
            this.Link.InitializeLinkEndpoint(linkEndpoint, (uint)initialCredit);
            this.Link.CompleteAttach(this.Attach, null);
        }

        /// <summary>
        /// Completes the processing of the attach performative with an error.
        /// </summary>
        /// <param name="error">The error to be sent to the remote peer.</param>
        public void Complete(Error error)
        {
            this.Link.CompleteAttach(this.Attach, error);
        }
    }
}
