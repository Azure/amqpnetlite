AMQP defines a type system for primitive types and custom types. The serializer converts
any serializable .NET object into AMQP bytes, and vice visa. Since the payload contains
a complete valid sequence of AMQP values, it becomes easy for the application to send
.NET objects across the network, and also interoperate with any other AMQP compliant
clients that can read/write AMQP values.

## Serializable Types

The following types are serializable.
* Primitives as listed in the below mapping table.
* Collection (Array/List/Map) of serializable types.
* Enum
* Custom Types - User defined classes are encoded as AMQP described types when the class
and its fields/properties are annotated with the AMQP serialization attributes.
See [AmqpContract](#amqpcontract) for details.
* [IAmqpSerializable](#iamqpserializable) - User defined classes that implement this
interface can be serialized using the Encode and Decode implementation provided by the user.

The serializer follows this order to resolve a type. If a type cannot be resolved, an
exception is thrown.
```
[AmqpContract] -> AMQP primitive types -> IAmqpSerializable -> Enum -> Array/List/Map
```
A custom class should not have AmqpContractAttribute and implement IAmqpSerializable at
the same time, because the serializer stops checking for IAmqpSerializable as soon as
AmqpContract attribute is found.

The mapping between .NET and AMQP primitive types is defined in the following table.

| .NET | AMQP |
| ---- | ---- |
| null | null |
| bool | boolean |
| byte | ubyte |
| sbyte | byte |
| ushort | ushort |
| short | short |
| uint | uint |
| int | int |
| ulong | ulong |
| long | long |
| float | float |
| double | double |
| char | char |
| DateTime | timestamp |
| Guid | uuid |
| byte[] | binary |
| string | string |
| Symbol | symbol |
| Array | array |
| IList | list |
| IDictionary | map |
| Enum | the underlying type, e.g. int |
| `Nullable<T>` | null or the supported type T |
* decimal is not supported

## AmqpContract

A user defined class can be annotated with a few attribute classes to be serializable.

### Example

```
namespace Amqp.Examples;

[AmqpContract]
class Person
{
    [AmqpMember]
    public string Name { get; set; }

    [AmqpMember]
    public DateTime DateOfBirth;

    [AmqpMember]
    Dictionary<string, object> properties;
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
}
```

With these annotations, the class is serialized as an AMQP described list. The descriptor
name is the class's full name. The following is the equivalent xml definition.

```
<type name="Person" class="composite" source="list">
  <descriptor name="Amqp.Examples.Person"/>
  <field name="Name" type="string"/>
  <field name="DateOfBirth" type="timestamp"/>
  <field name="properties" type="map"/>
</type>
```

### AmqpContractAttribute

A class annotated with this attribute is serializable. The attribute specifies the descriptor
name and code, and also the AMQP type to contain the fields/properties.

```
[AmqpContract(Name = "amqp.examples:person", Code = 0x0000123400000000, Encoding = EncodingType.Map)]
```

By default, the descriptor name is the class full name. If descriptor code is provided, the name
is ignored. The descriptor is scoped to the instance of AmqpSerializer that performs the serialization.
The static methods of AmqpSerializer belong to the default static instance. Application SHOULD ensure
that the descriptor is unique in the serialization scope to avoid conflicts.  
When Encoding is set to SimpleList or SimpleMap, the descriptor is not serialized. The object is
encoded directly as an AMQP list or map.

### AmqpMemberAttribute

This attribute specifies that a field or a property should be included in serialization.

```
[AmqpMember(Name = "name", Order = 0)]
```

The Name property defines the member name, and the Order property defines the position of the member
in all members. For list type of encoding, Name is not used, and Order is used to sort the members
to decide the final position of each member in the list. Order does not need to contiguous, but must
be unique. For map type of encoding, Order is not used, and Name is used as the key of the item in
the map. EncodingType.Map encodes the key as AMQP symbol and decodes both symbol and string keys.
EncodingType.SimpleMap encodes keys as strings.

### AmqpProvidesAttribute

This attribute is used by the decoder to resolve types based on the given type and the descriptor in
the payload. After the descriptor is read from the payload, it is compared to the descriptors of
the known types and the matching one is chosen. This attribute can only be used for encoding type
List or Map because descriptors are required in serialization. A common scenario is class inheritance.
The base and derived classes must have the same EncodingType.

```
[AmqpContract]
class Student : Person
{
    [AmqpMember]
    public double GPA { get; set; }
}

[AmqpContract]
class Teacher : Person
{
    [AmqpMember]
    public string Department { get; set; }
}

[AmqpContract]
[AmqpProvides(typeof(Student))]
[AmqpProvides(typeof(Teacher))]
class Person
{
}
```

Student and Teacher are now known types of Person, so it is possible to deserialize objects by calling.

```
var person = AmqpSerializer.Deserialize<Person>(ByteBuffer);
```

If the payload contains Student, Teacher, or Person objects, the concrete object will be returned
and referenced by person variable.

This attribute can also be used just as a type resolver without class inheritance. In this case,
the generic argument type is only a registry for knowns types. Since the generic argument type is not
the base class of its known types, the serializer will fail to cast the decoded object to the generic
argument type. This issue can be resolved by calling the `AmqpSerializer.Deserialize<T, TAs>(ByteBuffer)`
method, where T is the registry type and TAs is the base type of all decoded objects. Let’s assume we
have another class Address which is not related to Person and its derived classes.

```
[AmqpContract]
class Address
{
    [AmqpMember]
    public string Street { get; set; }
}

[AmqpContract]
[AmqpProvides(typeof(Person))]
[AmqpProvides(typeof(Student))]
[AmqpProvides(typeof(Teacher))]
[AmqpProvides(typeof(Address))]
class Resolver
{
}
```

In addition to Person objects, if the buffer could also contain Address objects, using Person as
deserialization type will fail. With the Resolver class, we can decode all objects by calling
`AmqpSerializer.Deserialize<Resolver, object>(ByteBuffer)`.  
Note that Student and Teacher should be added even when Person is added.

### Serialization Callbacks

Extra logic can be inserted before and after serialization/deserialization by serialization callbacks.
The callback methods should be annotated with the system runtime serialization attributes.
* [OnSerializingAttribute](https://msdn.microsoft.com/en-us/library/system.runtime.serialization.onserializingattribute(v=vs.110).aspx) - invoked before writing members
* [OnSerializedAttribute](https://msdn.microsoft.com/en-us/library/system.runtime.serialization.onserializedattribute(v=vs.110).aspx) - invoked after writing members
* [OnDeserializingAttribute](https://msdn.microsoft.com/en-us/library/system.runtime.serialization.ondeserializingattribute(v=vs.110).aspx) - invoked before reading members
* [OnDeserializedAttribute](https://msdn.microsoft.com/en-us/library/system.runtime.serialization.ondeserializedattribute(v=vs.110).aspx) - invoked after reading members

## IAmqpSerializable

The interface enables the application to have full control on how a type should be serialized by
implementing Encode and Decode methods.

## Message Integration

The library provides a seamless integration of the serializer and the messaging layer. When a message
is created from a serializable object, the object is automatically serialized in the AMQP Value body.
When a message is received, the object can be read by calling `Message.GetBody<T>()`.

```
// sending an object in a message
var student = new Student() { Name = "Bob" };
sender.Send(new Message(student));

// reading an object from a message
var message = receiver.Receive();
var person = message.GetBody<Person>();
```

Here are some benefits you will get.
* Serialization is completely transparent. The application works with strongly typed objects.
* No need of another serializer to handle message body. It minimizes dependency of your application,
and very likely improves the performance.
* It is standard and open. Any client that supports the AMQP type system is able to produce and consume the data.

On .Net Core (netstandard1.3), the serializer is in a seperate package (AMQPNetLite.Serialization).
To use this feature, you should do the following.
* Install the serialization package.
* When creating a message for send, use the `AmqpValue<T>` class to wrap the object of your custom type.
```
var student = new Student() { Name = "Bob" };
var message = new Message() { BodySection = new AmqpValue<Person>(student) };
sender.Send(message);
```
* Use the same way to read the object by calling `Message.GetBody<T>()`.
