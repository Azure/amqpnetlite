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
    using Amqp.Serialization;
    using Amqp.Types;

    /// <summary>
    /// Provides extension methods for message serialization.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Gets an object of type T from the message body.
        /// </summary>
        /// <typeparam name="T">The object type.</typeparam>
        /// <param name="message">The message from which the body is deserialized.</param>
        /// <returns>An object of type T.</returns>
        public static T GetBody<T>(this Message message)
        {
            return GetBody<T>(message, AmqpSerializer.instance);
        }

        /// <summary>
        /// Gets an object of type T from the message body using the
        /// provided serializer.
        /// </summary>
        /// <typeparam name="T">The object type.</typeparam>
        /// <param name="message">The message from which the body is deserialized.</param>
        /// <param name="serializer">The serializer to deserialize the object.</param>
        /// <returns>An object of type T.</returns>
        public static T GetBody<T>(this Message message, AmqpSerializer serializer)
        {
            if (message.BodySection != null)
            {
                if (message.BodySection.Descriptor.Code == 0x0000000000000077UL/*Codec.AmqpValue.Code*/)
                {
                    return ((AmqpValue)message.BodySection).GetValue<T>(serializer);
                }
                else if (message.BodySection.Descriptor.Code == 0x0000000000000075UL/*Codec.Data.Code*/)
                {
                    Data data = (Data)message.BodySection;
                    if (typeof(T) == typeof(byte[]))
                    {
                        return (T)(object)data.Binary;
                    }
                    else if (typeof(T) == typeof(ByteBuffer))
                    {
                        return (T)(object)data.Buffer;
                    }
                }
            }

            return (T)message.Body;
        }

        static T GetValue<T>(this AmqpValue value, AmqpSerializer serializer)
        {
            ByteBuffer buffer = value.ValueBuffer;
            if (buffer == null)
            {
                return (T)value.Value;
            }

            buffer.Seek(0);
            if (typeof(T) == typeof(ByteBuffer))
            {
                int offset = GetBinaryOffset(buffer);
                int len = buffer.Length - offset;
                var payload = new ByteBuffer(buffer.Buffer, buffer.Offset + offset, len, len);
                return (T)(object)payload;
            }
            else if (typeof(T) == typeof(byte[]))
            {
                int offset = GetBinaryOffset(buffer);
                int len = buffer.Length - offset;
                byte[] payload = new byte[len];
                Buffer.BlockCopy(buffer.Buffer, buffer.Offset + offset, payload, 0, payload.Length);
                return (T)(object)payload;
            }
            else
            {
                return serializer.ReadObject<T>(buffer);
            }
        }

        static int GetBinaryOffset(ByteBuffer buffer)
        {
            byte formatCode = buffer.Buffer[buffer.Offset];
            if (formatCode != FormatCode.Binary8 && formatCode != FormatCode.Binary32)
            {
                throw new InvalidOperationException("Body is not binary type.");
            }

            return FixedWidth.FormatCode + (formatCode == FormatCode.Binary8 ? FixedWidth.UByte : FixedWidth.UInt);
        }
    }
}
