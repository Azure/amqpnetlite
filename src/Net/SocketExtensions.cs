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
    using System.Runtime.InteropServices;

    static class SocketExtensions
    {
        public static void SetTcpKeepAlive(this Socket socket, TcpKeepAliveSettings settings)
        {
            /* the native structure
            struct tcp_keepalive {
            ULONG onoff;
            ULONG keepalivetime;
            ULONG keepaliveinterval;
            };
            */

            ulong keepAliveTime = settings.KeepAliveTime;
            ulong keepAliveInterval = settings.KeepAliveInterval;

            int bytesPerULong = 8;
            byte[] inOptionValues = new byte[bytesPerULong * 3];
            BitConverter.GetBytes((ulong)1).CopyTo(inOptionValues, 0);
            BitConverter.GetBytes(keepAliveTime).CopyTo(inOptionValues, bytesPerULong);
            BitConverter.GetBytes(keepAliveInterval).CopyTo(inOptionValues, bytesPerULong * 2);

            socket.IOControl(IOControlCode.KeepAliveValues, inOptionValues, null);
        }

        public static void Complete<T>(object sender, SocketAsyncEventArgs args, bool throwOnError, T result)
        {
            var tcs = (TaskCompletionSource<T>)args.UserToken;
            args.UserToken = null;
            if (args.SocketError != SocketError.Success && throwOnError)
            {
                tcs.SetException(new SocketException((int)args.SocketError));
            }
            else
            {
                tcs.SetResult(result);
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
