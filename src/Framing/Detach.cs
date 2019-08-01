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
        /// <summary>
        /// Initializes a Detach object.
        /// </summary>
        public Detach()
            : base(Codec.Detach, 3)
        {
        }

        private uint? handle;
        /// <summary>
        /// Gets or sets the handle field.
        /// </summary>
        public uint Handle
        {
            get { return this.handle == null ? uint.MinValue : this.handle.Value; }
            set { this.handle = value; }
        }

        private bool? closed;
        /// <summary>
        /// Gets or sets the closed field.
        /// </summary>
        public bool Closed
        {
            get { return this.closed == null ? false : this.closed.Value; }
            set { this.closed = value; }
        }

        private Error error;
        /// <summary>
        /// Gets or sets the error field.
        /// </summary>
        public Error Error
        {
            get { return this.error; }
            set { this.error = value; }
        }

        internal override void OnDecode(ByteBuffer buffer, int count)
        {
            if (count-- > 0)
            {
                this.handle = Encoder.ReadUInt(buffer);
            }

            if (count-- > 0)
            {
                this.closed = Encoder.ReadBoolean(buffer);
            }

            if (count-- > 0)
            {
                this.error = (Error)Encoder.ReadObject(buffer);
            }
        }

        internal override void OnEncode(ByteBuffer buffer)
        {
            Encoder.WriteUInt(buffer, handle, true);
            Encoder.WriteBoolean(buffer, closed, true);
            Encoder.WriteObject(buffer, error, true);
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