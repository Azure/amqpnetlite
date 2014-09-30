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
    using Amqp.Framing;
    using Amqp.Types;
    using System;

    abstract class SaslProfile
    {
        public void Open(string hostname, ITransport transport)
        {
            ProtocolHeader myHeader = this.Start(hostname, transport);

            ProtocolHeader theirHeader = Reader.ReadHeader(transport);
            Trace.WriteLine(TraceLevel.Frame, "RECV AMQP {0}", theirHeader);
            this.OnHeader(myHeader, theirHeader);

            SaslCode code = SaslCode.SysTemp;
            while (true)
            {
                ByteBuffer buffer = Reader.ReadFrameBuffer(transport, new byte[4], uint.MaxValue);
                if (buffer == null)
                {
                    throw new ObjectDisposedException(transport.GetType().Name);
                }

                if (!this.OnFrame(transport, buffer, out code))
                {
                    break;
                }
            }

            if (code != SaslCode.Ok)
            {
                throw new AmqpException(ErrorCode.UnauthorizedAccess,
                    Fx.Format(SRAmqp.SaslNegoFailed, code));
            }
        }

        public ProtocolHeader Start(string hostname, ITransport transport)
        {
            ProtocolHeader myHeader = new ProtocolHeader() { Id = 3, Major = 1, Minor = 0, Revision = 0 };

            ByteBuffer headerBuffer = new ByteBuffer(
                new byte[] { (byte)'A', (byte)'M', (byte)'Q', (byte)'P', myHeader.Id, myHeader.Major, myHeader.Minor, myHeader.Revision },
                0,
                8,
                8);
            transport.Send(headerBuffer);
            Trace.WriteLine(TraceLevel.Frame, "SEND AMQP {0}", myHeader);

            SaslInit init = this.GetInit(hostname);
            this.SendCommand(transport, init);
           
            return myHeader;
        }

        public void OnHeader(ProtocolHeader myHeader, ProtocolHeader theirHeader)
        {
            if (theirHeader.Id != myHeader.Id || theirHeader.Major != myHeader.Major ||
                theirHeader.Minor != myHeader.Minor || theirHeader.Revision != myHeader.Revision)
            {
                throw new AmqpException(ErrorCode.NotImplemented, theirHeader.ToString());
            }
        }

        public bool OnFrame(ITransport transport, ByteBuffer buffer, out SaslCode code)
        {
            ushort channel;
            DescribedList command;
            Frame.GetFrame(buffer, out channel, out command);
            Trace.WriteLine(TraceLevel.Frame, "RECV {0}", command);

            bool shouldContinue = true; ;
            code = SaslCode.Ok;
            if (command.Descriptor.Code == Codec.SaslMechanisms.Code)
            {
            }
            else if (command.Descriptor.Code == Codec.SaslChallenge.Code)
            {
                SaslResponse response = this.OnChallenge((SaslChallenge)command);
                this.SendCommand(transport, response);
            }
            else if (command.Descriptor.Code == Codec.SaslOutcome.Code)
            {
                SaslOutcome outcome = (SaslOutcome)command;
                this.OnOutcome(outcome);
                code = outcome.Code;
                shouldContinue = false;
            }
            else
            {
                throw new AmqpException(ErrorCode.NotImplemented, command.Descriptor.Name);
            }

            return shouldContinue;
        }

        protected abstract SaslInit GetInit(string hostname);

        protected abstract SaslResponse OnChallenge(SaslChallenge challenge);

        protected abstract void OnOutcome(SaslOutcome outcome);

        void SendCommand(ITransport transport, DescribedList command)
        {
            ByteBuffer buffer = Frame.GetBuffer(FrameType.Sasl, 0, command, 128, 0);
            transport.Send(buffer);
            Trace.WriteLine(TraceLevel.Frame, "SEND {0}", command);
        }
    }
}