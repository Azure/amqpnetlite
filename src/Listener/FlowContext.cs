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
    using Amqp.Types;

    /// <summary>
    /// Provides the context to a link endpoint to process a received flow.
    /// </summary>
    public class FlowContext
    {
        internal FlowContext(ListenerLink link, int messages, Fields properties)
        {
            this.Link = link;
            this.Messages = messages;
            this.Properties = properties;
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
        /// Gets the number of messages allowed to send by the peer.
        /// </summary>
        public int Messages
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the properties associated with the flow performative.
        /// </summary>
        public Fields Properties
        {
            get;
            private set;
        }
    }
}
