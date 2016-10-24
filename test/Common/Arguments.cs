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

namespace Test.Common
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    abstract class Arguments
    {
        protected Arguments(string[] args, int offset)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            for (int i = offset; i < args.Length; i++)
            {
                int j = 0;
                while (j < args[i].Length && args[i][j] == '-') j++;
                if (j == 0 || j == args[i].Length)
                {
                    throw new ArgumentException(args[i]);
                }

                string option = args[i].Substring(j);
                string value = null;
                if (i < args.Length - 1 && args[i + 1][0] != '-')
                {
                    value = args[++i];
                }

                dict[option] = value;
            }

            var properties = this.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var prop in properties)
            {
                var attribute = (ArgumentAttribute)prop.GetCustomAttribute(typeof(ArgumentAttribute));
                if (attribute != null)
                {
                    string value;
                    if ((attribute.Name != null && dict.TryGetValue(attribute.Name, out value)) ||
                        (attribute.Shortcut != null && dict.TryGetValue(attribute.Shortcut, out value)))
                    {
                        prop.SetValue(this, Convert(prop.PropertyType, value));
                    }
                    else if (attribute.Default != null)
                    {
                        prop.SetValue(this, attribute.Default);
                    }
                }
            }
        }

        public static bool IsHelp(string value)
        {
            return value.Equals("--help", StringComparison.OrdinalIgnoreCase) ||
                    value.Equals("-h", StringComparison.OrdinalIgnoreCase) ||
                    value.Equals("-?");
        }

        static object Convert(Type type, string value)
        {
            object obj;
            if (type == typeof(string))
            {
                obj = value;
            }
            else if (type == typeof(int))
            {
                obj = int.Parse(value);
            }
            else if (type == typeof(bool))
            {
                return true;
            }
            else
            {
                throw new NotSupportedException(type.Name);
            }

            return obj;
        }
    }
}
