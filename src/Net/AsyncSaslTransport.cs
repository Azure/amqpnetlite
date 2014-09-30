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

namespace Amqp
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    sealed class AsyncSaslTransport : IAsyncTransport
    {
        readonly IAsyncTransport innerTransport;

        public AsyncSaslTransport(IAsyncTransport innerTransport)
        {
            this.innerTransport = innerTransport;
        }

        public void Close()
        {
            this.innerTransport.Close();
        }

        public void Send(ByteBuffer buffer)
        {
            this.innerTransport.Send(buffer);
        }

        public int Receive(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException();
        }

        public bool SendAsync(ByteBuffer buffer, IList<ArraySegment<byte>> bufferList, int listSize)
        {
            throw new InvalidOperationException();
        }

        public Task<int> ReceiveAsync(byte[] buffer, int offset, int count)
        {
            return this.innerTransport.ReceiveAsync(buffer, offset, count);
        }
    }
}