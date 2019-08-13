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
        object messageId;
        byte[] userId;
        string to;
        string subject;
        string replyTo;
        object correlationId;
        Symbol contentType;
        Symbol contentEncoding;
        DateTime absoluteExpiryTime;
        DateTime creationTime;
        string groupId;
        uint groupSequence;
        string replyToGroupId;

        /// <summary>
        /// Initializes a properties section.
        /// </summary>
        public Properties()
            : base(Codec.Properties, 13)
        {
        }

        /// <summary>
        /// Gets or sets the message-id field (index=0).
        /// </summary>
        /// <remarks>
        /// The default message identifier type assumed by the library is string.
        /// If the application needs to process other types (ulong, uuid, or binary),
        /// it must use the Get/SetMessageId methods.
        /// </remarks>
        public string MessageId
        {
            get { return (string)this.GetField(0, this.messageId); }
            set { this.SetField(0, ref this.messageId, value); }
        }

        /// <summary>
        /// Gets or sets the user-id field (index=1).
        /// </summary>
        public byte[] UserId
        {
            get { return this.GetField(1, this.userId); }
            set { this.SetField(1, ref this.userId, value); }
        }

        /// <summary>
        /// Gets or sets the to field (index=2).
        /// </summary>
        public string To
        {
            get { return this.GetField(2, this.to); }
            set { this.SetField(2, ref this.to, value); }
        }

        /// <summary>
        /// Gets or sets the subject field (index=3).
        /// </summary>
        public string Subject
        {
            get { return this.GetField(3, this.subject); }
            set { this.SetField(3, ref this.subject, value); }
        }

        /// <summary>
        /// Gets or sets the reply-to field (index=4).
        /// </summary>
        public string ReplyTo
        {
            get { return this.GetField(4, this.replyTo); }
            set { this.SetField(4, ref this.replyTo, value); }
        }

        /// <summary>
        /// Gets or sets the correlation-id field (index=5).
        /// </summary>
        /// <remarks>
        /// The default correlation identifier type assumed by the library is string.
        /// If the application needs to process other types (ulong, uuid, or binary),
        /// it must use the Get/SetCorrelationId methods.
        /// </remarks>
        public string CorrelationId
        {
            get { return (string)this.GetField(5, this.correlationId); }
            set { this.SetField(5, ref this.correlationId, value); }
        }

        /// <summary>
        /// Gets or sets the content-type field (index=6).
        /// </summary>
        public Symbol ContentType
        {
            get { return this.GetField(6, this.contentType); }
            set { this.SetField(6, ref this.contentType, value); }
        }

        /// <summary>
        /// Gets or sets the content-encoding field (index=7).
        /// </summary>
        public Symbol ContentEncoding
        {
            get { return this.GetField(7, this.contentEncoding); }
            set { this.SetField(7, ref this.contentEncoding, value); }
        }

        /// <summary>
        /// Gets or sets the absolute-expiry-time field (index=8).
        /// </summary>
        public DateTime AbsoluteExpiryTime
        {
            get { return this.GetField(8, this.absoluteExpiryTime, DateTime.MinValue); }
            set { this.SetField(8, ref this.absoluteExpiryTime, value); }
        }

        /// <summary>
        /// Gets or sets the creation-time field (index=9).
        /// </summary>
        public DateTime CreationTime
        {
            get { return this.GetField(9, this.creationTime, DateTime.MinValue); }
            set { this.SetField(9, ref this.creationTime, value); }
        }

        /// <summary>
        /// Gets or sets the group-id field (index=10).
        /// </summary>
        public string GroupId
        {
            get { return this.GetField(10, this.groupId); }
            set { this.SetField(10, ref this.groupId, value); }
        }

        /// <summary>
        /// Gets or sets the group-sequence field (index=11).
        /// </summary>
        public uint GroupSequence
        {
            get { return this.GetField(11, this.groupSequence, uint.MinValue); }
            set { this.SetField(11, ref this.groupSequence, value); }
        }

        /// <summary>
        /// Gets or sets the reply-to-group-id field (index=12).
        /// </summary>
        public string ReplyToGroupId
        {
            get { return this.GetField(12, this.replyToGroupId); }
            set { this.SetField(12, ref this.replyToGroupId, value); }
        }

        internal override void WriteField(ByteBuffer buffer, int index)
        {
            switch (index)
            {
                case 0:
                    Encoder.WriteObject(buffer, this.messageId);
                    break;
                case 1:
                    Encoder.WriteBinary(buffer, this.userId, true);
                    break;
                case 2:
                    Encoder.WriteString(buffer, this.to, true);
                    break;
                case 3:
                    Encoder.WriteString(buffer, this.subject, true);
                    break;
                case 4:
                    Encoder.WriteString(buffer, this.replyTo, true);
                    break;
                case 5:
                    Encoder.WriteObject(buffer, this.correlationId);
                    break;
                case 6:
                    Encoder.WriteSymbol(buffer, this.contentType, true);
                    break;
                case 7:
                    Encoder.WriteSymbol(buffer, this.contentEncoding, true);
                    break;
                case 8:
                    Encoder.WriteTimestamp(buffer, this.absoluteExpiryTime);
                    break;
                case 9:
                    Encoder.WriteTimestamp(buffer, this.creationTime);
                    break;
                case 10:
                    Encoder.WriteString(buffer, this.groupId, true);
                    break;
                case 11:
                    Encoder.WriteUInt(buffer, this.groupSequence, true);
                    break;
                case 12:
                    Encoder.WriteString(buffer, this.replyToGroupId, true);
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
                    this.messageId = Encoder.ReadObject(buffer, formatCode);
                    break;
                case 1:
                    this.userId = Encoder.ReadBinary(buffer, formatCode);
                    break;
                case 2:
                    this.to = Encoder.ReadString(buffer, formatCode);
                    break;
                case 3:
                    this.subject = Encoder.ReadString(buffer, formatCode);
                    break;
                case 4:
                    this.replyTo = Encoder.ReadString(buffer, formatCode);
                    break;
                case 5:
                    this.correlationId = Encoder.ReadObject(buffer, formatCode);
                    break;
                case 6:
                    this.contentType = Encoder.ReadSymbol(buffer, formatCode);
                    break;
                case 7:
                    this.contentEncoding = Encoder.ReadSymbol(buffer, formatCode);
                    break;
                case 8:
                    this.absoluteExpiryTime = Encoder.ReadTimestamp(buffer, formatCode);
                    break;
                case 9:
                    this.creationTime = Encoder.ReadTimestamp(buffer, formatCode);
                    break;
                case 10:
                    this.groupId = Encoder.ReadString(buffer, formatCode);
                    break;
                case 11:
                    this.groupSequence = Encoder.ReadUInt(buffer, formatCode);
                    break;
                case 12:
                    this.replyToGroupId = Encoder.ReadString(buffer, formatCode);
                    break;
                default:
                    Fx.Assert(false, "Invalid field index");
                    break;
            }
        }

        /// <summary>
        /// Gets the message identifier.
        /// </summary>
        /// <returns>An object representing the message identifier. null if
        /// it is not set.</returns>
        public object GetMessageId()
        {
            return this.GetField(0, ValidateIdentifier(this.messageId));
        }

        /// <summary>
        /// Sets the message identifier. If not null, the object type must be
        /// string, Guid, ulong or byte[].
        /// </summary>
        /// <param name="id">The identifier object to set.</param>
        public void SetMessageId(object id)
        {
            this.SetField(0, ref this.messageId, ValidateIdentifier(id));
        }

        /// <summary>
        /// Gets the correlation identifier.
        /// </summary>
        /// <returns>An object representing the message identifier. null if
        /// it is not set.</returns>
        public object GetCorrelationId()
        {
            return this.GetField(5, ValidateIdentifier(this.correlationId));
        }

        /// <summary>
        /// Sets the correlation identifier. If not null, the object type must be
        /// string, Guid, ulong or byte[].
        /// </summary>
        /// <param name="id">The identifier object to set.</param>
        public void SetCorrelationId(object id)
        {
            this.SetField(5, ref this.correlationId, ValidateIdentifier(id));
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