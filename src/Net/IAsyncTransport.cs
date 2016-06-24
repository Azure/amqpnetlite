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
    using System.Collections.Generic;
    using System.Threading.Tasks;
   
    /// <summary>
    /// Provides asynchronous I/O calls.
    /// </summary>
    public interface IAsyncTransport : ITransport
    {
        /// <summary>
        /// Sets a connection to the transport.
        /// </summary>
        /// <param name="connection">The <see cref="Connection"/> to attach to the transport.</param>
        void SetConnection(Connection connection);

        /// <summary>
        /// Sends a list of buffers asynchronously.
        /// </summary>
        /// <param name="bufferList">The list of buffers to send.</param>
        /// <param name="listSize">Number of bytes of all buffers in bufferList.</param>
        /// <returns>A task for the send operation.</returns>
        Task SendAsync(IList<ByteBuffer> bufferList, int listSize);

        /// <summary>
        /// Reads bytes into a buffer asynchronously.
        /// </summary>
        /// <param name="buffer">The buffer to store data.</param>
        /// <param name="offset">The buffer offset where data starts.</param>
        /// <param name="count">The number of bytes to read.</param>
        /// <returns>A task for the receive operation. The result is the actual bytes
        /// read from the transport.</returns>
        Task<int> ReceiveAsync(byte[] buffer, int offset, int count);
    }
}
