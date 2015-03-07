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

    public partial class Map
    {
        public new object this[object key]
        {
            get
            {
                this.CheckKeyType(key.GetType());
                return this.GetValue(key);
            }

            set
            {
                this.CheckKeyType(key.GetType());
                base[key] = value;
            }
        }

        internal static void ValidateKeyType(Type expected, Type actual)
        {
            if (expected != actual)
            {
                throw new InvalidOperationException(Fx.Format(SRAmqp.InvalidMapKeyType, actual.Name, expected.Name));
            }
        }

        protected virtual void CheckKeyType(Type keyType)
        {
        }

#if TRACE
        public override string ToString()
        {
            var sb = new System.Text.StringBuilder(64);
            sb.Append('[');
            int i = 0;
            foreach (var key in this.Keys)
            {
                if (i++ > 0) sb.Append(',');
                sb.Append(key);
                sb.Append(':');
                sb.Append(this[key]);
            }
            sb.Append(']');

            return sb.ToString();
        }
#endif
    }
}
