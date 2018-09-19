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
        internal const uint MaxFrameSize = 512;
        internal const string ExternalName = "EXTERNAL";
        internal const string AnonymousName = "ANONYMOUS";
        internal const string PlainName = "PLAIN";

        /// <summary>
        /// Initializes a SaslProfile object.
        /// </summary>
        /// <param name="mechanism">The SASL profile mechanism.</param>
        protected SaslProfile(Symbol mechanism)
        {
            this.Mechanism = mechanism;
        }

        /// <summary>
        /// Gets a SASL ANONYMOUS profile.
        /// </summary>
        public static SaslProfile Anonymous
        {
            get { return new SaslNoActionProfile(AnonymousName, AnonymousName); }
        }

        /// <summary>
        /// Gets a SASL EXTERNAL profile.
        /// </summary>
        public static SaslProfile External
        {
            get { return new SaslNoActionProfile(ExternalName, string.Empty); }
        }

        /// <summary>
        /// Gets the mechanism of the SASL profile.
        /// </summary>
        public Symbol Mechanism
        {
            get;
            private set;
        }

        // this is only used by sync client connection
        internal ITransport Open(string hostname, ITransport transport)
        {
            ProtocolHeader myHeader = this.Start(transport, null);
            ProtocolHeader theirHeader = Reader.ReadHeader(transport);
            Trace.WriteLine(TraceLevel.Frame, "RECV AMQP {0}", theirHeader);
            this.OnHeader(myHeader, theirHeader);

            SaslCode code = SaslCode.SysTemp;
            while (true)
            {
                ByteBuffer buffer = Reader.ReadFrameBuffer(transport, new byte[4], MaxFrameSize);
                if (buffer == null)
                {
                    throw new OperationCanceledException(Fx.Format(SRAmqp.TransportClosed, transport.GetType().Name));
                }

                if (!this.OnFrame(hostname, transport, buffer, out code))
                {
                    break;
                }
            }

            if (code != SaslCode.Ok)
            {
                throw new AmqpException(ErrorCode.UnauthorizedAccess,
                    Fx.Format(SRAmqp.SaslNegoFailed, code));
            }

            return this.UpgradeTransport(transport);
        }

        internal ProtocolHeader Start(ITransport transport, DescribedList command)
        {
            ProtocolHeader myHeader = new ProtocolHeader() { Id = 3, Major = 1, Minor = 0, Revision = 0 };

            ByteBuffer headerBuffer = new ByteBuffer(
                new byte[] { (byte)'A', (byte)'M', (byte)'Q', (byte)'P', myHeader.Id, myHeader.Major, myHeader.Minor, myHeader.Revision },
                0,
                8,
                8);
            transport.Send(headerBuffer);
            Trace.WriteLine(TraceLevel.Frame, "SEND AMQP {0}", myHeader);

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
                throw new AmqpException(ErrorCode.NotImplemented,
                    Fx.Format(SRAmqp.AmqpProtocolMismatch, theirHeader, myHeader));
            }
        }

        internal bool OnFrame(string hostname, ITransport transport, ByteBuffer buffer, out SaslCode code)
        {
            ushort channel;
            DescribedList command;
            Frame.Decode(buffer, out channel, out command);
            Trace.WriteLine(TraceLevel.Frame, "RECV {0}", command);

            bool shouldContinue = true;
            if (command.Descriptor.Code == Codec.SaslOutcome.Code)
            {
                code = ((SaslOutcome)command).Code;
                shouldContinue = false;
            }
            else if (command.Descriptor.Code == Codec.SaslMechanisms.Code)
            {
                code = SaslCode.Ok;
                SaslMechanisms mechanisms = (SaslMechanisms)command;
                Symbol matched = null;
                foreach (var m in mechanisms.SaslServerMechanisms)
                {
                    if (this.Match(m))
                    {
                        this.Mechanism = m;
                        matched = m;
                        break;
                    }
                }

                if (matched == null)
                {
                    throw new AmqpException(ErrorCode.NotImplemented, mechanisms.ToString());
                }

                DescribedList init = this.GetStartCommand(hostname);
                if (init != null)
                {
                    this.SendCommand(transport, init);
                }
            }
            else
            {
                code = SaslCode.Ok;
                DescribedList response = this.OnCommand(command);
                if (response != null)
                {
                    this.SendCommand(transport, response);
                    if (response.Descriptor.Code == Codec.SaslOutcome.Code)
                    {
                        code = ((SaslOutcome)response).Code;
                        shouldContinue = false;
                    }
                }
            }

            return shouldContinue;
        }

        internal DescribedList OnCommandInternal(DescribedList command)
        {
            return this.OnCommand(command);
        }

        internal ITransport UpgradeTransportInternal(ITransport transport)
        {
            return this.UpgradeTransport(transport);
        }

        /// <summary>
        /// Checks if a mechanism is supported.
        /// </summary>
        /// <param name="mechanism">A mechanism supported by the remote peer.</param>
        /// <returns>True if the mechanism is supported; False otherwise.</returns>
        protected virtual bool Match(Symbol mechanism)
        {
            return mechanism.Equals(this.Mechanism);
        }

        /// <summary>
        /// If a profile needs to change the buffer (e.g. encryption), it should
        /// create a new ITransport object. Otherwise, it can simply return the
        /// same transport object.
        /// </summary>
        /// <param name="transport">The current transport.</param>
        /// <returns>A transport upgraded from the current transport per the SASL mechanism.</returns>
        protected abstract ITransport UpgradeTransport(ITransport transport);

        /// <summary>
        /// Gets a SASL command, which is typically a SaslInit command for the client,
        /// or a SaslMechanisms command for the server, to start SASL negotiation.
        /// </summary>
        /// <param name="hostname">The hostname of the remote peer.</param>
        /// <returns>A SASL command to send to the remote peer.</returns>
        protected abstract DescribedList GetStartCommand(string hostname);

        /// <summary>
        /// Processes the received command and returns a response. If returns
        /// null, the SASL handshake completes.
        /// </summary>
        /// <param name="command">The SASL command received from the peer.</param>
        /// <returns>A SASL command as a response to the incoming command.</returns>
        protected abstract DescribedList OnCommand(DescribedList command);

        void SendCommand(ITransport transport, DescribedList command)
        {
            ByteBuffer buffer = new ByteBuffer(Frame.CmdBufferSize, true);
            Frame.Encode(buffer, FrameType.Sasl, 0, command);
            transport.Send(buffer);
            Trace.WriteLine(TraceLevel.Frame, "SEND {0}", command);
        }
    }
}