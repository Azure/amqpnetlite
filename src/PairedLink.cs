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
    /// 
    /// </summary>
    public class PairedLink
    {
        /// <summary>
        /// 
        /// </summary>
        public Session Session { get; }
        /// <summary>
        /// 
        /// </summary>
        public string LinkName { get; }
        /// <summary>
        /// 
        /// </summary>
        public ReceiverLink Receiver { get; protected set; }
        /// <summary>
        /// 
        /// </summary>
        public SenderLink Sender { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        /// <param name="linkName"></param>
        public PairedLink(Session session, string linkName)
        {
            Session = session;
            LinkName = linkName;
        }
    }
}
