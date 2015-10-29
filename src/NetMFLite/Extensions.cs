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
    using Amqp.Types;
    using Microsoft.SPOT.Net.Security;

    static class Extensions
    {
        public const int TransferFramePrefixSize = 29;

        // Frame extensions

        public static void WriteFrame(this NetworkStream stream, byte frameType, ushort channel, ulong code, List fields)
        {
            ByteBuffer buffer = new ByteBuffer(64, true);

            // frame header
            buffer.Append(FixedWidth.UInt);
            AmqpBitConverter.WriteUByte(buffer, 2);
            AmqpBitConverter.WriteUByte(buffer, (byte)frameType);
            AmqpBitConverter.WriteUShort(buffer, channel);

            // command
            AmqpBitConverter.WriteUByte(buffer, FormatCode.Described);
            Encoder.WriteULong(buffer, code, true);
            AmqpBitConverter.WriteUByte(buffer, FormatCode.List32);
            int sizeOffset = buffer.WritePos;
            buffer.Append(8);
            AmqpBitConverter.WriteInt(buffer.Buffer, sizeOffset + 4, fields.Count);
            for (int i = 0; i < fields.Count; i++)
            {
                Encoder.WriteObject(buffer, fields[i]);
            }
            AmqpBitConverter.WriteInt(buffer.Buffer, sizeOffset, buffer.Length - sizeOffset);
            AmqpBitConverter.WriteInt(buffer.Buffer, 0, buffer.Length); // frame size
            stream.Write(buffer.Buffer, buffer.Offset, buffer.Length);
        }

        public static void WriteTransferFrame(this NetworkStream stream, uint deliveryId, bool settled,
            ByteBuffer buffer, int maxFrameSize)
        {
            // payload should have bytes reserved for frame header and transfer
            int frameSize = Math.Min(buffer.Length + TransferFramePrefixSize, maxFrameSize);
            int payloadSize = frameSize - TransferFramePrefixSize;
            int offset = buffer.Offset - TransferFramePrefixSize;
            int pos = offset;

            // frame size
            buffer.Buffer[pos++] = (byte)(frameSize >> 24);
            buffer.Buffer[pos++] = (byte)(frameSize >> 16);
            buffer.Buffer[pos++] = (byte)(frameSize >> 8);
            buffer.Buffer[pos++] = (byte)frameSize;

            // DOF, type and channel
            buffer.Buffer[pos++] = 0x02;
            buffer.Buffer[pos++] = 0x00;
            buffer.Buffer[pos++] = 0x00;
            buffer.Buffer[pos++] = 0x00;

            // transfer(list8-size,count)
            buffer.Buffer[pos++] = 0x00;
            buffer.Buffer[pos++] = 0x53;
            buffer.Buffer[pos++] = 0x14;
            buffer.Buffer[pos++] = 0xc0;
            buffer.Buffer[pos++] = 0x10;
            buffer.Buffer[pos++] = 0x06;

            buffer.Buffer[pos++] = 0x43; // handle

            buffer.Buffer[pos++] = 0x70; // delivery id: uint
            buffer.Buffer[pos++] = (byte)(deliveryId >> 24);
            buffer.Buffer[pos++] = (byte)(deliveryId >> 16);
            buffer.Buffer[pos++] = (byte)(deliveryId >> 8);
            buffer.Buffer[pos++] = (byte)deliveryId;

            buffer.Buffer[pos++] = 0xa0; // delivery tag: bin8
            buffer.Buffer[pos++] = 0x04;
            buffer.Buffer[pos++] = (byte)(deliveryId >> 24);
            buffer.Buffer[pos++] = (byte)(deliveryId >> 16);
            buffer.Buffer[pos++] = (byte)(deliveryId >> 8);
            buffer.Buffer[pos++] = (byte)deliveryId;

            buffer.Buffer[pos++] = 0x43; // message-format
            buffer.Buffer[pos++] = settled ? (byte)0x41 : (byte)0x42;   // settled
            buffer.Buffer[pos++] = buffer.Length > payloadSize ? (byte)0x41 : (byte)0x42;   // more

            stream.Write(buffer.Buffer, offset, frameSize);
            buffer.Complete(payloadSize);
        }

        public static void ReadFrame(this NetworkStream stream, out byte frameType, out ushort channel,
            out ulong code, out List fields, out ByteBuffer payload)
        {
            byte[] headerBuffer = stream.ReadFixedSizeBuffer(8);
            int size = AmqpBitConverter.ReadInt(headerBuffer, 0);
            frameType = headerBuffer[5];    // TOOD: header EXT
            channel = (ushort)(headerBuffer[6] << 8 | headerBuffer[7]);

            size -= 8;
            if (size > 0)
            {
                byte[] frameBuffer = stream.ReadFixedSizeBuffer(size);
                ByteBuffer buffer = new ByteBuffer(frameBuffer, 0, size, size);
                Fx.AssertAndThrow(1001, Encoder.ReadFormatCode(buffer) == FormatCode.Described);

                code = Encoder.ReadULong(buffer, Encoder.ReadFormatCode(buffer));
                fields = Encoder.ReadList(buffer,Encoder.ReadFormatCode(buffer));
                if (buffer.Length > 0)
                {
                    payload = new ByteBuffer(buffer.Buffer, buffer.Offset, buffer.Length, buffer.Length);
                }
                else
                {
                    payload = null;
                }
            }
            else
            {
                code = 0;
                fields = null;
                payload = null;
            }
        }

        public static List ReadFrameBody(this NetworkStream stream, byte frameType, ushort channel, ulong code)
        {
            byte t;
            ushort c;
            ulong d;
            List f;
            ByteBuffer p;
            stream.ReadFrame(out t, out c, out d, out f, out p);
            Fx.AssertAndThrow(1002, t == frameType);
            Fx.AssertAndThrow(1003, c == channel);
            Fx.AssertAndThrow(1004, d == code);
            Fx.AssertAndThrow(1005, f != null);
            Fx.AssertAndThrow(1006, p == null);
            return f;
        }

        // Transport extensions

        public static byte[] ReadFixedSizeBuffer(this NetworkStream stream, int size)
        {
            byte[] buffer = new byte[size];
            int offset = 0;
            while (size > 0)
            {
                int bytes = stream.Read(buffer, offset, size);
                offset += bytes;
                size -= bytes;
            }
            return buffer;
        }

        // Helper methods

        public static NetworkStream Connect(string host, int port, bool useSsl)
        {
            var ipHostEntry = Dns.GetHostEntry(host);
            Socket socket = null;
            SocketException exception = null;
            foreach (var ipAddress in ipHostEntry.AddressList)
            {
                if (ipAddress == null) continue;

                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    socket.Connect(new IPEndPoint(ipAddress, port));
                    exception = null;
                    break;
                }
                catch (SocketException socketException)
                {
                    exception = socketException;
                    socket = null;
                }
            }

            if (exception != null)
            {
                throw exception;
            }

            NetworkStream stream;
            if (useSsl)
            {
                SslStream sslStream = new SslStream(socket);
                sslStream.AuthenticateAsClient(host, null, SslVerification.VerifyPeer, SslProtocols.Default);
                stream = sslStream;
            }
            else
            {
                stream = new NetworkStream(socket, true);
            }

            return stream;
        }

        public static Symbol[] GetSymbolMultiple(object multiple)
        {
            Symbol[] array = multiple as Symbol[];
            if (array != null)
            {
                return array;
            }

            Symbol symbol = multiple as Symbol;
            if (symbol != null)
            {
                return new Symbol[] { symbol };
            }

            throw new Exception("object is not a multiple type");
        }
    }
}