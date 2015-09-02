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

namespace Test.Amqp
{
    using System;
    using System.Collections.Generic;
    using global::Amqp.Serialization;
    using global::Amqp.Types;
    using global::Amqp;

    [AmqpContract(Name = "test.amqp:person", Code = 0x0000123400000000)]
    [AmqpProvides(typeof(Student))]
    [AmqpProvides(typeof(Teacher))]
    class Person
    {
        public Person(string name)
        {
            this.Name = name;
        }

        [AmqpMember(Order = 1)]
        public string Name
        {
            get;
            private set;
        }

        [AmqpMember(Order = 2)]
        public int Age
        {
            get;
            set;
        }

        [AmqpMember(Order = 3)]
        public DateTime? DateOfBirth;

        public IDictionary<string, object> Properties
        {
            get
            {
                if (this.properties == null)
                {
                    this.properties = new Dictionary<string, object>();
                }

                return this.properties;
            }
        }

        [AmqpMember(Order = 8)]
        Dictionary<string, object> properties;

        [System.Runtime.Serialization.OnDeserialized]
        void OnDesrialized()
        {
            this.Age = this.Age + 1;
        }
    }

    [AmqpContract(Name = "test.amqp:student", Code = 0x0000123400000001)]
    class Student : Person
    {
        Student() : base(null) { }

        public Student(string name)
            : base(name)
        {
        }

        [AmqpMember(Name = "address", Order = 4)]
        public StreetAddress Address;

        [AmqpMember(Name = "grades", Order = 10)]
        public List<int> Grades { get; set; }
    }

    [AmqpContract(Name = "test.amqp:teacher", Code = 0x0000123400000002)]
    class Teacher : Person
    {
        public Teacher(string name)
            : base(name)
        {
            this.Id = EmployeeId.New();
        }

        [AmqpMember(Name = "sallary", Order = 4)]
        public int Sallary;

        [AmqpMember(Order = 10)]
        public EmployeeId Id
        {
            get;
            private set;
        }

        [AmqpMember(Order = 11)]
        public Dictionary<int, string> Classes
        {
            get;
            set;
        }

        [System.Runtime.Serialization.OnDeserialized]
        void OnDesrialized()
        {
            this.Sallary *= 2;
        }
    }

    [AmqpContract(Name = "test.amqp:address", Code = 0x0000123400000003)]
    class StreetAddress
    {
        [AmqpMember]
        public string FullAddress;
    }

    [AmqpContract(Name = "test.amqp:product", Encoding = EncodingType.Map)]
    class Product
    {
        [AmqpMember]
        public string Name;

        [AmqpMember]
        public double Price;

        [AmqpMember]
        public long Weight;
    }

    class EmployeeId : IAmqpSerializable
    {
        Guid uuid;

        EmployeeId(Guid uuid)
        {
            this.uuid = uuid;
        }

        public int EncodeSize
        {
            get { return 16; }
        }

        public void Encode(ByteBuffer buffer)
        {
            byte[] bytes = this.uuid.ToByteArray();
            buffer.Validate(true, bytes.Length);
            Buffer.BlockCopy(bytes, 0, buffer.Buffer, buffer.WritePos, bytes.Length);
            buffer.Append(bytes.Length);
        }

        public void Decode(ByteBuffer buffer)
        {
            byte[] bytes = new byte[16];
            Buffer.BlockCopy(buffer.Buffer, buffer.Offset, bytes, 0, bytes.Length);
            this.uuid = new Guid(bytes);
            buffer.Complete(bytes.Length);
        }

        public static EmployeeId New()
        {
            return new EmployeeId(Guid.NewGuid());
        }

        public override bool Equals(object obj)
        {
            return obj is EmployeeId && ((EmployeeId)obj).uuid == this.uuid;
        }

        public override int GetHashCode()
        {
            return this.uuid.GetHashCode();
        }
    }
}
