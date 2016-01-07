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
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;

    static class SocketExtensions
    {
        public static void Complete<T>(this TaskCompletionSource<T> tcs, SocketAsyncEventArgs args, Func<SocketAsyncEventArgs, T> getResult)
        {
            args.UserToken = null;
            if (args.SocketError != SocketError.Success)
            {
                tcs.SetException(new SocketException((int)args.SocketError));
            }
            else
            {
                tcs.SetResult(getResult(args));
            }
        }

        public static Task ConnectAsync(this Socket socket, IPAddress addr, int port)
        {
            var tcs = new TaskCompletionSource<int>();
            var args = new SocketAsyncEventArgs();
            args.RemoteEndPoint = new IPEndPoint(addr, port);
            args.UserToken = tcs;
            args.Completed += (s, a) => { ((TaskCompletionSource<int>)a.UserToken).Complete(a, _ => 0); a.Dispose(); };
            if (!socket.ConnectAsync(args))
            {
                tcs.Complete(args, _ => 0);
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
                tcs.Complete<int>(args, a => a.BytesTransferred);
            }

            return tcs.Task;
        }

        public static Task<Socket> AcceptAsync(this Socket socket, SocketAsyncEventArgs args, SocketFlags flags)
        {
            var tcs = new TaskCompletionSource<Socket>();
            args.UserToken = tcs;
            if (!socket.AcceptAsync(args))
            {
                tcs.Complete(args, a => a.AcceptSocket);
            }

            return tcs.Task;
        }
    }
}
