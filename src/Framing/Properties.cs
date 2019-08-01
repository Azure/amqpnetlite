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
    public sealed class Properties : DescribedList
    {
        /// <summary>
        /// Initializes a properties section.
        /// </summary>
        public Properties()
            : base(Codec.Properties, 13)
        {
        }

        private object messageId;
        /// <summary>
        /// Gets or sets the message-id field.
        /// </summary>
        /// <remarks>
        /// The default message identifier type assumed by the library is string.
        /// If the application needs to process other types (ulong, uuid, or binary),
        /// it must use the Get/SetMessageId methods.
        /// </remarks>
        public string MessageId
        {
            get { return (string)this.messageId; }
            set { this.messageId = value; }
        }

        private byte[] userId;
        /// <summary>
        /// Gets or sets the user-id field.
        /// </summary>
        public byte[] UserId
        {
            get { return this.userId; }
            set { this.userId = value; }
        }

        private string to;
        /// <summary>
        /// Gets or sets the to field.
        /// </summary>
        public string To
        {
            get { return this.to; }
            set { this.to = value; }
        }

        private string subject;
        /// <summary>
        /// Gets or sets the subject field.
        /// </summary>
        public string Subject
        {
            get { return this.subject; }
            set { this.subject = value; }
        }

        private string replyTo;
        /// <summary>
        /// Gets or sets the reply-to field.
        /// </summary>
        public string ReplyTo
        {
            get { return this.replyTo; }
            set { this.replyTo = value; }
        }

        private object correlationId;
        /// <summary>
        /// Gets or sets the correlation-id field.
        /// </summary>
        /// <remarks>
        /// The default correlation identifier type assumed by the library is string.
        /// If the application needs to process other types (ulong, uuid, or binary),
        /// it must use the Get/SetCorrelationId methods.
        /// </remarks>
        public string CorrelationId
        {
            get { return (string)this.correlationId; }
            set { this.correlationId = value; }
        }

        private Symbol contentType;
        /// <summary>
        /// Gets or sets the content-type field.
        /// </summary>
        public Symbol ContentType
        {
            get { return this.contentType; }
            set { this.contentType = value; }
        }

        private Symbol contentEncoding;
        /// <summary>
        /// Gets or sets the content-encoding field.
        /// </summary>
        public Symbol ContentEncoding
        {
            get { return this.contentEncoding; }
            set { this.contentEncoding = value; }
        }

        private DateTime? absoluteExpiryTime;
        /// <summary>
        /// Gets or sets the absolute-expiry-time field.
        /// </summary>
        public DateTime AbsoluteExpiryTime
        {
            get { return this.absoluteExpiryTime == null ? DateTime.MinValue : this.absoluteExpiryTime.Value; }
            set { this.absoluteExpiryTime = value; }
        }

        private DateTime? creationTime;
        /// <summary>
        /// Gets or sets the creation-time field.
        /// </summary>
        public DateTime CreationTime
        {
            get { return this.creationTime == null ? DateTime.MinValue : this.creationTime.Value; }
            set { this.creationTime = value; }
        }

        private string groupId;
        /// <summary>
        /// Gets or sets the group-id field.
        /// </summary>
        public string GroupId
        {
            get { return this.groupId; }
            set { this.groupId = value; }
        }

        private uint? groupSequence;
        /// <summary>
        /// Gets or sets the group-sequence field.
        /// </summary>
        public uint GroupSequence
        {
            get { return this.groupSequence == null ? uint.MinValue : this.groupSequence.Value; }
            set { this.groupSequence = value; }
        }

        private string replyToGroupId;
        /// <summary>
        /// Gets or sets the reply-to-group-id field.
        /// </summary>
        public string ReplyToGroupId
        {
            get { return this.replyToGroupId; }
            set { this.replyToGroupId = value; }
        }

        internal override void OnDecode(ByteBuffer buffer, int count)
        {
            if (count-- > 0)
            {
                this.messageId = Encoder.ReadObject(buffer);
            }

            if (count-- > 0)
            {
                this.userId = Encoder.ReadBinary(buffer);
            }

            if (count-- > 0)
            {
                this.to = Encoder.ReadString(buffer);
            }

            if (count-- > 0)
            {
                this.subject = Encoder.ReadString(buffer);
            }

            if (count-- > 0)
            {
                this.replyTo = Encoder.ReadString(buffer);
            }

            if (count-- > 0)
            {
                this.correlationId = Encoder.ReadObject(buffer);
            }

            if (count-- > 0)
            {
                this.contentType = Encoder.ReadSymbol(buffer);
            }

            if (count-- > 0)
            {
                this.contentEncoding = Encoder.ReadSymbol(buffer);
            }

            if (count-- > 0)
            {
                this.absoluteExpiryTime = Encoder.ReadTimestamp(buffer);
            }

            if (count-- > 0)
            {
                this.creationTime = Encoder.ReadTimestamp(buffer);
            }

            if (count-- > 0)
            {
                this.groupId = Encoder.ReadString(buffer);
            }

            if (count-- > 0)
            {
                this.groupSequence = Encoder.ReadUInt(buffer);
            }

            if (count-- > 0)
            {
                this.replyToGroupId = Encoder.ReadString(buffer);
            }
        }

        internal override void OnEncode(ByteBuffer buffer)
        {
            Encoder.WriteObject(buffer, messageId, true);
            Encoder.WriteBinary(buffer, userId, true);
            Encoder.WriteString(buffer, to, true);
            Encoder.WriteString(buffer, subject, true);
            Encoder.WriteString(buffer, replyTo, true);
            Encoder.WriteObject(buffer, correlationId, true);
            Encoder.WriteSymbol(buffer, contentType, true);
            Encoder.WriteSymbol(buffer, contentEncoding, true);
            Encoder.WriteTimestamp(buffer, absoluteExpiryTime);
            Encoder.WriteTimestamp(buffer, creationTime);
            Encoder.WriteString(buffer, groupId, true);
            Encoder.WriteUInt(buffer, groupSequence, true);
            Encoder.WriteString(buffer, replyToGroupId, true);
        }

        /// <summary>
        /// Gets the message identifier.
        /// </summary>
        /// <returns>An object representing the message identifier. null if
        /// it is not set.</returns>
        public object GetMessageId()
        {
            return ValidateIdentifier(this.messageId);
        }

        /// <summary>
        /// Sets the message identifier. If not null, the object type must be
        /// string, Guid, ulong or byte[].
        /// </summary>
        /// <param name="id">The identifier object to set.</param>
        public void SetMessageId(object id)
        {
            this.messageId = ValidateIdentifier(id);
        }

        /// <summary>
        /// Gets the correlation identifier.
        /// </summary>
        /// <returns>An object representing the message identifier. null if
        /// it is not set.</returns>
        public object GetCorrelationId()
        {
            return ValidateIdentifier(this.correlationId);
        }

        /// <summary>
        /// Sets the correlation identifier. If not null, the object type must be
        /// string, Guid, ulong or byte[].
        /// </summary>
        /// <param name="id">The identifier object to set.</param>
        public void SetCorrelationId(object id)
        {
            this.correlationId = ValidateIdentifier(id);
        }

#if TRACE
        /// <summary>
        /// Returns a string that represents the current properties object.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.GetDebugString(
                "properties",
                new object[] { "message-id", "user-id", "to", "subject", "reply-to", "correlation-id", "content-type", "content-encoding", "absolute-expiry-time", "creation-time", "group-id", "group-sequence", "reply-to-group-id" },
                new object[] {messageId, userId, to, subject, replyTo, correlationId, contentType, contentEncoding, absoluteExpiryTime, creationTime, groupId, groupSequence, replyToGroupId });
        }
#endif

        static object ValidateIdentifier(object id)
        {
            if (id != null && !(id is string || id is ulong || id is Guid || id is byte[]))
            {
                throw new AmqpException(ErrorCode.NotAllowed, id.GetType().FullName);
            }

            return id;
        }
    }
}