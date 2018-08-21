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

namespace Amqp.Serialization
{
    using System;
#if !NETFX35
    using System.Collections.Concurrent;
#endif
    using System.Collections.Generic;
    using System.Reflection;
    using Amqp.Types;

    /// <summary>
    /// Serializes and deserializes an instance of an AMQP type.
    /// The descriptor (name and code) is scoped to and must be
    /// unique within an instance of the serializer.
    /// When the static Serialize and Deserialize methods are called,
    /// the default instance is used.
    /// </summary>
    public sealed class AmqpSerializer
    {
        internal static readonly AmqpSerializer instance = new AmqpSerializer();
        readonly ConcurrentDictionary<Type, SerializableType> typeCache;
        readonly IContractResolver contractResolver;

        /// <summary>
        /// Initializes a new instance of the AmqpSerializer class with the default contract
        /// resolver that supports custom classes decorated with
        /// <see cref="AmqpContractAttribute"/> and <see cref="AmqpMemberAttribute"/>.
        /// </summary>
        public AmqpSerializer()
            : this(new AmqpContractResolver())
        {
        }

        /// <summary>
        /// Initializes a new instance of the AmqpSerializer class with a custom contract
        /// resolver. See documentation for the order followed by the serializer to resolve
        /// a type.
        /// </summary>
        /// <param name="contractResolver">A contract resolver to create a serialization
        /// contract for a given type.</param>
        public AmqpSerializer(IContractResolver contractResolver)
        {
            this.typeCache = new ConcurrentDictionary<Type, SerializableType>();
            this.contractResolver = contractResolver;
        }

        /// <summary>
        /// Serializes an instance of an AMQP type into a buffer.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="graph">The serializable AMQP object.</param>
        public static void Serialize(ByteBuffer buffer, object graph)
        {
            WriteObject(instance, buffer, graph);
        }

        /// <summary>
        /// Deserializes an instance of an AMQP type from a buffer.
        /// </summary>
        /// <typeparam name="T">The serializable type.</typeparam>
        /// <param name="buffer">The buffer to read from.</param>
        /// <returns></returns>
        public static T Deserialize<T>(ByteBuffer buffer)
        {
            return ReadObject<T, T>(instance, buffer);
        }

        /// <summary>
        /// Deserializes an instance of an AMQP type from a buffer.
        /// </summary>
        /// <typeparam name="T">The serializable type.</typeparam>
        /// <typeparam name="TAs">The return type of the deserialized object.</typeparam>
        /// <param name="buffer">The buffer to read from.</param>
        /// <returns></returns>
        public static TAs Deserialize<T, TAs>(ByteBuffer buffer)
        {
            return ReadObject<T, TAs>(instance, buffer);
        }

        /// <summary>
        /// Writes an serializable object into a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="graph">The serializable object.</param>
        public void WriteObject(ByteBuffer buffer, object graph)
        {
            WriteObject(this, buffer, graph);
        }

        /// <summary>
        /// Reads an serializable object from a buffer.
        /// </summary>
        /// <typeparam name="T">The type of the serializable object.</typeparam>
        /// <param name="buffer">The buffer to read.</param>
        /// <returns></returns>
        public T ReadObject<T>(ByteBuffer buffer)
        {
            return ReadObject<T, T>(this, buffer);
        }

        /// <summary>
        /// Reads an serializable object from a buffer.
        /// </summary>
        /// <typeparam name="T">The type of the serializable object.</typeparam>
        /// <typeparam name="TAs">The return type of the deserialized object.</typeparam>
        /// <param name="buffer">The buffer to read.</param>
        /// <returns></returns>
        public TAs ReadObject<T, TAs>(ByteBuffer buffer)
        {
            return ReadObject<T, TAs>(this, buffer);
        }

        internal SerializableType GetType(Type type)
        {
            return this.GetOrCompileType(type, false);
        }

        static void WriteObject(AmqpSerializer serializer, ByteBuffer buffer, object graph)
        {
            if (graph == null)
            {
                Encoder.WriteObject(buffer, null);
            }
            else
            {
                SerializableType type = serializer.GetType(graph.GetType());
                type.WriteObject(buffer, graph);
            }
        }

        static TAs ReadObject<T, TAs>(AmqpSerializer serializer, ByteBuffer buffer)
        {
            SerializableType type = serializer.GetType(typeof(T));
            return (TAs)type.ReadObject(buffer);
        }

        SerializableType GetOrCompileType(Type type, bool describedOnly)
        {
            SerializableType serialiableType = null;
            if (!this.typeCache.TryGetValue(type, out serialiableType))
            {
                serialiableType = this.CompileType(type, describedOnly);
                if (serialiableType != null)
                {
                    serialiableType = this.typeCache.GetOrAdd(type, serialiableType);
                }
            }

            if (serialiableType == null)
            {
                throw new NotSupportedException(type.FullName);
            }

            return serialiableType;
        }

        SerializableType CompileType(Type type, bool describedOnly)
        {
            AmqpContract contract = this.contractResolver.Resolve(type);
            if (contract != null)
            {
                return this.CreateContractType(contract);
            }

            return this.CompileNonContractTypes(type);
        }

        SerializableType CreateContractType(AmqpContract contract)
        {
            Type type = contract.Type;
            string descriptorName = contract.Attribute.Name;
            ulong? descriptorCode = contract.Attribute.InternalCode;
            if (descriptorName == null && descriptorCode == null)
            {
                descriptorName = type.FullName;
            }

            SerializableMember[] members = new SerializableMember[contract.Members.Length];
            for (int i = 0; i < contract.Members.Length; i++)
            {
                SerializableMember member = new SerializableMember();
                members[i] = member;

                AmqpMember amqpMember = contract.Members[i];
                member.Name = amqpMember.Name;
                member.Order = amqpMember.Order;
                member.Accessor = MemberAccessor.Create(amqpMember.Info, true);

                // This will recursively resolve member types
                Type memberType = amqpMember.Info is FieldInfo ?
                    ((FieldInfo)amqpMember.Info).FieldType :
                    ((PropertyInfo)amqpMember.Info).PropertyType;
                member.Type = GetType(memberType);
            }

            MethodAccessor[] serializationCallbacks = new MethodAccessor[]
            {
                contract.Serializing == null ? null : MethodAccessor.Create(contract.Serializing),
                contract.Serialized == null ? null : MethodAccessor.Create(contract.Serialized),
                contract.Deserializing == null ? null : MethodAccessor.Create(contract.Deserializing),
                contract.Deserialized == null ? null : MethodAccessor.Create(contract.Deserialized)
            };

            SerializableType baseType = null;
            if (contract.BaseContract != null)
            {
                baseType = this.CreateContractType(contract.BaseContract);
            }

            Dictionary<Type, SerializableType> knownTypes = null;
            if (contract.Provides != null)
            {
                knownTypes = new Dictionary<Type, SerializableType>();
                for (int i = 0; i < contract.Provides.Length; i++)
                {
                    // KnownType compilation is delayed and non-recursive to avoid circular references
                    knownTypes.Add(contract.Provides[i], null);
                }
            }

            if (contract.Attribute.Encoding == EncodingType.List)
            {
                return SerializableType.CreateDescribedListType(this, type, baseType, descriptorName,
                    descriptorCode, members, knownTypes, serializationCallbacks);
            }
            else if (contract.Attribute.Encoding == EncodingType.Map)
            {
                return SerializableType.CreateDescribedMapType(this, type, baseType, descriptorName,
                    descriptorCode, members, knownTypes, serializationCallbacks);
            }
            else if (contract.Attribute.Encoding == EncodingType.SimpleMap)
            {
                return SerializableType.CreateDescribedSimpleMapType(this, type, baseType, members, serializationCallbacks);
            }
            else if (contract.Attribute.Encoding == EncodingType.SimpleList)
            {
                return SerializableType.CreateDescribedSimpleListType(this, type, baseType, members, serializationCallbacks);
            }
            else
            {
                throw new NotSupportedException(contract.Attribute.Encoding.ToString());
            }
        }

        SerializableType CompileNonContractTypes(Type type)
        {
            // built-in type
            Encode encoder;
            Decode decoder;
            if (Encoder.TryGetCodec(type, out encoder, out decoder))
            {
                return SerializableType.CreatePrimitiveType(type, encoder, decoder);
            }

            if (type == typeof(object))
            {
                return SerializableType.CreateObjectType(type);
            }

            if (typeof(Described).IsAssignableFrom(type))
            {
                return SerializableType.CreateObjectType(type);
            }

            if (typeof(IAmqpSerializable).IsAssignableFrom(type))
            {
                return SerializableType.CreateAmqpSerializableType(this, type);
            }

            if (type.IsGenericType() && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                Type[] argTypes = type.GetGenericArguments();
                Fx.Assert(argTypes.Length == 1, "Nullable type must have one argument");
                Type argType = argTypes[0];
                if (argType.IsEnum())
                {
                    return CompileEnumType(argType);
                }
                else
                {
                    return SerializableType.CreateObjectType(type);
                }
            }

            if (type.IsEnum())
            {
                return CompileEnumType(type);
            }

            SerializableType collection = this.CompileCollectionTypes(type);
            if (collection != null)
            {
                return collection;
            }

            return null;
        }

        SerializableType CompileEnumType(Type type)
        {
            SerializableType underlyingType = GetType(Enum.GetUnderlyingType(type));
            return SerializableType.CreateEnumType(type, underlyingType);
        }

        SerializableType CompileCollectionTypes(Type type)
        {
            MemberAccessor keyAccessor = null;
            MemberAccessor valueAccessor = null;
            MethodAccessor addAccess = null;
            Type itemType = null;

            if (type.IsArray)
            {
                // array of custom types. encode it as list
                return SerializableType.CreateArrayType(this, type, type.GetElementType());
            }

            foreach (Type it in type.GetInterfaces())
            {
                if (it.IsGenericType())
                {
                    Type genericTypeDef = it.GetGenericTypeDefinition();
                    if (genericTypeDef == typeof(IDictionary<,>))
                    {
                        Type[] argTypes = it.GetGenericArguments();
                        itemType = typeof(KeyValuePair<,>).MakeGenericType(argTypes);
                        keyAccessor = MemberAccessor.Create(itemType.GetProperty("Key"), false);
                        valueAccessor = MemberAccessor.Create(itemType.GetProperty("Value"), false);
                        addAccess = MethodAccessor.Create(type.GetMethod("Add", argTypes));

                        return SerializableType.CreateGenericMapType(this, type, keyAccessor, valueAccessor, addAccess);
                    }
                    else if (genericTypeDef == typeof(IList<>))
                    {
                        Type[] argTypes = it.GetGenericArguments();
                        itemType = argTypes[0];
                        addAccess = MethodAccessor.Create(type.GetMethod("Add", argTypes));

                        return SerializableType.CreateGenericListType(this, type, itemType, addAccess);
                    }
                }
            }

            return null;
        }

        sealed class MemberOrderComparer : IComparer<SerializableMember>
        {
            public static readonly MemberOrderComparer Instance = new MemberOrderComparer();

            public int Compare(SerializableMember m1, SerializableMember m2)
            {
                return m1.Order == m2.Order ? 0 : (m1.Order > m2.Order ? 1 : -1);
            }
        }

#if NETFX35
        // this is for use within the serializer class only
        // ensure only the synchronized methods are called
        class ConcurrentDictionary<TKey, TValue> : Dictionary<TKey, TValue>
        {
            readonly object syncRoot;

            public ConcurrentDictionary()
            {
                this.syncRoot = new object();
            }

            public new bool TryGetValue(TKey key, out TValue value)
            {
                lock (this.syncRoot)
                {
                    return base.TryGetValue(key, out value);
                }
            }

            public TValue GetOrAdd(TKey key, TValue value)
            {
                lock (this.syncRoot)
                {
                    TValue temp;
                    if (base.TryGetValue(key, out temp))
                    {
                        return temp;
                    }
                    else
                    {
                        base.Add(key, value);
                        return value;
                    }
                }
            }
        }
#endif
    }
}
