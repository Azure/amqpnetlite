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
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;

    static class SocketExtensions
    {
        public static void SetTcpKeepAlive(this Socket socket, TcpKeepAliveSettings settings)
        {
#if NETSTANDARD2_0
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

            // the keep-alive option names are not available in netstandard2.0.
            // but it seems that the value is passed to the PAL implementation
            // so it should work if the runtime supports it.
            // 3 = TcpKeepAliveTime, 17 = TcpKeepAliveInterval
            socket.SetSocketOption(SocketOptionLevel.Tcp, (SocketOptionName)3, (int)settings.KeepAliveTime);
            socket.SetSocketOption(SocketOptionLevel.Tcp, (SocketOptionName)17, (int)settings.KeepAliveInterval);
#else
            // This is only supported on Windows

            /* the native structure
            struct tcp_keepalive {
            ULONG onoff;
            ULONG keepalivetime;
            ULONG keepaliveinterval;
            };
            
            ULONG is an unsigned 32 bit integer
            */

            int bytesPerUInt = 4;
            byte[] inOptionValues = new byte[bytesPerUInt * 3];
            
            BitConverter.GetBytes((uint)1).CopyTo(inOptionValues, 0);
            BitConverter.GetBytes((uint)settings.KeepAliveTime).CopyTo(inOptionValues, bytesPerUInt);
            BitConverter.GetBytes((uint)settings.KeepAliveInterval).CopyTo(inOptionValues, bytesPerUInt * 2);

            socket.IOControl(IOControlCode.KeepAliveValues, inOptionValues, null);
#endif
        }

        public static void Complete<T>(object sender, SocketAsyncEventArgs args, bool throwOnError, T result)
        {
            var tcs = (TaskCompletionSource<T>)args.UserToken;
            args.UserToken = null;
            if (tcs == null)
            {
                return;
            }

            if (args.SocketError != SocketError.Success && throwOnError)
            {
                tcs.TrySetException(new SocketException((int)args.SocketError));
            }
            else
            {
                tcs.TrySetResult(result);
            }
        }

        public static Task ConnectAsync(this Socket socket, IPAddress addr, int port)
        {
            var tcs = new TaskCompletionSource<int>();
            var args = new SocketAsyncEventArgs();
            args.RemoteEndPoint = new IPEndPoint(addr, port);
            args.UserToken = tcs;
            args.Completed += (s, a) => { Complete(s, a, true, 0); a.Dispose(); };
            if (!socket.ConnectAsync(args))
            {
                Complete(socket, args, true, 0);
                args.Dispose();
            }

            return tcs.Task;
        }

        public static Task<int> ReceiveAsync(this Socket socket, SocketAsyncEventArgs args, byte[] buffer, int offset, int count)
        {
            var tcs = new TaskCompletionSource<int>();
            args.SetBuffer(buffer, offset, count);
            args.UserToken = tcs;
            if (!socket.ReceiveAsync(args))
            {
                Complete(socket, args, true, args.BytesTransferred);
            }

            return tcs.Task;
        }

        public static Task<int> SendAsync(this Socket socket, SocketAsyncEventArgs args, IList<ArraySegment<byte>> buffers)
        {
            var tcs = new TaskCompletionSource<int>();
            args.SetBuffer(null, 0, 0);
            args.BufferList = buffers;
            args.UserToken = tcs;
            if (!socket.SendAsync(args))
            {
                Complete(socket, args, true, 0);
            }

            return tcs.Task;
        }

        public static Task<Socket> AcceptAsync(this Socket socket, SocketAsyncEventArgs args, SocketFlags flags)
        {
            var tcs = new TaskCompletionSource<Socket>();
            args.UserToken = tcs;
            if (!socket.AcceptAsync(args))
            {
                Complete(socket, args, false, args.AcceptSocket);
            }

            return tcs.Task;
        }
    }
}
