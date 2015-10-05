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

namespace Amqp.Sasl
{
    using System;
    using Amqp.Framing;
    using Amqp.Types;

    /// <summary>
    /// The SaslProfile is the base class of an SASL profile implementation. It provides
    /// the basic support for frame exchange during SASL authentication.
    /// </summary>
    public abstract class SaslProfile
    {
        /// <summary>
        /// The SASL EXTERNAL profile.
        /// </summary>
        public static SaslProfile External
        {
            get { return new SaslExternalProfile(); }
        }

        internal ITransport Open(string hostname, ITransport transport)
        {
#if SMALL_MEMORY
            ProtocolHeader myHeader = this.Start(hostname, ref transport);
#else
            ProtocolHeader myHeader = this.Start(hostname, transport);
#endif

            ProtocolHeader theirHeader = Reader.ReadHeader(transport);
#if !SMALL_MEMORY
            Trace.WriteLine(TraceLevel.Frame, "RECV AMQP {0}", theirHeader);
#endif
            this.OnHeader(myHeader, theirHeader);

            SaslCode code = SaslCode.SysTemp;
            while (true)
            {
                ByteBuffer buffer = Reader.ReadFrameBuffer(transport, new byte[4], uint.MaxValue);
                if (buffer == null)
                {
                    throw new ObjectDisposedException(transport.GetType().Name);
                }

#if NETMF
                //Microsoft.SPOT.Debug.GC(true);
#endif

#if SMALL_MEMORY
                if (!this.OnFrame(ref transport, ref buffer, out code))
#else
                if (!this.OnFrame(transport, buffer, out code))
#endif
                {
                    break;
                }
            }

            if (code != SaslCode.Ok)
            {
#if SMALL_MEMORY
                throw new AmqpException(ErrorCode.SaslNegoFailed, code.ToString());
#else
                throw new AmqpException(ErrorCode.UnauthorizedAccess,
                    Fx.Format(SRAmqp.SaslNegoFailed, code));
#endif
            }

            return this.UpgradeTransport(transport);
        }

#if SMALL_MEMORY
        internal ProtocolHeader Start(string hostname, ref ITransport transport)
#else
        internal ProtocolHeader Start(string hostname, ITransport transport)
#endif
        {
            ProtocolHeader myHeader = new ProtocolHeader() { Id = 3, Major = 1, Minor = 0, Revision = 0 };


            ByteBuffer headerBuffer = new ByteBuffer(
                new byte[] { (byte)'A', (byte)'M', (byte)'Q', (byte)'P', myHeader.Id, myHeader.Major, myHeader.Minor, myHeader.Revision },
                0,
                8,
                8);
#if SMALL_MEMORY
            transport.Send(ref headerBuffer);
#else
            transport.Send(headerBuffer);
            Trace.WriteLine(TraceLevel.Frame, "SEND AMQP {0}", myHeader);
#endif

            DescribedList command = this.GetStartCommand(hostname);
            if (command != null)
            {
                this.SendCommand(transport, command);
            }

            return myHeader;
        }

        internal void OnHeader(ProtocolHeader myHeader, ProtocolHeader theirHeader)
        {
            if (theirHeader.Id != myHeader.Id || theirHeader.Major != myHeader.Major ||
                theirHeader.Minor != myHeader.Minor || theirHeader.Revision != myHeader.Revision)
            {
                throw new AmqpException(ErrorCode.NotImplemented, theirHeader.ToString());
            }
        }

#if SMALL_MEMORY
        internal bool OnFrame(ref ITransport transport, ref ByteBuffer buffer, out SaslCode code)
#else
        internal bool OnFrame(ITransport transport, ByteBuffer buffer, out SaslCode code)
#endif
        {
            ushort channel;
            DescribedList command;
#if SMALL_MEMORY
            Frame.GetFrame(ref buffer, out channel, out command);
#else
            Frame.GetFrame(buffer, out channel, out command);
#endif

#if !SMALL_MEMORY
            Trace.WriteLine(TraceLevel.Frame, "RECV {0}", command);
#endif

            bool shouldContinue = true;
            if (command.Descriptor.Code == Codec.SaslOutcome.Code)
            {
                code = ((SaslOutcome)command).Code;
                shouldContinue = false;
            }
            else
            {
                code = SaslCode.Ok;
                DescribedList response = this.OnCommand(command);
                if (response != null)
                {
                    this.SendCommand(transport, response);
                    shouldContinue = response.Descriptor.Code != Codec.SaslOutcome.Code;
                }
            }

            return shouldContinue;
        }

#if SMALL_MEMORY
        internal DescribedList OnCommandInternal(ref DescribedList command)
#else
        internal DescribedList OnCommandInternal(DescribedList command)
#endif
        {
            return this.OnCommand(command);
        }

#if SMALL_MEMORY
        internal ITransport UpgradeTransportInternal(ref ITransport transport)
#else
        internal ITransport UpgradeTransportInternal(ITransport transport)
#endif
        {
            return this.UpgradeTransport(transport);
        }

        /// <summary>
        /// If a profile needs to change the buffer (e.g. encryption), it should
        /// create a new ITransport object. Otherwise, it can simply return the
        /// same transport object.
        /// </summary>
        /// <param name="transport">The current transport.</param>
        /// <returns></returns>
        protected abstract ITransport UpgradeTransport(ITransport transport);

        /// <summary>
        /// The start SASL command.
        /// </summary>
        /// <param name="hostname">The hostname of the remote peer.</param>
        /// <returns></returns>
        protected abstract DescribedList GetStartCommand(string hostname);

        /// <summary>
        /// Processes the received command and returns a response. If returns
        /// null, the SASL handshake completes.
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        protected abstract DescribedList OnCommand(DescribedList command);

        void SendCommand(ITransport transport, DescribedList command)
        {
            ByteBuffer buffer = Frame.Encode(FrameType.Sasl, 0, command);
#if SMALL_MEMORY
            transport.Send(ref buffer);
#else
            transport.Send(buffer);
            Trace.WriteLine(TraceLevel.Frame, "SEND {0}", command);
#endif
        }
    }
}
