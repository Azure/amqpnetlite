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
    interface INode
    {
        INode Next { get; set; }

        INode Previous { get; set; }
    }

    class LinkedList
    {
        INode head;
        INode tail;

        public INode First { get { return this.head; } }

        public static void Add(ref INode head, ref INode tail, INode node)
        {
            Fx.Assert(node.Previous == null && node.Next == null, "node is already in a list");
            if (head == null)
            {
                Fx.Assert(tail == null, "tail must be null");
                head = tail = node;
            }
            else
            {
                Fx.Assert(tail != null, "tail must not be null");
                tail.Next = node;
                node.Previous = tail;
                tail = node;
            }
        }

        public static void Remove(ref INode head, ref INode tail, INode node)
        {
            Fx.Assert(node != null, "node cannot be null");
            if (node == head)
            {
                head = node.Next;
                if (head == null)
                {
                    tail = null;
                }
                else
                {
                    head.Previous = null;
                }
            }
            else if (node == tail)
            {
                tail = node.Previous;
                tail.Next = null;
            }
            else if (node.Previous != null && node.Next != null)
            {
                // remove middle
                node.Previous.Next = node.Next;
                node.Next.Previous = node.Previous;
            }

            node.Previous = node.Next = null;
        }

        public void Add(INode node)
        {
            Add(ref this.head, ref this.tail, node);
        }

        public void Remove(INode node)
        {
            Remove(ref this.head, ref this.tail, node);
        }

        public INode Clear()
        {
            INode first = this.head;
            this.head = this.tail = null;
            return first;
        }
    }
}