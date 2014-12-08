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

namespace Amqp.Transactions
{
    using System;
    using System.Threading.Tasks;
    using Amqp;
    using Amqp.Framing;
    using Amqp.Types;

    sealed class Controller : SenderLink
    {
        public static readonly Descriptor Coordinator = new Descriptor(0x0000000000000030, "amqp:coordinator:list");
        public static readonly Descriptor Declare = new Descriptor(0x0000000000000031, "amqp:declare:list");
        public static readonly Descriptor Discharge = new Descriptor(0x0000000000000032, "amqp:discharge:list");
        public static readonly Descriptor Declared = new Descriptor(0x0000000000000033, "amqp:declared:list");
        public static readonly Descriptor TransactionalState = new Descriptor(0x0000000000000034, "amqp:transactional-state:list");

        static Controller()
        {
            Encoder.AddKnownDescribed(Coordinator, () => new Coordinator());
            Encoder.AddKnownDescribed(Declare, () => new Declare());
            Encoder.AddKnownDescribed(Discharge, () => new Discharge());
            Encoder.AddKnownDescribed(Declared, () => new Declared());
            Encoder.AddKnownDescribed(TransactionalState, () => new TransactionalState());
        }

        public Controller(Session session)
            : base(session, GetName(), new Coordinator(), new Source())
        {
        }

        public Task<byte[]> DeclareAsync()
        {
            Message message = new Message(new Declare());
            TaskCompletionSource<byte[]> tcs = new TaskCompletionSource<byte[]>();
            this.Send(message, null, OnOutcome, tcs);
            return tcs.Task;
        }

        public Task DischargeAsync(byte[] txnId, bool fail)
        {
            Message message = new Message(new Discharge() { TxnId = txnId, Fail = fail });
            TaskCompletionSource<byte[]> tcs = new TaskCompletionSource<byte[]>();
            this.Send(message, null, OnOutcome, tcs);
            return tcs.Task;
        }

        static string GetName()
        {
            return "controller-link-" + Guid.NewGuid().ToString("N").Substring(0, 5);
        }

        static void OnOutcome(Message message, Outcome outcome, object state)
        {
            var tcs = (TaskCompletionSource<byte[]>)state;
            if (outcome.Descriptor.Code == Declared.Code)
            {
                tcs.SetResult(((Declared)outcome).TxnId);
            }
            else if (outcome.Descriptor.Code == Codec.Rejected.Code)
            {
                tcs.SetException(new AmqpException(((Rejected)outcome).Error));
            }
            else
            {
                tcs.SetCanceled();
            }
        }
   }
}
