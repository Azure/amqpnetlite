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

namespace Serialization.Poco
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Amqp;
    using Amqp.Serialization;

    /// <summary>
    /// Creates a contract for public instance properties. Serialization callbacks are
    /// not supported. Refer to <see cref="AmqpContractResolver"/> code for adding
    /// more supports.
    /// </summary>
    public class PocoContractResolver : IContractResolver
    {
        public IList<string> PrefixList { get; set; }

        AmqpContract IContractResolver.Resolve(Type type)
        {
            if (PrefixList != null)
            {
                bool matched = false;
                for (int i = 0; i < this.PrefixList.Count; i++)
                {
                    if (type.FullName.StartsWith(this.PrefixList[i], StringComparison.Ordinal))
                    {
                        matched = true;
                        break;
                    }
                }

                if (!matched)
                {
                    return null;
                }
            }

            AmqpContract baseContract = null;
            if (type.BaseType() != typeof(object))
            {
                baseContract = ((IContractResolver)this).Resolve(type.BaseType());
            }

            int order = 0;
            var memberList = new List<AmqpMember>();
            if (baseContract != null)
            {
                for (int i = 0; i < baseContract.Members.Length; i++)
                {
                    memberList.Add(baseContract.Members[i]);
                    order = Math.Max(order, baseContract.Members[i].Attribute.Order);
                }
            }

            order++;
            MemberInfo[] memberInfos = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (MemberInfo memberInfo in memberInfos)
            {
                if (memberInfo.DeclaringType != type)
                {
                    continue;
                }

                if (memberInfo is PropertyInfo)
                {
                    memberList.Add(new AmqpMember()
                    {
                        Attribute = new AmqpMemberAttribute()
                        {
                            Name = memberInfo.Name,
                            Order = order++
                        },
                        Info = memberInfo,
                    });
                }
            }

            List<Type> knownTypes = new List<Type>();
            foreach (Type t in type.Assembly().GetTypes())
            {
                if (t.BaseType() == type)
                {
                    knownTypes.Add(t);
                }
            }

            var contract = new AmqpContract(type)
            {
                Attribute = new AmqpContractAttribute()
                {
                    Name = type.FullName,
                    Encoding = EncodingType.List
                },
                Members = memberList.ToArray(),
                Provides = knownTypes.ToArray(),
                BaseContract = baseContract
            };

            return contract;
        }
    }
}