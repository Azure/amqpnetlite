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
    using System.Reflection;

    /// <summary>
    /// Represents an AMQP member (a field or a property) within a contract.
    /// </summary>
    public class AmqpMember
    {
        /// <summary>
        /// The attribute on the member.
        /// </summary>
        public AmqpMemberAttribute Attribute { get; set; }

        /// <summary>
        /// The member info provided by the system.
        /// </summary>
        public MemberInfo Info { get; set; }

        internal string Name { get { return this.Attribute.Name ?? this.Info.Name; } }

        internal int Order { get; set; }
    }
}