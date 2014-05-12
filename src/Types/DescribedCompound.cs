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
    using System;
    using System.Diagnostics;
    using System.Text;

    public abstract class DescribedCompound : Described
    {
        protected DescribedCompound(Descriptor descriptor)
            : base(descriptor)
        {
        }

#if DEBUG
        protected string GetDebugString(string name, object[] fieldNames, object[] fieldValues)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(name);
            sb.Append("(");
            bool addComma = false;
            for (int i = 0; i < fieldValues.Length; i++)
            {
                if (fieldValues[i] != null)
                {
                    if (addComma)
                    {
                        sb.Append(",");
                    }

                    sb.Append(fieldNames[i]);
                    sb.Append(":");
                    sb.Append(GetStringObject(fieldValues[i]));
                    addComma = true;
                }
            }
            sb.Append(")");

            return sb.ToString();
        }

        object GetStringObject(object value)
        {
            string hexChars = "0123456789ABCDEF"; 
            byte[] binary = value as byte[];
            if (binary != null)
            {
                StringBuilder sb = new StringBuilder(binary.Length * 2);
                for (int i = 0; i < binary.Length; ++i)
                {
                    sb.Append(hexChars[binary[i] >> 4]);
                    sb.Append(hexChars[binary[i] & 0x0F]);
                }

                return sb.ToString();
            }
            else
            {
                return value;
            }
        }
#endif
    }
}
