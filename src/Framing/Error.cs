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
    /// Defines the details of an error.
    /// </summary>
    public sealed class Error : DescribedList
    {
        Symbol condition;
        string description;
        Fields info;

        /// <summary>
        /// Initializes an error object.
        /// </summary>
        [Obsolete]
        public Error()
            : this(ErrorCode.InternalError)
        {
        }

        /// <summary>
        /// Initializes an error object.
        /// </summary>
        /// <param name="condition">The error condition (<see cref="ErrorCode"/> for standard error conditions).</param>
        public Error(Symbol condition)
            : base(Codec.Error, 3)
        {
            this.Condition = condition;
        }

        /// <summary>
        /// Gets or sets a symbolic value indicating the error condition (index=0).
        /// </summary>
        public Symbol Condition
        {
            get { return this.GetField(0, this.condition); }
            set { this.SetField(0, ref this.condition, value); }
        }

        /// <summary>
        /// Gets or sets the descriptive text about the error condition (index=1).
        /// </summary>
        public string Description
        {
            get { return this.GetField(1, this.description); }
            set { this.SetField(1, ref this.description, value); }
        }

        /// <summary>
        /// Gets or sets the map carrying information about the error condition (index=2).
        /// </summary>
        public Fields Info
        {
            get { return this.GetField(2, this.info); }
            set { this.SetField(2, ref this.info, value); }
        }

        internal override void WriteField(ByteBuffer buffer, int index)
        {
            switch (index)
            {
                case 0:
                    Encoder.WriteSymbol(buffer, this.condition, true);
                    break;
                case 1:
                    Encoder.WriteString(buffer, this.description, true);
                    break;
                case 2:
                    Encoder.WriteMap(buffer, this.info, true);
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
                    this.condition = Encoder.ReadSymbol(buffer, formatCode);
                    break;
                case 1:
                    this.description = Encoder.ReadString(buffer, formatCode);
                    break;
                case 2:
                    this.info = Encoder.ReadFields(buffer, formatCode);
                    break;
                default:
                    Fx.Assert(false, "Invalid field index");
                    break;
            }
        }

#if TRACE
        /// <summary>
        /// Returns a string that represents the current error object.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.GetDebugString(
                "error",
                new object[] { "condition", "description", "fields" },
                new object[] { condition, description, info });
        }
#endif
    }
}