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
    using System.Collections.Generic;
    using System.Reflection;
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
        /// <returns>An object of type T.</returns>
        public static T GetBody<T>(this Message message)
        {
            if (message.BodySection != null)
            {
                if (message.BodySection.Descriptor.Code == 0x0000000000000077UL/*Codec.AmqpValue.Code*/)
                {
                    return ((AmqpValue)message.BodySection).GetValue<T>();
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

        static T GetValue<T>(this AmqpValue value)
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
                return AmqpSerializer.Deserialize<T>(buffer);
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

#if DOTNET
        internal static T GetCustomAttribute<T>(this Type type, bool inherit) where T : Attribute
        {
            return type.GetTypeInfo().GetCustomAttribute<T>(inherit);
        }

        internal static IEnumerable<T> GetCustomAttributes<T>(this Type type, bool inherit) where T : Attribute
        {
            return type.GetTypeInfo().GetCustomAttributes<T>(inherit);
        }

        internal static Type BaseType(this Type type)
        {
            return type.GetTypeInfo().BaseType;
        }

        internal static bool IsValueType(this Type type)
        {
            return type.GetTypeInfo().IsValueType;
        }

        internal static bool IsEnum(this Type type)
        {
            return type.GetTypeInfo().IsEnum;
        }

        internal static bool IsGenericType(this Type type)
        {
            return type.GetTypeInfo().IsGenericType;
        }

        internal static bool IsAssignableFrom(this Type type, Type from)
        {
            return type.GetTypeInfo().IsAssignableFrom(from.GetTypeInfo());
        }

        internal static object CreateInstance(this Type type, bool hasDefaultCtor)
        {
            return Activator.CreateInstance(type);
        }
#endif
    }
}
