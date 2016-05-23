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
    using System.Threading.Tasks;
    using Amqp.Framing;

    /// <summary>
    /// The AMQP link endpoint for a message source. Use this class to attach a link
    /// endpoint to an IMessageSource for sending outgoing messages. This class manages
    /// the link credit (flow control).
    /// </summary>
    public class SourceLinkEndpoint : LinkEndpoint
    {
        readonly IMessageSource messageSource;
        readonly ListenerLink link;
        object syncRoot;
        bool receiving;

        /// <summary>
        /// Initializes a SourceLinkEndpoint object
        /// </summary>
        /// <param name="messageSource">The associated message source.</param>
        /// <param name="link">The listener link.</param>
        public SourceLinkEndpoint(IMessageSource messageSource, ListenerLink link)
        {
            this.syncRoot = new object();
            this.messageSource = messageSource;
            this.link = link;
        }

        /// <summary>
        /// Processes a received flow performative and start receiving from the
        /// message source if required.
        /// </summary>
        /// <param name="flowContext">Context of the received flow performative.</param>
        public override void OnFlow(FlowContext flowContext)
        {
            lock (this.syncRoot)
            {
                if (this.receiving || this.link.Credit == 0)
                {
                    return;
                }

                this.receiving = true;
            }

            Task.Factory.StartNew(o => ((SourceLinkEndpoint)o).ReceiveAsync(), this);
        }

        /// <summary>
        /// Notifies the message source the delivery state of an outgoing message.
        /// </summary>
        /// <param name="dispositionContext">Context of the received disposition performative.</param>
        public override void OnDisposition(DispositionContext dispositionContext)
        {
            var receiveContext = (ReceiveContext)dispositionContext.Message.Delivery.UserToken;
            this.messageSource.DisposeMessage(receiveContext, dispositionContext);
        }

        async Task ReceiveAsync()
        {
            while (!this.link.IsDetaching)
            {
                ReceiveContext receiveContext = await this.messageSource.GetMessageAsync(this.link);
                if (receiveContext != null)
                {
                    try
                    {
                        uint remaining = this.link.SendMessageInternal(receiveContext.Message, null, receiveContext);
                        if (remaining == 0)
                        {
                            lock (this.syncRoot)
                            {
                                // credits could be received right after the check
                                if (this.link.Credit == 0)
                                {
                                    this.receiving = false;
                                    break;
                                }
                            }
                        }
                    }
                    catch
                    {
                        this.messageSource.DisposeMessage(receiveContext,
                            new DispositionContext(this.link, receiveContext.Message, new Released(), true));
                        break;
                    }
                }
            }
        }
    }
}
