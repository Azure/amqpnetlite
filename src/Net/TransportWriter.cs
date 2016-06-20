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

    class TransportWriter : ITransport
    {
        readonly IAsyncTransport transport;
        readonly Action<Exception> onException;
        readonly Queue<ByteBuffer> bufferQueue;
        bool writing;
        bool closed;

        public TransportWriter(IAsyncTransport transport, Action<Exception> onException)
        {
            this.transport = transport;
            this.onException = onException;
            this.bufferQueue = new Queue<ByteBuffer>();
        }

        object SyncRoot
        {
            get { return this.bufferQueue; }
        }

        void ITransport.Send(ByteBuffer buffer)
        {
            lock (this.SyncRoot)
            {
                if (this.closed)
                {
                    buffer.ReleaseReference();
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                if (this.writing)
                {
                    this.bufferQueue.Enqueue(buffer);
                    return;
                }

                this.writing = true;
            }

            this.WriteAsync(buffer);
        }

        int ITransport.Receive(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        void ITransport.Close()
        {
            lock (this.SyncRoot)
            {
                if (!this.closed)
                {
                    this.closed = true;
                    if (!this.writing)
                    {
                        this.transport.Close();
                    }
                }
            }
        }

        async void WriteAsync(ByteBuffer buffer)
        {
            const int maxBatchSize = 128 * 1024;

            List<ByteBuffer> buffers = new List<ByteBuffer>() { buffer };
            int size = buffer.Length;

            do
            {
                try
                {
                    await this.transport.SendAsync(buffers, size);
                }
                catch (Exception exception)
                {
                    lock (this.SyncRoot)
                    {
                        this.closed = true;

                        foreach (var f in this.bufferQueue)
                        {
                            f.ReleaseReference();
                        }

                        this.bufferQueue.Clear();
                        this.transport.Close();
                    }

                    this.onException(exception);

                    break;
                }
                finally
                {
                    for (int i = 0; i < buffers.Count; i++)
                    {
                        buffers[i].ReleaseReference();
                    }

                    buffers.Clear();
                    size = 0;
                }

                lock (this.SyncRoot)
                {
                    while (size < maxBatchSize && this.bufferQueue.Count > 0)
                    {
                        ByteBuffer item = this.bufferQueue.Dequeue();
                        size += item.Length;
                        buffers.Add(item);
                    }

                    if (size == 0)
                    {
                        this.writing = false;
                        if (this.closed)
                        {
                            this.transport.Close();
                        }

                        break;
                    }
                }
            }
            while (true);
        }
    }
}