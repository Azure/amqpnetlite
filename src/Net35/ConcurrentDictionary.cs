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

namespace System.Collections.Concurrent
{
    using System.Collections.Generic;

    class ConcurrentDictionary<TKey, TValue> : Dictionary<TKey, TValue>
    {
        readonly object syncRoot;

        public ConcurrentDictionary()
        {
            this.syncRoot = new object();
        }

        public TValue GetOrAdd(TKey key, TValue value)
        {
            lock (this.syncRoot)
            {
                TValue temp;
                if (this.TryGetValue(key, out temp))
                {
                    return temp;
                }
                else
                {
                    this.Add(key, value);
                    return value;
                }
            }
        }
    }
}