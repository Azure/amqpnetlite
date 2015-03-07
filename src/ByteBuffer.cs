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
    using Amqp.Types;
    using System;

    public sealed class ByteBuffer
    {
        //
        // A buffer has start and end and two cursors (read/write)
        // All four are absolute values.
        //
        //   +---------+--------------+----------------+
        // start      read          write             end
        //
        // read - start: already consumed
        // write - read: Length (bytes to be consumed)
        // end - write: Size (free space to write)
        // end - start: Capacity
        //
        byte[] buffer;
        int start;
        int read;
        int write;
        int end;
        bool autoGrow;

        public ByteBuffer(byte[] buffer, int offset, int count, int capacity)
            : this(buffer, offset, count, capacity, false)
        {
        }

        public ByteBuffer(int size, bool autoGrow)
            : this(new byte[size], 0, 0, size, autoGrow)
        {
        }

        ByteBuffer(byte[] buffer, int offset, int count, int capacity, bool autoGrow)
        {
            this.buffer = buffer;
            this.start = offset;
            this.read = offset;
            this.write = offset + count;
            this.end = offset + capacity;
            this.autoGrow = autoGrow;
        }

        public byte[] Buffer
        {
            get { return this.buffer; }
        }

        public int Capacity
        {
            get { return this.end - this.start; }
        }

        public int Offset
        {
            get { return this.read; }
        }

        public int Size
        {
            get { return this.end - this.write; }
        }

        public int Length
        {
            get { return this.write - this.read; }
        }

        public int WritePos
        {
            get { return this.write; }
        }

        public void Validate(bool write, int dataSize)
        {
            bool valid = false;
            if (write)
            {
                if (this.Size < dataSize && this.autoGrow)
                {
                    int newSize = Math.Max(this.Capacity * 2, this.Capacity + dataSize);
                    byte[] newBuffer = new byte[newSize];
                    Array.Copy(this.buffer, this.start, newBuffer, 0, this.Length);
                    this.buffer = newBuffer;
                    int consumed = this.read - this.start;
                    int written = this.write - this.start;
                    this.start = 0;
                    this.read = consumed;
                    this.write = written;
                    this.end = newSize;
                }

                valid = this.Size >= dataSize;
            }
            else
            {
                valid = this.Length >= dataSize;
            }

            if (!valid)
            {
                throw new AmqpException(ErrorCode.DecodeError, "buffer too small");
            }
        }

        public void Append(int size)
        {
            Fx.Assert(size >= 0, "size must be positive.");
            Fx.Assert((this.write + size) <= this.end, "Append size too large.");
            this.write += size;
        }

        public void Complete(int size)
        {
            Fx.Assert(size >= 0, "size must be positive.");
            Fx.Assert((this.read + size) <= this.write, "Complete size too large.");
            this.read += size;
        }

        public void Seek(int seekPosition)
        {
            Fx.Assert(seekPosition >= 0, "seekPosition must not be negative.");
            Fx.Assert((this.start + seekPosition) <= this.write, "seekPosition too large.");
            this.read = this.start + seekPosition;
        }

        public void Shrink(int size)
        {
            Fx.Assert(size >= 0 && size <= this.Length, "size must be positive and not greater then length.");
            this.write -= size;
        }

        public void Reset()
        {
            this.read = this.start;
            this.write = this.start;
        }

        public void AdjustPosition(int offset, int length)
        {
            Fx.Assert(offset >= this.start, "Invalid offset!");
            Fx.Assert(offset + length <= this.end, "length too large!");
            this.read = offset;
            this.write = this.read + length;
        }
    }
}
