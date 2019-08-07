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
    /// <summary>
    /// Client side paired link holder
    /// </summary>
    public abstract class PairedLink
    {
        /// <summary>
        /// AMQP Session
        /// </summary>
        public Session Session { get; }
        /// <summary>
        /// Name of the links
        /// </summary>
        public string LinkName { get; }
        /// <summary>
        /// Receiving link
        /// </summary>
        public ReceiverLink Receiver { get; protected set; }
        /// <summary>
        /// Sending link
        /// </summary>
        public SenderLink Sender { get; protected set; }

        /// <summary>
        /// Creates a new paired link
        /// </summary>
        /// <param name="session">Session</param>
        /// <param name="linkName">Link name</param>
        public PairedLink(Session session, string linkName)
        {
            Session = session;
            LinkName = linkName;
        }
    }
}
