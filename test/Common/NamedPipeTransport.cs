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

using System.Collections.Generic;
using System.IO.Pipes;
using System.Threading.Tasks;
using Amqp;

namespace Test.Amqp
{
    public class NamedPipeTransport : IAsyncTransport
    {
        const string PipeScheme = "pipe";
        PipeStream pipeStream;

        public NamedPipeTransport(PipeStream pipeStream)
        {
            this.pipeStream = pipeStream;
        }

        public static TransportProvider Factory
        {
            get { return new NamedPipeTransportFactory(); }
        }

        public static TransportProvider Listener
        {
            get { return new NamedPipeTransportListener(); }
        }

        void ITransport.Send(ByteBuffer buffer)
        {
            // make it non-blocking
            this.pipeStream.WriteAsync(buffer.Buffer, buffer.Offset, buffer.Length);
        }

        int ITransport.Receive(byte[] buffer, int offset, int count)
        {
            return this.pipeStream.Read(buffer, offset, count);
        }

        void ITransport.Close()
        {
            this.pipeStream.Dispose();
        }

        void IAsyncTransport.SetConnection(Connection connection)
        {
        }

        async Task IAsyncTransport.SendAsync(IList<ByteBuffer> bufferList, int listSize)
        {
            foreach (var buffer in bufferList)
            {
                await this.pipeStream.WriteAsync(buffer.Buffer, buffer.Offset, buffer.Length);
            }
        }

        Task<int> IAsyncTransport.ReceiveAsync(byte[] buffer, int offset, int count)
        {
            return this.pipeStream.ReadAsync(buffer, offset, count);
        }

        class NamedPipeTransportFactory : TransportProvider
        {
            public NamedPipeTransportFactory()
            {
                this.AddressSchemes = new string[] { PipeScheme };
            }

            public override Task<IAsyncTransport> CreateAsync(Address address)
            {
                NamedPipeClientStream client = new NamedPipeClientStream(address.Host, address.Path,
                    PipeDirection.InOut, PipeOptions.Asynchronous);
                client.Connect();

                TaskCompletionSource<IAsyncTransport> tcs = new TaskCompletionSource<IAsyncTransport>();
                tcs.SetResult(new NamedPipeTransport(client));
                return tcs.Task;
            }
        }

        class NamedPipeTransportListener : TransportProvider
        {
            public NamedPipeTransportListener()
            {
                this.AddressSchemes = new string[] { PipeScheme };
            }

            public override async Task<IAsyncTransport> CreateAsync(Address address)
            {
                NamedPipeServerStream server = new NamedPipeServerStream(address.Path, PipeDirection.InOut, 4,
                    PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
#if DOTNET
                await server.WaitForConnectionAsync();
#else
                await Task.Factory.FromAsync(
                    (c, s) => server.BeginWaitForConnection(c, s),
                    (r) => server.EndWaitForConnection(r),
                    null);
#endif
                return new NamedPipeTransport(server);
            }
        }
    }
}
