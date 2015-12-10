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
    /// The Properties class defines the Immutable properties of the Message.
    /// </summary>
    public sealed class Properties
    {
        private List fields;

        /// <summary>
        /// Initializes a properties section with a <see cref="List"/> value/>.
        /// </summary>
        public Properties(List value)
        {
            this.fields = value;
        }

        /// <summary>
        /// Gets or sets the message-id field.
        /// </summary>
        public string MessageId
        {
            get { return (string)this.fields[0]; }
            set { this.fields[0] = value; }
        }

        /// <summary>
        /// Gets or sets the user-id field.
        /// </summary>
        public byte[] UserId
        {
            get { return (byte[])this.fields[1]; }
            set { this.fields[1] = value; }
        }

        /// <summary>
        /// Gets or sets the to field.
        /// </summary>
        public string To
        {
            get { return (string)this.fields[2]; }
            set { this.fields[2] = value; }
        }

        /// <summary>
        /// Gets or sets the subject field.
        /// </summary>
        public string Subject
        {
            get { return (string)this.fields[3]; }
            set { this.fields[3] = value; }
        }

        /// <summary>
        /// Gets or sets the reply-to field.
        /// </summary>
        public string ReplyTo
        {
            get { return (string)this.fields[4]; }
            set { this.fields[4] = value; }
        }

        /// <summary>
        /// Gets or sets the correlation-id field.
        /// </summary>
        public string CorrelationId
        {
            get { return (string)this.fields[5]; }
            set { this.fields[5] = value; }
        }

        /// <summary>
        /// Gets or sets the content-type field.
        /// </summary>
        public Symbol ContentType
        {
            get { return (Symbol)this.fields[6]; }
            set { this.fields[6] = value; }
        }

        /// <summary>
        /// Gets or sets the content-encoding field.
        /// </summary>
        public Symbol ContentEncoding
        {
            get { return (Symbol)this.fields[7]; }
            set { this.fields[7] = value; }
        }

        /// <summary>
        /// Gets or sets the absolute-expiry-time field.
        /// </summary>
        public DateTime AbsoluteExpiryTime
        {
            get { return this.fields[8] == null ? DateTime.MinValue : (DateTime)this.fields[8]; }
            set { this.fields[8] = value; }
        }

        /// <summary>
        /// Gets or sets the creation-time field.
        /// </summary>
        public DateTime CreationTime
        {
            get { return this.fields[9] == null ? DateTime.MinValue : (DateTime)this.fields[9]; }
            set { this.fields[9] = value; }
        }

        /// <summary>
        /// Gets or sets the group-id field.
        /// </summary>
        public string GroupId
        {
            get { return (string)this.fields[10]; }
            set { this.fields[10] = value; }
        }

        /// <summary>
        /// Gets or sets the group-sequence field.
        /// </summary>
        public uint GroupSequence
        {
            get { return this.fields[11] == null ? uint.MinValue : (uint)this.fields[11]; }
            set { this.fields[11] = value; }
        }

        /// <summary>
        /// Gets or sets the reply-to-group-id field.
        /// </summary>
        public string ReplyToGroupId
        {
            get { return (string)this.fields[12]; }
            set { this.fields[12] = value; }
        }
    }
}