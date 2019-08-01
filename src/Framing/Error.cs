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

        private Symbol condition;
        /// <summary>
        /// Gets or sets a symbolic value indicating the error condition.
        /// </summary>
        public Symbol Condition
        {
            get { return this.condition; }
            set { this.condition = value; }
        }

        private string description;
        /// <summary>
        /// Gets or sets the descriptive text about the error condition.
        /// </summary>
        public string Description
        {
            get { return this.description; }
            set { this.description = value; }
        }

        private Fields info;
        /// <summary>
        /// Gets or sets the map carrying information about the error condition.
        /// </summary>
        public Fields Info
        {
            get { return this.info; }
            set { this.info = value; }
        }

        internal override void OnDecode(ByteBuffer buffer, int count)
        {
            if (count-- > 0)
            {
                this.condition = Encoder.ReadSymbol(buffer);
            }

            if (count-- > 0)
            {
                this.description = Encoder.ReadString(buffer);
            }

            if (count-- > 0)
            {
                this.info = Encoder.ReadFields(buffer);
            }
        }

        internal override void OnEncode(ByteBuffer buffer)
        {
            Encoder.WriteSymbol(buffer, condition, true);
            Encoder.WriteString(buffer, description, true);
            Encoder.WriteMap(buffer, info, true);
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