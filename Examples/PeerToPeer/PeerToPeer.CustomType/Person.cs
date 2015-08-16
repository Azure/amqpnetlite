using System;
using Amqp.Serialization;

namespace PeerToPeer.CustomType
{
    [AmqpContract]
    public class Person
    {
        [AmqpMember]
        public int Weight { get; set; }

        [AmqpMember]
        public int Height { get; set; }

        [AmqpMember]
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
}
