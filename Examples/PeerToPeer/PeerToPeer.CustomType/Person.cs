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

using System;
using System.Collections.Generic;
using Amqp.Serialization;

namespace PeerToPeer.CustomType
{
    /// <summary>
    /// The class defined below demonstrate the following features of the serializer.
    /// * descriptor: specify the Name property of AmqpContractAttribute.
    /// * encoding types: the object can be encoded as an AMQP described list, a described map,
    ///   or even a simple map.
    /// * order: specify the Order property of AmqpMemberAttribute. No effect for Map encoding.
    /// * collection values: AmqpMember can be generic List<T> or Dictionary<TKey, TValue> types.
    /// * class inheritance: use AmqpProvidesAttribute to define known types.
    /// * nested custom types: Student.Address is another custom type of a different encoding.
    /// * default attributes values: the Address class use default values for all attributes.
    /// * simple map with no descriptors: make it easy for other clients to send plain AMQP map
    ///   to represent the object.
    /// </summary>
    [AmqpContract(Name = "samples.amqpnetlite:person", Encoding = EncodingType.List)]
    [AmqpProvides(typeof(Student))]
    [AmqpProvides(typeof(Teacher))]
    public class Person
    {
        [AmqpMember(Name = "weight", Order = 1)]
        public int Weight { get; set; }

        [AmqpMember(Name = "height", Order = 2)]
        public int Height { get; set; }

        [AmqpMember(Name = "eye-color", Order = 3)]
        public string EyeColor { get; set; }

        public override string ToString()
        {
            return string.Format(
                "Weight: {0},\nHeight: {1},\nEyeColor: {2}",
                Weight,
                Height,
                EyeColor);
        }
    }

    [AmqpContract(Name = "samples.amqpnetlite:student", Encoding = EncodingType.List)]
    public class Student : Person
    {
        [AmqpMember(Name = "gpa", Order = 10)]
        public double GPA { get; set; }

        [AmqpMember(Name = "address", Order = 11)]
        public ListAddress Address { get; set; }
    }

    [AmqpContract(Name = "samples.amqpnetlite:teacher", Encoding = EncodingType.List)]
    public class Teacher : Person
    {
        [AmqpMember(Name = "department", Order = 10)]
        public string Department { get; set; }

        [AmqpMember(Name = "classes", Order = 11)]
        public List<string> Classes { get; set; }
    }

    [AmqpContract]
    public class ListAddress
    {
        [AmqpMember]
        public string Street { get; set; }

        [AmqpMember]
        public string City { get; set; }

        [AmqpMember]
        public string State { get; set; }

        [AmqpMember]
        public string Zip { get; set; }
    }

    [AmqpContract(Encoding = EncodingType.SimpleMap)]
    public class MapAddress
    {
        [AmqpMember(Name = "street")]
        public string Street { get; set; }

        [AmqpMember(Name = "city")]
        public string City { get; set; }

        [AmqpMember(Name = "state")]
        public string State { get; set; }

        [AmqpMember(Name = "zip")]
        public string Zip { get; set; }
    }

    [AmqpContract(Encoding = EncodingType.SimpleMap)]
    public class InternationalAddress
    {
        [AmqpMember(Name = "address")]
        public MapAddress Address { get; set; }

        [AmqpMember(Name = "country")]
        public string Country { get; set; }
    }
}
