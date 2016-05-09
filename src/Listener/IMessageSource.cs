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
    /// Defines the property and methods of a message source.
    /// </summary>
    public interface IMessageSource
    {
        /// <summary>
        /// Gets a message to send to the remote peer. The implementation may set
        /// a UserToken object so that internal state can be found by the context later.
        /// </summary>
        /// <returns>The receive context for the outgoing delivery.</returns>
        /// <remarks>
        /// Implementation must not throw exception. If a null context is returned and there
        /// are outstanding link credits, the library calls the method again.
        /// </remarks>
        Task<ReceiveContext> GetMessageAsync(ListenerLink link);

        /// <summary>
        /// Disposes the message by releasing any associated resources.
        /// This is called when a disposition frame is received from the peer.
        /// </summary>
        /// <param name="receiveContext">The ReceiveContext object returned by the GetMessageAsync method.</param>
        /// <param name="dispositionContext">The DispositionContext object containing the disposition information.</param>
        /// <remarks>
        /// The implementation typically performs the following actions in this event.
        /// (1) release any internal state associated with the message found by receiveContext.UserToken.
        /// (2) complete the delivery by calling dispositionContext.Complete().
        /// </remarks>
        void DisposeMessage(ReceiveContext receiveContext, DispositionContext dispositionContext);
    }
}
