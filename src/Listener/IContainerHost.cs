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
    /// <summary>
    /// Represents an AMQP container that hosts processors.
    /// </summary>
    public interface IContainerHost : IContainer
    {
        /// <summary>
        /// Opens the container host object.
        /// </summary>
        void Open();

        /// <summary>
        /// Closes the container host object.
        /// </summary>
        void Close();

        /// <summary>
        /// Registers a link processor to handle received attach performatives.
        /// </summary>
        /// <param name="linkProcessor">The link processor to be registered.</param>
        void RegisterLinkProcessor(ILinkProcessor linkProcessor);

        /// <summary>
        /// Registers a message processor to accept incoming messages from the specified address.
        /// When it is called, the container creates a node where the client can attach.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <param name="messageProcessor">The message processor to be registered.</param>
        void RegisterMessageProcessor(string address, IMessageProcessor messageProcessor);

        /// <summary>
        /// Registers a message source at the specified address where client receives messages.
        /// When it is called, the container creates a node where the client can attach.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <param name="messageSource">The message source to be registered.</param>
        void RegisterMessageSource(string address, IMessageSource messageSource);

        /// <summary>
        /// Registers a request processor from the specified address.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <param name="requestProcessor">The request processor to be registered.</param>
        /// <remarks>
        /// Client must create a pair of links (sending and receiving) at the address. The
        /// source.address on the sending link should contain an unique address in the client
        /// and it should be specified in target.address on the receiving link.
        /// </remarks>
        void RegisterRequestProcessor(string address, IRequestProcessor requestProcessor);

        /// <summary>
        /// Unregisters a message processor at the specified address.
        /// </summary>
        /// <param name="address">The address.</param>
        void UnregisterMessageProcessor(string address);

        /// <summary>
        /// Unregisters a message source at the specified address.
        /// </summary>
        /// <param name="address">The address.</param>
        void UnregisterMessageSource(string address);

        /// <summary>
        /// Unregisters a request processor at the specified address.
        /// </summary>
        /// <param name="address">The address.</param>
        void UnregisterRequestProcessor(string address);

        /// <summary>
        /// Unregisters a link processor that was previously registered.
        /// </summary>
        /// <param name="linkProcessor">The link processor to unregister.</param>
        /// <remarks>If the linkProcessor was not registered or is different
        /// from the current registered one, an exception is thrown.</remarks>
        void UnregisterLinkProcessor(ILinkProcessor linkProcessor);
    }
}
