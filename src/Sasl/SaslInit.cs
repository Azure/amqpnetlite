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

        /// <summary>
        /// Gets or sets the selected security mechanism.
        /// </summary>
        public Symbol Mechanism
        {
            get { return (Symbol)this.Fields[0]; }
            set { this.Fields[0] = value; }
        }

        /// <summary>
        /// Gets or sets the initial security response data.
        /// </summary>
        public byte[] InitialResponse
        {
            get { return (byte[])this.Fields[1]; }
            set { this.Fields[1] = value; }
        }

        /// <summary>
        /// Gets or sets the name of the target host.
        /// </summary>
        public string HostName
        {
            get { return (string)this.Fields[2]; }
            set { this.Fields[2] = value; }
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
                new object[] { this.Fields[0], "...", this.Fields[2] });
        }
#endif
    }
}