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

namespace Amqp
{
    using System;
    using Amqp.Framing;
    using Amqp.Types;

    /// <summary>
    /// The Message class represents an AMQP message.
    /// </summary>
    public class Message : IDisposable
    {
        /// <summary>
        /// The header section.
        /// </summary>
        public Header Header;

        /// <summary>
        /// The delivery annotation section.
        /// </summary>
        public DeliveryAnnotations DeliveryAnnotations;

        /// <summary>
        /// The message annotation section.
        /// </summary>
        public MessageAnnotations MessageAnnotations;

        /// <summary>
        /// The properties section.
        /// </summary>
        public Properties Properties;

        /// <summary>
        /// The application properties section.
        /// </summary>
        public ApplicationProperties ApplicationProperties;

        /// <summary>
        /// The body section. It can be one of the following,
        /// * <see cref="AmqpValue"/>
        /// * <see cref="AmqpSequence"/>
        /// * <see cref="Data"/>
        /// * <see cref="DataList"/>
        /// </summary>
        public RestrictedDescribed BodySection;

        /// <summary>
        /// The footer section.
        /// </summary>
        public Footer Footer;

        /// <summary>
        /// Initializes an empty message.
        /// </summary>
        public Message()
        {
        }

        /// <summary>
        /// Initializes a message from an object as the body. The object is wrapped
        /// in an <see cref="AmqpValue"/> section. To control the body section type,
        /// create an empty message and set <see cref="Message.BodySection"/> to either
        /// <see cref="AmqpValue"/>, <see cref="Data"/> or <see cref="AmqpSequence"/>.
        /// </summary>
        /// <param name="body">the object stored in the AmqpValue section.</param>
        public Message(object body)
        {
#if (NETFX || NETFX40 || NETFX35)
            this.BodySection = new AmqpValue<object>(body);
#else
            this.BodySection = new AmqpValue() { Value = body };
#endif
        }

        /// <summary>
        /// Gets the object from the body. The returned value depends on the type of the body section.
        /// Use the BodySection field if the entire section is needed.
        /// </summary>
        /// <remarks>Returns null if body section is null; otherwise one of the following,
        /// * A value contained in a <see cref="AmqpValue"/> section.
        /// * A list of objects contained in a <see cref="AmqpSequence"/> section.
        /// * A byte[] object contained in a signle <see cref="Data"/> section.
        /// * A Data[] representing multiple <see cref="Data"/> sections.
        /// </remarks>
        public object Body
        {
            get
            {
                if (this.BodySection == null)
                {
                    return null;
                }
                else if (this.BodySection.Descriptor.Code == Codec.AmqpValue.Code)
                {
                    return ((AmqpValue)this.BodySection).Value;
                }
                else if (this.BodySection.Descriptor.Code == Codec.Data.Code)
                {
                    var dataList = this.BodySection as DataList;
                    if (dataList != null)
                    {
                        return dataList.ToArray();
                    }

                    return ((Data)this.BodySection).Binary;
                }
                else if (this.BodySection.Descriptor.Code == Codec.AmqpSequence.Code)
                {
                    return ((AmqpSequence)this.BodySection).List;
                }
                else
                {
                    throw new AmqpException(ErrorCode.DecodeError, "The body section is invalid.");
                }
            }
        }

        /// <summary>
        /// Gets or sets the format of the message. Warning: setting a non-zero value may cause
        /// inter-operability issues with other standard 1.0 implementations.
        /// </summary>
        /// <remarks>The custom format MUST use the same encoding layout as the standard message.
        /// </remarks>
        public uint Format
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the delivery tag associated with the message.
        /// </summary>
        public byte[] DeliveryTag
        {
            get
            {
                return this.Delivery != null ? this.Delivery.Tag : null;
            }
        }

        internal Delivery Delivery
        {
            get;
            set;
        }

        /// <summary>
        /// Encodes the message into a buffer.
        /// </summary>
        /// <returns>The buffer.</returns>
        public ByteBuffer Encode()
        {
            return this.Encode(0);
        }

        /// <summary>
        /// Decodes a message from a buffer and advance the buffer read cursor.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <returns></returns>
        public static Message Decode(ByteBuffer buffer)
        {
            Message message = new Message();

            DataList dataList = null;
            while (buffer.Length > 0)
            {
                var described = (RestrictedDescribed)Codec.Decode(buffer);
                if (described.Descriptor.Code == Codec.Header.Code)
                {
                    message.Header = (Header)described;
                }
                else if (described.Descriptor.Code == Codec.DeliveryAnnotations.Code)
                {
                    message.DeliveryAnnotations = (DeliveryAnnotations)described;
                }
                else if (described.Descriptor.Code == Codec.MessageAnnotations.Code)
                {
                    message.MessageAnnotations = (MessageAnnotations)described;
                }
                else if (described.Descriptor.Code == Codec.Properties.Code)
                {
                    message.Properties = (Properties)described;
                }
                else if (described.Descriptor.Code == Codec.ApplicationProperties.Code)
                {
                    message.ApplicationProperties = (ApplicationProperties)described;
                }
                else if (described.Descriptor.Code == Codec.Data.Code)
                {
                    if (message.BodySection == null)
                    {
                        message.BodySection = described;
                    }
                    else
                    {
                        if (dataList == null)
                        {
                            dataList = new DataList();
                            dataList.Add((Data)message.BodySection);
                            message.BodySection = dataList;
                        }

                        dataList.Add((Data)described);
                    }
                }
                else if (described.Descriptor.Code == Codec.AmqpValue.Code ||
                    described.Descriptor.Code == Codec.AmqpSequence.Code)
                {
                    message.BodySection = described;
                }
                else if (described.Descriptor.Code == Codec.Footer.Code)
                {
                    message.Footer = (Footer)described;
                }
                else
                {
                    throw new AmqpException(ErrorCode.FramingError,
                        Fx.Format(SRAmqp.AmqpUnknownDescriptor, described.Descriptor));
                }
            }

            return message;
        }

        /// <summary>
        /// Gets the information of the message delivery. It can be used to acknowledge the message
        /// later even if the message has been disposed or discarded.
        /// </summary>
        /// <returns>A <see cref="MessageDelivery"/> object, or null if delivery has not happened yet.</returns>
        public MessageDelivery GetDelivery()
        {
            return this.Delivery == null ? MessageDelivery.None : new MessageDelivery(this.Delivery, this.Format);
        }

        /// <summary>
        /// Disposes the current message to release resources.
        /// </summary>
        public void Dispose()
        {
            if (this.Delivery != null)
            {
                this.Delivery.Dispose();
                this.Delivery = null;
            }
        }

        internal ByteBuffer Encode(int reservedBytes)
        {
            ByteBuffer buffer = new ByteBuffer(reservedBytes + 128, true);
            buffer.AdjustPosition(buffer.Offset + reservedBytes, 0);
            this.WriteToBuffer(buffer);
            return buffer;
        }

        static void EncodeIfNotNull(RestrictedDescribed section, ByteBuffer buffer)
        {
            if (section != null)
            {
                section.Encode(buffer);
            }
        }

        void WriteToBuffer(ByteBuffer buffer)
        {
            EncodeIfNotNull(this.Header, buffer);
            EncodeIfNotNull(this.DeliveryAnnotations, buffer);
            EncodeIfNotNull(this.MessageAnnotations, buffer);
            EncodeIfNotNull(this.Properties, buffer);
            EncodeIfNotNull(this.ApplicationProperties, buffer);
            EncodeIfNotNull(this.BodySection, buffer);
            EncodeIfNotNull(this.Footer, buffer);
        }

#if NETFX || NETFX40 || DOTNET
        internal ByteBuffer Encode(IBufferManager bufferManager, int reservedBytes)
        {
            // get some extra space to store the frame header
            // and the transfer command.
            int size = reservedBytes + this.GetEstimatedMessageSize();
            ByteBuffer buffer = bufferManager.GetByteBuffer(size);
            buffer.AdjustPosition(buffer.Offset + reservedBytes, 0);
            this.WriteToBuffer(buffer);
            return buffer;
        }

        static int GetEstimatedBodySize(RestrictedDescribed body)
        {
            var data = body as Data;
            if (data != null)
            {
                return GetEstimatedDataSize(data);
            }

            var value = body as AmqpValue;
            if (value != null)
            {
                var b = value.Value as byte[];
                if (b != null)
                {
                    return b.Length;
                }

                var f = value.Value as ByteBuffer;
                if (f != null)
                {
                    return f.Length;
                }
            }

            var dataList = body as DataList;
            if (dataList != null)
            {
                int size = 0;
                for (int i = 0; i < dataList.Count; i++)
                {
                    size += GetEstimatedDataSize(dataList[i]);
                }
                return size;
            }

            return 64;
        }

        static int GetEstimatedDataSize(Data data)
        {
            if (data.Buffer != null)
            {
                return data.Buffer.Length;
            }
            else
            {
                return data.Binary.Length;
            }
        }

        /// <summary>
        /// Gets estimated message size in bytes.
        /// </summary>
        public int GetEstimatedMessageSize()
        {
            int size = 0;
            if (this.Header != null) size += 64;
            if (this.DeliveryAnnotations != null) size += 64;
            if (this.MessageAnnotations != null) size += 64;
            if (this.Properties != null) size += 64;
            if (this.ApplicationProperties != null) size += 64;
            if (this.BodySection != null) size += GetEstimatedBodySize(this.BodySection) + 8;
            if (this.Footer != null) size += 64;
            return size;
        }
#endif
    }
}