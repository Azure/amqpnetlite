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

namespace Amqp.Sasl
{
    using Amqp.Framing;
    using Amqp.Types;

    /// <summary>
    /// SaslInit selects the mechanism and provides the initial response if needed.
    /// </summary>
    public class SaslInit : DescribedList
    {
        /// <summary>
        /// Initializes a SaslInit object.
        /// </summary>
        public SaslInit()
            : base(Codec.SaslInit, 3)
        {
        }

        private Symbol mechanism;
        /// <summary>
        /// Gets or sets the selected security mechanism.
        /// </summary>
        public Symbol Mechanism
        {
            get { return this.mechanism; }
            set { this.mechanism = value; }
        }

        private byte[] initialResponse;
        /// <summary>
        /// Gets or sets the initial security response data.
        /// </summary>
        public byte[] InitialResponse
        {
            get { return this.initialResponse; }
            set { this.initialResponse = value; }
        }

        private string hostName;
        /// <summary>
        /// Gets or sets the name of the target host.
        /// </summary>
        public string HostName
        {
            get { return this.hostName; }
            set { this.hostName = value; }
        }

        internal override void OnDecode(ByteBuffer buffer, int count)
        {
            if (count-- > 0)
            {
                this.mechanism = Encoder.ReadSymbol(buffer);
            }

            if (count-- > 0)
            {
                this.initialResponse = Encoder.ReadBinary(buffer);
            }

            if (count-- > 0)
            {
                this.hostName = Encoder.ReadString(buffer);
            }
        }

        internal override void OnEncode(ByteBuffer buffer)
        {
            Encoder.WriteSymbol(buffer, mechanism, true);
            Encoder.WriteBinary(buffer, initialResponse, true);
            Encoder.WriteString(buffer, hostName, true);
        }

#if TRACE
        /// <summary>
        /// Returns a string that represents the current SASL init object.
        /// </summary>
        public override string ToString()
        {
            return this.GetDebugString(
                "sasl-init",
                new object[] { "mechanism", "initial-response", "hostname" },
                new object[] { mechanism, "...", hostName });
        }
#endif
    }
}