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
    using Amqp.Types;

    /// <summary>
    /// The Detach class contains parameters to detach the link endpoint from the session.
    /// </summary>
    public sealed class Detach : DescribedList
    {
        uint handle;
        bool closed;
        Error error;

        /// <summary>
        /// Initializes a Detach object.
        /// </summary>
        public Detach()
            : base(Codec.Detach, 3)
        {
        }

        /// <summary>
        /// Gets or sets the handle field (index=0).
        /// </summary>
        public uint Handle
        {
            get { return this.GetField(0, this.handle, uint.MinValue); }
            set { this.SetField(0, ref this.handle, value); }
        }

        /// <summary>
        /// Gets or sets the closed field (index=1).
        /// </summary>
        public bool Closed
        {
            get { return this.GetField(1, this.closed, false); }
            set { this.SetField(1, ref this.closed, value); }
        }

        /// <summary>
        /// Gets or sets the error field (index=2).
        /// </summary>
        public Error Error
        {
            get { return this.GetField(2, this.error); }
            set { this.SetField(2, ref this.error, value); }
        }

        internal override void WriteField(ByteBuffer buffer, int index)
        {
            switch (index)
            {
                case 0:
                    Encoder.WriteUInt(buffer, this.handle, true);
                    break;
                case 1:
                    Encoder.WriteBoolean(buffer, this.closed, true);
                    break;
                case 2:
                    Encoder.WriteObject(buffer, this.error, true);
                    break;
                default:
                    Fx.Assert(false, "Invalid field index");
                    break;
            }
        }

        internal override void ReadField(ByteBuffer buffer, int index, byte formatCode)
        {
            switch (index)
            {
                case 0:
                    this.handle = Encoder.ReadUInt(buffer, formatCode);
                    break;
                case 1:
                    this.closed = Encoder.ReadBoolean(buffer, formatCode);
                    break;
                case 2:
                    this.error = (Error)Encoder.ReadObject(buffer, formatCode);
                    break;
                default:
                    Fx.Assert(false, "Invalid field index");
                    break;
            }
        }

        /// <summary>
        /// Returns a string that represents the current begin object.
        /// </summary>
        public override string ToString()
        {
#if TRACE
            return this.GetDebugString(
                "detach",
                new object[] { "handle", "closed", "error" },
                new object[] { handle, closed, error });
#else
            return base.ToString();
#endif
        }
    }
}