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

namespace Amqp.Framing
{
    using System;
    using Amqp.Types;

    /// <summary>
    /// A data section contains opaque binary data.
    /// </summary>
    public sealed class Data : RestrictedDescribed
    {
        byte[] binary;

        /// <summary>
        /// Initializes a Data object.
        /// </summary>
        public Data()
            : base(Codec.Data)
        {
        }

#if (NETFX || NETFX40 || DOTNET || NETFX35)
        /// <summary>
        /// Gets or sets the binary data in this section.
        /// </summary>
        public byte[] Binary
        {
            get
            {
                if (this.Buffer != null)
                {
                    byte[] temp = new byte[this.Buffer.Length];
                    Array.Copy(this.Buffer.Buffer, this.Buffer.Offset, temp, 0, temp.Length);
                    return temp;
                }

                return this.binary;
            }
            set
            {
                this.binary = value;
            }
        }

        /// <summary>
        /// Gets or sets the binary data contained in a buffer in this section.
        /// </summary>
        public ByteBuffer Buffer
        {
            get;
            set;
        }

        internal override void EncodeValue(ByteBuffer buffer)
        {
            if (this.Buffer != null)
            {
                Encoder.WriteBinaryBuffer(buffer, this.Buffer);
            }
            else
            {
                Encoder.WriteBinary(buffer, this.binary, true);
            }
        }

        internal override void DecodeValue(ByteBuffer buffer)
        {
            this.Buffer = Encoder.ReadBinaryBuffer(buffer);
        }
#else
        /// <summary>
        /// Gets or sets the binary data in this section.
        /// </summary>
        public byte[] Binary
        {
            get { return this.binary; }
            set { this.binary = value; }
        }

        internal override void EncodeValue(ByteBuffer buffer)
        {
            Encoder.WriteBinary(buffer, this.binary, true);
        }

        internal override void DecodeValue(ByteBuffer buffer)
        {
            this.binary = Encoder.ReadBinary(buffer, Encoder.ReadFormatCode(buffer));
        }
#endif
    }
}