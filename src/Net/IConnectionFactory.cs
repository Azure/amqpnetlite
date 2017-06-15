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
    using System.Threading.Tasks;

    /// <summary>
    /// The factory to create connections asynchronously.
    /// </summary>
    public interface IConnectionFactory
    {
        /// <summary>
        /// Creates a connection to the specified address.
        /// </summary>
        /// <param name="address">The address of a remote endpoint to connect to.</param>
        /// <returns>A task for the creation operation. On success, the result is an <see cref="IConnection"/></returns>
        Task<IConnection> CreateAsync(Address address);
    }

    public partial class ConnectionFactory : IConnectionFactory
    {
        async Task<IConnection> IConnectionFactory.CreateAsync(Address address)
        {
            return await this.CreateAsync(address, null, null);
        }
    }
}
