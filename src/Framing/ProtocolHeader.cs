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

namespace Amqp.Framing
{
    using System.Text;

    struct ProtocolHeader
    {
        public byte Id;

        public byte Major;

        public byte Minor;

        public byte Revision;

        public static ProtocolHeader Create(byte[] buffer, int offset)
        {
            const string Protocol = "AMQP";

            if (buffer[offset + 0] != (byte)Protocol[0] ||
                buffer[offset + 1] != (byte)Protocol[1] ||
                buffer[offset + 2] != (byte)Protocol[2] ||
                buffer[offset + 3] != (byte)Protocol[3])
            {
                throw new AmqpException(ErrorCode.InvalidField, string.Format("Unexpected protocol name in the header. Expected {0}, but received {1}.", Protocol, Encoding.ASCII.GetString(buffer, offset, 4)));
            }

            return new ProtocolHeader()
            {
                Id = buffer[offset + 4],
                Major = buffer[offset + 5],
                Minor = buffer[offset + 6],
                Revision = buffer[offset + 7]
            };
        }

#if TRACE
        public override string ToString()
        {
            return Fx.Format("{0} {1} {2} {3}", this.Id, this.Major, this.Minor, this.Revision);
        }
#endif
    }
}