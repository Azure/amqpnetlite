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
    using System.Reflection;

    /// <summary>
    /// The AMQP contract for serializing a custom type.
    /// </summary>
    public class AmqpContract
    {
        readonly Type type;

        /// <summary>
        /// Creates a contract object for a given type.
        /// </summary>
        /// <param name="type">The type that the contract is for.</param>
        public AmqpContract(Type type)
        {
            this.type = type;
        }

        /// <summary>
        /// Gets the type associated with the contract.
        /// </summary>
        public Type Type { get { return this.type; } }

        /// <summary>
        /// Gets or sets the <see cref="AmqpContractAttribute"/> of the type. 
        /// </summary>
        public AmqpContractAttribute Attribute { get; set; }

        /// <summary>
        /// Gets or sets the members of the type for serialization.
        /// </summary>
        public AmqpMember[] Members { get; set; }

        /// <summary>
        /// Gets or sets the types that this type can provide (typically through inheritance).
        /// </summary>
        public Type[] Provides { get; set; }

        /// <summary>
        /// Gets or sets the method to invoke before serialization. (<seealso cref="OnSerializingAttribute"/> .
        /// </summary>
        public MethodInfo Serializing { get; set; }

        /// <summary>
        /// Gets or sets the method to invoke after serialization. (<seealso cref="OnSerializedAttribute"/> .
        /// </summary>
        public MethodInfo Serialized { get; set; }

        /// <summary>
        /// Gets or sets the method to invoke before deserialization. (<seealso cref="OnDeserializingAttribute"/> .
        /// </summary>
        public MethodInfo Deserializing { get; set; }

        /// <summary>
        /// Gets or sets the method to invoke after deserialization. (<seealso cref="OnDeserializedAttribute"/> .
        /// </summary>
        public MethodInfo Deserialized { get; set; }

        /// <summary>
        /// Gets or sets the contract of the base type if it exists.
        /// </summary>
        public AmqpContract BaseContract { get; set; }
    }
}