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
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    /// A BufferManager class manages pooled buffers (byte arrays) with different sizes.
    /// It provides an implementation of the <see cref="IBufferManager"/> interface.
    /// </summary>
    public class BufferManager : IBufferManager
    {
        int minBufferSize;
        int maxBufferSize;
        long maxMemorySize;
        long currentMemorySize;
        int indexOffset;
        List<Pool> bufferPools;

        /// <summary>
        /// Initializes a new instance of the BufferManager class.
        /// </summary>
        /// <param name="minBufferSize">The minimum size in bytes of pooled buffers.</param>
        /// <param name="maxBufferSize">The maximum size in bytes of pooled buffers.</param>
        /// <param name="maxMemorySize">The maximum total size in bytes of pooled buffers.</param>
        public BufferManager(int minBufferSize, int maxBufferSize, long maxMemorySize)
        {
            CheckBufferSize(minBufferSize, "minBufferSize");
            CheckBufferSize(maxBufferSize, "maxBufferSize");
            this.minBufferSize = minBufferSize;
            this.maxBufferSize = maxBufferSize;
            this.maxMemorySize = maxMemorySize;
            this.indexOffset = RoundToNextLog2(minBufferSize);
            this.bufferPools = new List<Pool>();

            int size = minBufferSize;
            while (size <= maxBufferSize)
            {
                this.bufferPools.Add(new Pool(size));
                size *= 2;
            }
        }

        ArraySegment<byte> IBufferManager.TakeBuffer(int bufferSize)
        {
            if (bufferSize > this.maxBufferSize)
            {
                return new ArraySegment<byte>(new byte[bufferSize]);
            }

            int index = bufferSize <= this.minBufferSize ? 0 : RoundToNextLog2(bufferSize) - this.indexOffset;
            Pool pool = this.bufferPools[index];
            byte[] buffer;
            if (pool.TryTake(bufferSize, out buffer))
            {
                Interlocked.Add(ref this.currentMemorySize, -buffer.Length);
            }
            else
            {
                buffer = new byte[pool.BufferSize];
            }

            return new ArraySegment<byte>(buffer);
        }

        void IBufferManager.ReturnBuffer(ArraySegment<byte> buffer)
        {
            int bufferSize = buffer.Array.Length;
            if (bufferSize > this.maxBufferSize)
            {
                return;
            }

            int index = bufferSize <= this.minBufferSize ? 0 : RoundToNextLog2(bufferSize) - this.indexOffset;
            Pool pool = this.bufferPools[index];
            if (bufferSize == pool.BufferSize && this.currentMemorySize < this.maxMemorySize)
            {
                Interlocked.Add(ref this.currentMemorySize, pool.BufferSize);
                pool.Return(buffer.Array);
            }
        }

        static void CheckBufferSize(int bufferSize, string name)
        {
            if (bufferSize <= 0 || !IsPowerOfTwo(bufferSize))
            {
                throw new ArgumentException(name);
            }
        }

        static bool IsPowerOfTwo(int number)
        {
            return (number & (number - 1)) == 0;
        }

        static readonly int[] MultiplyDeBruijnBitPosition = 
        {
            0, 1, 28, 2, 29, 14, 24, 3, 30, 22, 20, 15, 25, 17, 4, 8, 
            31, 27, 13, 23, 21, 19, 16, 7, 26, 12, 18, 6, 11, 5, 10, 9
        };

        static int RoundToNextLog2(int bufferSize)
        {
            uint v = (uint)bufferSize;
            if (!IsPowerOfTwo(bufferSize))
            {
                v--;
                v |= v >> 1;
                v |= v >> 2;
                v |= v >> 4;
                v |= v >> 8;
                v |= v >> 16;
                v++;
            }

            return MultiplyDeBruijnBitPosition[(v * 0x077CB531U) >> 27];
        }

        class Pool
        {
            ConcurrentQueue<byte[]> buffers;

            public Pool(int bufferSize)
            {
                this.BufferSize = bufferSize;
                this.buffers = new ConcurrentQueue<byte[]>();
            }

            public int BufferSize
            {
                get;
                private set;
            }

            public bool TryTake(int size, out byte[] buffer)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (this.buffers.TryDequeue(out buffer))
                    {
                        return true;
                    }
                }

                buffer = null;
                return false;
            }

            public void Return(byte[] buffer)
            {
                this.buffers.Enqueue(buffer);
            }
        }
    }
}
