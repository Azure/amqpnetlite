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

    class WrappedByteBuffer : ByteBuffer
    {
        readonly ByteBuffer buffer;

        public WrappedByteBuffer(ByteBuffer buffer, int offset, int length)
            : base(buffer.Buffer, offset, length, length, false)
        {
            buffer.AddReference();
            this.buffer = buffer;
        }

        internal override void DuplicateBuffer(int bufferSize, int dataSize, out byte[] buffer, out int offset, out int count)
        {
            throw new InvalidOperationException();
        }

        internal override void AddReference()
        {
            this.buffer.AddReference();
        }

        internal override void ReleaseReference()
        {
            this.buffer.ReleaseReference();
        }
    }
}
