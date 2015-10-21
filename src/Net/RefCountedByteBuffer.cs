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
    using System.Threading;

    class RefCountedByteBuffer : ByteBuffer
    {
        readonly IBufferManager bufferManager;
        int references;

        internal RefCountedByteBuffer(IBufferManager bufferManager, byte[] buffer, int offset, int count, int length)
            : base(buffer, offset, length, count, true)
        {
            this.bufferManager = bufferManager;
            this.references = 1;
        }

        internal IBufferManager BufferManager
        {
            get { return this.bufferManager; }
        }

        internal override void AddReference()
        {
            if (Interlocked.Increment(ref this.references) <= 1)
            {
                Interlocked.Decrement(ref this.references);
                throw new ObjectDisposedException(this.GetType().Name);
            }
        }

        internal override void ReleaseReference()
        {
            if (Interlocked.Decrement(ref this.references) == 0)
            {
                this.bufferManager.ReturnBuffer(this.ToArraySegment());
            }
        }

        internal override void DuplicateBuffer(int bufferSize, int dataSize, out byte[] buffer, out int offset, out int count)
        {
            var segment = this.bufferManager.TakeBuffer(bufferSize);
            buffer = segment.Array;
            offset = segment.Offset;
            count = segment.Count;
            System.Buffer.BlockCopy(this.Buffer, this.Start, segment.Array, segment.Offset, dataSize);
            this.bufferManager.ReturnBuffer(this.ToArraySegment());
        }
    }
}
