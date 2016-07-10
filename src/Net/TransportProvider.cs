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
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// The TransportProvider class provides transport implementation for given address schemes.
    /// </summary>
    public abstract class TransportProvider : IDisposable
    {
        /// <summary>
        /// Gets or sets the supported address schemes.
        /// </summary>
        public string[] AddressSchemes { get; protected set; }

        /// <summary>
        /// Creates a transport for the given address.
        /// </summary>
        /// <param name="address">The address to connect.</param>
        /// <returns>An IAsyncTransport object representing the transport.</returns>
        public abstract Task<IAsyncTransport> CreateAsync(Address address);

        /// <summary>
        /// Disposes the provider and release any associated resources.
        /// </summary>
        public virtual void Dispose()
        {
        }
    }
}
