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
    using System.Collections.Generic;
    using System.Reflection;

    /// <summary>
    /// Creates a serialization contract for types with <see cref="AmqpContractAttribute"/>
    /// and <see cref="AmqpMemberAttribute"/> attributes defined.
    /// </summary>
    public class AmqpContractResolver : IContractResolver
    {
        AmqpContract IContractResolver.Resolve(Type type)
        {
            AmqpContractAttribute contractAttribute = type.GetCustomAttribute<AmqpContractAttribute>(false);
            if (contractAttribute == null)
            {
                foreach (MemberInfo memberInfo in type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    AmqpMemberAttribute attribute = memberInfo.GetCustomAttribute<AmqpMemberAttribute>(true);
                    if (attribute != null)
                    {
                        throw new AmqpException(ErrorCode.NotAllowed,
                            Fx.Format("{0} has AmqpMemberAttribute members without AmqpContractAttribute on class", type.Name));
                    }
                }

                return null;
            }

            if (contractAttribute.Encoding == EncodingType.SimpleMap &&
                type.GetCustomAttribute<AmqpProvidesAttribute>(false) != null)
            {
                throw new AmqpException(ErrorCode.NotAllowed,
                    Fx.Format("{0}: SimpleMap encoding does not include descriptors so it does not support AmqpProvidesAttribute.", type.Name));
            }

            if (contractAttribute.Encoding == EncodingType.SimpleList &&
                type.GetCustomAttribute<AmqpProvidesAttribute>(false) != null)
            {
                throw new AmqpException(ErrorCode.NotAllowed,
                    Fx.Format("{0}: SimpleList encoding does not include descriptors so it does not support AmqpProvidesAttribute.", type.Name));
            }

            AmqpContract baseContract = null;
            if (type.BaseType() != typeof(object))
            {
                baseContract = ((IContractResolver)this).Resolve(type.BaseType());
                if (baseContract != null && baseContract.Attribute.Encoding != contractAttribute.Encoding)
                {
                    throw new AmqpException(ErrorCode.NotAllowed,
                        Fx.Format("{0}.Encoding ({1}) is different from {2}.Encoding ({3})",
                            type.Name, contractAttribute.Encoding, type.BaseType().Name, baseContract.Attribute.Encoding));
                }
            }

            string descriptorName = contractAttribute.Name;
            ulong? descriptorCode = contractAttribute.InternalCode;
            if (descriptorName == null && descriptorCode == null)
            {
                descriptorName = type.FullName;
            }

            var memberList = new List<AmqpMember>();
            int lastOrder = 0;
            if (baseContract != null)
            {
                for (int i = 0; i < baseContract.Members.Length; i++)
                {
                    memberList.Add(baseContract.Members[i]);
                    lastOrder = Math.Max(lastOrder, baseContract.Members[i].Order);
                }
            }

            lastOrder++;
            MethodInfo serializing = null;
            MethodInfo serialized = null;
            MethodInfo deserializing = null;
            MethodInfo deserialized = null;

            MemberInfo[] memberInfos = type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (MemberInfo memberInfo in memberInfos)
            {
                if (memberInfo.DeclaringType != type)
                {
                    continue;
                }

                if (memberInfo is FieldInfo || memberInfo is PropertyInfo)
                {
                    AmqpMemberAttribute attribute = memberInfo.GetCustomAttribute<AmqpMemberAttribute>(true);
                    if (attribute == null)
                    {
                        continue;
                    }

                    memberList.Add(new AmqpMember()
                    {
                        Attribute = attribute,
                        Info = memberInfo,
                        Order = attribute.InternalOrder ?? lastOrder++
                    });
                }
                else if (memberInfo is MethodInfo)
                {
                    if (memberInfo.GetCustomAttribute<OnSerializingAttribute>(false) != null)
                    {
                        serializing = (MethodInfo)memberInfo;
                    }
                    else if (memberInfo.GetCustomAttribute<OnSerializedAttribute>(false) != null)
                    {
                        serialized = (MethodInfo)memberInfo;
                    }
                    else if (memberInfo.GetCustomAttribute<OnDeserializingAttribute>(false) != null)
                    {
                        deserializing = (MethodInfo)memberInfo;
                    }
                    else if (memberInfo.GetCustomAttribute<OnDeserializedAttribute>(false) != null)
                    {
                        deserialized = (MethodInfo)memberInfo;
                    }
                }
            }

            if (contractAttribute.Encoding == EncodingType.List)
            {
                memberList.Sort(MemberOrderComparer.Instance);
                int order = -1;
                foreach (AmqpMember member in memberList)
                {
                    if (order > 0 && member.Order == order)
                    {
                        throw new AmqpException(ErrorCode.NotAllowed, Fx.Format("Duplicate Order {0} detected in {1}", order, type.Name));
                    }

                    order = member.Order;
                }
            }

            List<Type> knownTypes = new List<Type>();
            var providesAttributes = type.GetCustomAttributes<AmqpProvidesAttribute>(false);
            foreach (object o in providesAttributes)
            {
                AmqpProvidesAttribute knownAttribute = (AmqpProvidesAttribute)o;
                if (knownAttribute.Type.GetCustomAttribute<AmqpContractAttribute>(false) != null)
                {
                    knownTypes.Add(knownAttribute.Type);
                }
            }

            var contract = new AmqpContract(type)
            {
                Attribute = contractAttribute,
                Members = memberList.ToArray(),
                Provides = knownTypes.ToArray(),
                Serializing = serializing,
                Serialized = serialized,
                Deserializing = deserializing,
                Deserialized = deserialized,
                BaseContract = baseContract
            };

            this.OnResolved(contract);

            return contract;
        }

        /// <summary>
        /// Called when a type is successfully resolved. Derived class can
        /// override this method to update the contract if necessary.
        /// </summary>
        /// <param name="contract">The serialization contract.</param>
        protected virtual void OnResolved(AmqpContract contract)
        {
        }

        sealed class MemberOrderComparer : IComparer<AmqpMember>
        {
            public static readonly MemberOrderComparer Instance = new MemberOrderComparer();

            public int Compare(AmqpMember m1, AmqpMember m2)
            {
                return m1.Order.CompareTo(m2.Order);
            }
        }
    }
}