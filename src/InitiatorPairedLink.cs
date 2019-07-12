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
using System.Threading.Tasks;
using Amqp.Framing;
using Amqp.Types;

namespace Amqp
{
    /// <summary>
    /// Client for initiating paired links
    /// </summary>
    public class InitiatorPairedLink : PairedLink
    {
        /// <summary>
        /// Creates a new paired link over an existing session
        /// </summary>
        /// <param name="session">Session</param>
        /// <param name="linkName">Name of the link pair, must be unique in the session</param>
        /// <param name="remoteAddress">Address of the remote node</param>
        public InitiatorPairedLink(Session session, string linkName, string remoteAddress) :
            this(session, linkName, new Target() { Address = remoteAddress }, new Source() { Address = remoteAddress })
        {

        }
        /// <summary>
        /// Creates a new paired link over an existing session
        /// </summary>
        /// <param name="session">Session</param>
        /// <param name="linkName">Name of the link pair, must be unique in the session</param>
        /// <param name="remoteTarget">Name of the remote target</param>
        /// <param name="remoteSource">Name of the remote source</param>
        public InitiatorPairedLink(Session session, string linkName, Target remoteTarget, Source remoteSource) :
            this(session, linkName, "ini-" + Guid.NewGuid().ToString(), remoteTarget, remoteSource)
        {

        }

        /// <summary>
        /// Creates a new paired link over an existing session
        /// </summary>
        /// <param name="session">Session</param>
        /// <param name="linkName">Name of the link pair, must be unique in the session</param>
        /// <param name="localNodeAddress">Address of the local node</param>
        /// <param name="remoteTarget">Name of the remote target</param>
        /// <param name="remoteSource">Name of the remote source</param>
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
        /// Starts the receiver side by granting credit
        /// </summary>
        /// <param name="credit">Link credit</param>
        /// <param name="onMessage">Optional message vallback</param>
        public void Start(int credit, MessageCallback onMessage = null)
        {
            this.Receiver.Start(credit, onMessage);
        }

        /// <summary>
        /// Closes both paired links
        /// </summary>
        public void Close()
        {
            this.Receiver.Close();
            this.Sender.Close();
        }

        /// <summary>
        /// Closes both paired links asynchronously
        /// </summary>
        /// <returns>Task</returns>
        public Task CloseAsync()
        {
            return Task.WhenAll(this.Receiver.CloseAsync(), this.Sender.CloseAsync());
        }
    }
}
