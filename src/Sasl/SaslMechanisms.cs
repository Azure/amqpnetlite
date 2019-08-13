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
    /// Available SASL mechanisms advertised by the server.
    /// </summary>
    public class SaslMechanisms : DescribedList
    {
        object saslServerMechanisms;

        /// <summary>
        /// Initializes a SaslMechanisms object.
        /// </summary>
        public SaslMechanisms()
            : base(Codec.SaslMechanisms, 1)
        {
        }

        /// <summary>
        /// Gets or sets the available SASL mechanisms (index=0).
        /// </summary>
        public Symbol[] SaslServerMechanisms
        {
            get { return HasField(0) ? Codec.GetSymbolMultiple(ref this.saslServerMechanisms) : null; }
            set { this.SetField(0, ref this.saslServerMechanisms, value); }
        }

        internal override void WriteField(ByteBuffer buffer, int index)
        {
            switch (index)
            {
                case 0:
                    Encoder.WriteObject(buffer, this.saslServerMechanisms, true);
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
                    this.saslServerMechanisms = Encoder.ReadObject(buffer, formatCode);
                    break;
                default:
                    Fx.Assert(false, "Invalid field index");
                    break;
            }
        }
        
#if TRACE
        /// <summary>
        /// Returns a string that represents the current SASL mechanisms object.
        /// </summary>
        public override string ToString()
        {
            return this.GetDebugString(
                "sasl-mechanisms",
                new object[] { "sasl-server-mechanisms" },
                new object[] { saslServerMechanisms });
        }
#endif
    }
}