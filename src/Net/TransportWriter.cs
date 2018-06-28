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

        public Task FlushAsync()
        {
            var buffer = new FlushByteBuffer();
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
                }
                else
                {
                    buffer.ReleaseReference();
                }
            }

            return buffer.Task;
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

                this.bufferQueue.Enqueue(buffer);
                if (this.writing)
                {
                    return;
                }

                this.writing = true;
            }

            this.WriteAsync();
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
                    if (this.writing)
                    {
                        this.bufferQueue.Enqueue(new CloseByteBuffer(this));
                    }
                    else
                    {
                        this.transport.Close();
                    }
                }
            }
        }

        async void WriteAsync()
        {
            const int maxBatchSize = 128 * 1024;

            List<ByteBuffer> buffers = new List<ByteBuffer>();
            while (true)
            {
                ByteBuffer buffer = null;
                int size = 0;

                lock (this.SyncRoot)
                {
                    while (size < maxBatchSize && this.bufferQueue.Count > 0)
                    {
                        ByteBuffer item = this.bufferQueue.Dequeue();
                        if (item.Length == 0)   // special buffer
                        {
                            buffer = item;
                            break;
                        }
                        else
                        {
                            buffers.Add(item);
                            size += item.Length;
                        }
                    }

                    if (size == 0)
                    {
                        this.writing = false;
                        if (buffer == null)
                        {
                            break;
                        }
                    }
                }

                try
                {
                    if (size > 0)
                    {
                        await this.transport.SendAsync(buffers, size).ConfigureAwait(false);
                    }
                }
                catch (Exception exception)
                {
                    lock (this.SyncRoot)
                    {
                        this.closed = true;
                        this.writing = false;
                        this.transport.Close();
                        buffers.AddRange(this.bufferQueue);
                        this.bufferQueue.Clear();
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
                    if (buffer != null)
                    {
                        buffer.ReleaseReference();
                    }
                }
            }
        }

        class FlushByteBuffer : ByteBuffer
        {
            readonly TaskCompletionSource<object> tcs;

            public FlushByteBuffer()
                : base(null, 0, 0, 0)
            {
                this.tcs = new TaskCompletionSource<object>();
            }

            public Task Task
            {
                get { return this.tcs.Task; }
            }

            internal override void ReleaseReference()
            {
                this.tcs.TrySetResult(null);
            }
        }

        class CloseByteBuffer : ByteBuffer
        {
            readonly TransportWriter writer;

            public CloseByteBuffer(TransportWriter writer)
                : base(null, 0, 0, 0)
            {
                this.writer = writer;
            }

            internal override void ReleaseReference()
            {
                this.writer.transport.Close();
            }
        }
    }
}