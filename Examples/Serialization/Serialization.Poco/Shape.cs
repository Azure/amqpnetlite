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

namespace Serialization.Poco
{
    using System;
    using System.Collections.Generic;

    abstract class Shape
    {
        public Guid Id { get; set; }

        public Dictionary<string, object> Attributes { get; set; }
    }

    class Circle : Shape
    {
        public double Radius { get; set; }

        public override string ToString()
        {
            return string.Format("{0}(id={1} r={2})", this.GetType().Name, this.Id, this.Radius);
        }
    }

    class Rectangle : Shape
    {
        public int Width { get; set; }

        public int Height { get; set; }

        public override string ToString()
        {
            return string.Format("{0}(id={1} w={2} h={3})", this.GetType().Name, this.Id, this.Width, this.Height);
        }
    }
}
