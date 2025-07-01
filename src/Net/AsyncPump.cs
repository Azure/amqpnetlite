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
    using System.Threading.Tasks;
    using Amqp.Framing;
    using Amqp.Types;

    class AsyncPump
    {
        readonly IBufferManager bufferManager;
        readonly IAsyncTransport transport;

        public AsyncPump(IBufferManager bufferManager, IAsyncTransport transport)
        {
            this.bufferManager = bufferManager;
            this.transport = transport;
        }

        public void Start(Connection connection, Action<Exception> onException = null)
        {
            Task task = this.StartAsync(connection, onException);
        }

        public async Task PumpAsync(uint maxFrameSize,
            Func<ProtocolHeader, bool> onHeader,
            Func<ByteBuffer, bool> onBuffer,
            Func<ByteBuffer, Task<bool>> onBufferAsync = null)
        {
            byte[] header = new byte[FixedWidth.ULong];

            if (onHeader != null)
            {
                // header
                await this.ReceiveBufferAsync(header, 0, FixedWidth.ULong).ConfigureAwait(false);
                Trace.WriteBuffer("RECV {0}", header, 0, header.Length);
                if (!onHeader(ProtocolHeader.Create(header, 0)))
                {
                    return;
                }
            }

            // frames
            while (true)
            {
                await this.ReceiveBufferAsync(header, 0, FixedWidth.UInt).ConfigureAwait(false);
                int frameSize = AmqpBitConverter.ReadInt(header, 0);
                if ((uint)frameSize > maxFrameSize)
                {
                    throw new AmqpException(ErrorCode.InvalidField,
                        Fx.Format(SRAmqp.InvalidFrameSize, frameSize, maxFrameSize));
                }

                ByteBuffer buffer = this.bufferManager.GetByteBuffer(frameSize);

                try
                {
                    Buffer.BlockCopy(header, 0, buffer.Buffer, buffer.Offset, FixedWidth.UInt);
                    await this.ReceiveBufferAsync(buffer.Buffer, buffer.Offset + FixedWidth.UInt, frameSize - FixedWidth.UInt).ConfigureAwait(false);
                    buffer.Append(frameSize);
                    Trace.WriteBuffer("RECV {0}", buffer.Buffer, buffer.Offset, buffer.Length);

                    var pending = onBufferAsync != null ?
                        await onBufferAsync(buffer).ConfigureAwait(false) :
                        onBuffer(buffer);
                    if (!pending)
                    {
                        break;
                    }
                }
                finally
                {
                    buffer.ReleaseReference();
                }
            }
        }

        async Task StartAsync(Connection connection, Action<Exception> onException)
        {
            try
            {
                await this.PumpAsync(connection.MaxFrameSize, connection.OnHeader, connection.OnFrame).ConfigureAwait(false); 
            }
            catch (AmqpException amqpException)
            {
                connection.OnException(amqpException);
                if (onException != null)
                {
                    onException(amqpException);
                }
            }
            catch (Exception exception)
            {
                connection.OnIoException(exception);
                if (onException != null)
                {
                    onException(exception);
                }
            }
        }

        async Task ReceiveBufferAsync(byte[] buffer, int offset, int count)
        {
            while (count > 0)
            {
                int received = await this.transport.ReceiveAsync(buffer, offset, count).ConfigureAwait(false);
                if (received == 0)
                {
                    throw new OperationCanceledException(Fx.Format(SRAmqp.TransportClosed, this.transport.GetType().Name));
                }

                offset += received;
                count -= received;
            }
        }
    }
}
