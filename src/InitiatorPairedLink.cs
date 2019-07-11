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

using System;
using Amqp.Framing;
using Amqp.Types;

namespace Amqp
{
    /// <summary>
    /// 
    /// </summary>
    public class InitiatorPairedLink : PairedLink
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        /// <param name="linkName"></param>
        /// <param name="remoteAddress"></param>
        public InitiatorPairedLink(Session session, string linkName, string remoteAddress) :
            this(session, linkName, new Target() { Address = remoteAddress }, new Source() { Address = remoteAddress })
        {

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        /// <param name="linkName"></param>
        /// <param name="remoteTarget"></param>
        /// <param name="remoteSource"></param>
        public InitiatorPairedLink(Session session, string linkName, Target remoteTarget, Source remoteSource) :
            this(session, linkName, "ini-" + Guid.NewGuid().ToString(), remoteTarget, remoteSource)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        /// <param name="linkName"></param>
        /// <param name="localNodeAddress"></param>
        /// <param name="remoteTarget"></param>
        /// <param name="remoteSource"></param>
        public InitiatorPairedLink(Session session, string linkName, string localNodeAddress, Target remoteTarget, Source remoteSource) : base(session, linkName)
        {
            this.Sender = new SenderLink(session, linkName, new Attach()
            {
                Target = remoteTarget,
                Properties = new Fields
                {
                    { new Symbol("paired") , true }
                }
            }, null);
            this.Receiver = new ReceiverLink(session, linkName, new Attach()
            {
                Source = remoteSource,
                Target = new Target { Address = localNodeAddress },
                Properties = new Fields
                {
                    { new Symbol("paired") , true }
                }
            }, null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="credit"></param>
        public void Start(int credit)
        {
            this.Receiver.Start(credit);
        }
    }
}
