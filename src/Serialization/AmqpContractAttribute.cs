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

    /// <summary>
    /// Specifies that the type is serializable by the AMQP serializer.
    /// </summary>
    /// <remarks>
    /// The object is serialized as an AMQP described list, a described map,
    /// or a simple map, as determined by the Encoding property. The default value is List.
    /// 
    /// The Name or Code property specifies the symbol descriptor or the ulong descriptor
    /// of the described value, respectively. If both are set, the Code property
    /// (ulong descriptor) takes precedence. If none is set, the class's full name is used
    /// as the symbol descriptor. It is recommended to include a domain name/code in the
    /// descriptor to avoid collision. For example, Name can be "com.microsoft:my-class",
    /// and Code can be 0x0000013700000001 (domain code is the company number defined at
    /// http://www.iana.org/assignments/enterprise-numbers/enterprise-numbers)
    /// 
    /// List or Map encoding encodes the object as a described type, which contains the above
    /// mentioned descriptor and the value (a list or a map). SimpleMap encodes the object
    /// as a map value without a descriptor. How the value is encoded depends on EncodingType.
    /// List: the value is encoded as an AMQP List that contains all fields/properties of
    /// the class decorated with AmqpMemberAttribute. The Order property of the AmqpMember
    /// attribute determines the relative positions of the members in the list. Duplicate
    /// Order values are not allowed. If the class is derived from a base class, the
    /// AmqpMember fields/properties of the base class, if any, are also included in the list.
    /// The list is flattened.
    /// Map: the value is encoded as an AMQP map that contains all fields/properties of
    /// the class decorated with AmqpMemberAttribute. The key type is AMQP symbol. The key
    /// values are determined by the Name property of each AmqpMember attribute. If the class
    /// is derived from a base class, the AmqpMember fields/properties of the base class,
    /// if any, are also included in the map. Though the items in an AMQP map are ordered,
    /// the serializer ignores the Order property of the AmqpMember attribute.
    /// SimpleMap: value encoding is the same as Map except that the keys are AMQP string.
    /// Because this encoding does not have descriptors, decoding an object with a base type
    /// decorated with AmqpProvidesAttribute is not supported.
    /// SimpleList: value encoding is the same as List except that no initial descriptor is
    /// written. Because this encoding does not have descriptors, decoding an object with a
    /// base type decorated with AmqpProvidesAttribute is not supported.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct,
        AllowMultiple = false, Inherited = true)]
    public sealed class AmqpContractAttribute : Attribute
    {
        ulong? internalCode;

        /// <summary>
        /// Initializes a new instance of the AmqpContractAttribute class.
        /// </summary>
        public AmqpContractAttribute()
        {
            this.Encoding = EncodingType.List;
        }

        /// <summary>
        /// Gets or sets the descriptor name for the type.
        /// </summary>
        public string Name
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the descriptor code for the type.
        /// </summary>
        public long Code
        {
            get { return (long)(this.internalCode ?? 0UL); }
            set { this.internalCode = (ulong)value; }
        }

        /// <summary>
        /// Gets or sets the encoding type for the type.
        /// </summary>
        public EncodingType Encoding
        {
            get;
            set;
        }

        internal ulong? InternalCode
        {
            get { return this.internalCode; }
        }
    }
}