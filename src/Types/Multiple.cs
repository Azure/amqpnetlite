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

namespace Amqp.Types
{
    using System.Collections;
    using System.Text;

    public partial class Multiple
    {
        public void Encode(ByteBuffer buffer)
        {
            if (this.Count == 1)
            {
                Encoder.WriteObject(buffer, this[0]);
            }
            else
            {
                Encoder.WriteArray(buffer, this);
            }
        }

        public static Multiple From(object value)
        {
            object[] array = value as object[];
            if (array != null)
            {
                Multiple multiple = new Multiple();
                for (int i = 0; i < array.Length; ++i)
                {
                    multiple.Add(array[i]);
                }

                return multiple;
            }
            else
            {
                return new Multiple() { value };
            }
        }

        public override string ToString()
        {
            if (this.Count == 1)
            {
                return this[0].ToString();
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                sb.Append('[');
                for (int i = 0; i < this.Count; ++i)
                {
                    if (i > 0)
                    {
                        sb.Append(',');
                    }

                    sb.Append(this[i]);
                }
                sb.Append(']');
                return sb.ToString();
            }
        }
    }
}
