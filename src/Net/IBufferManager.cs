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

#if NETFX || NETFX40 || DOTNET
    /// <summary>
    /// The interface defines the methods to manage buffers.
    /// </summary>
    public
#endif
    interface IBufferManager
    {
        /// <summary>
        /// Takes a buffer from the buffer manager.
        /// </summary>
        /// <param name="bufferSize">the buffer size.</param>
        /// <returns>
        /// A segment of a byte array. The count should be the same, or larger
        /// than the requested bufferSize.
        /// </returns>
        ArraySegment<byte> TakeBuffer(int bufferSize);

        /// <summary>
        /// Returns a buffer to the buffer manager.
        /// </summary>
        /// <param name="buffer">The buffer to return.</param>
        void ReturnBuffer(ArraySegment<byte> buffer);
    }
}
