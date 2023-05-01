//  ------------------------------------------------------------------------------------
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

using Amqp;
using Amqp.Framing;
using Amqp.Serialization;
using System.Collections.Generic;

namespace Test.Amqp
{
    public class MessageBatch : Message
    {
        public const uint BatchFormat = 0x80013700;

        public static MessageBatch Create<T>(IEnumerable<T> objects)
        {
            DataList dataList = new DataList();
            foreach (var obj in objects)
            {
                ByteBuffer buffer = new ByteBuffer(1024, true);
                var section = new AmqpValue<T>(obj);
                AmqpSerializer.Serialize(buffer, section);
                dataList.Add(new Data() { Buffer = buffer });
            }

            return new MessageBatch() { Format = BatchFormat, BodySection = dataList };
        }
    }
}
