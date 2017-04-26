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
    using System.Text;

    abstract class Arguments
    {
        public bool HasHelp
        {
            get;
            protected set;
        }

        protected Arguments(string[] args, int offset)
        {
            var dict = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            for (int i = offset; i < args.Length; i++)
            {
                int j = 0;
                while (j < args[i].Length && args[i][j] == '-') j++;
                if (j == 0 || j == args[i].Length)
                {
                    throw new ArgumentException(args[i]);
                }

                string option = args[i].Substring(j);
                List<string> value = new List<string>();
                while (i < args.Length - 1 && args[i + 1][0] != '-')
                {
                    value.Add(args[++i]);
                }

                dict[option] = value;
            }

            var properties = this.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var prop in properties)
            {
                var attribute = (ArgumentAttribute)prop.GetCustomAttribute(typeof(ArgumentAttribute));
                if (attribute != null)
                {
                    List<string> value;
                    if ((attribute.Name != null && dict.TryGetValue(attribute.Name, out value)) ||
                        (attribute.Shortcut != null && dict.TryGetValue(attribute.Shortcut, out value)))
                    {
                        prop.SetValue(this, Convert(prop.PropertyType, value));
                    }
                    else if (IsHelp(attribute.Name))
                    {
                        this.HasHelp = true;
                    }
                    else if (attribute.Default != null)
                    {
                        prop.SetValue(this, attribute.Default);
                    }
                }
            }
        }

        public static string PrintArguments(Type type)
        {
            StringBuilder sb = new StringBuilder();
            Stack<Type> stack = new Stack<Type>();
            while (type != null)
            {
                stack.Push(type);
                type = type.BaseType;
            }

            while (stack.Count > 0)
            {
                type = stack.Pop();

                var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                foreach (var prop in properties)
                {
                    var attribute = (ArgumentAttribute)prop.GetCustomAttribute(typeof(ArgumentAttribute));
                    if (attribute != null)
                    {
                        int width = 0;
                        if (attribute.Name != null)
                        {
                            sb.Append('-');
                            sb.Append('-');
                            sb.Append(attribute.Name);
                            width += attribute.Name.Length + 2;
                        }

                        if (attribute.Shortcut != null)
                        {
                            sb.Append(' ');
                            sb.Append('(');
                            sb.Append('-');
                            sb.Append(attribute.Shortcut);
                            sb.Append(')');
                            width += attribute.Shortcut.Length + 4;
                        }

                        if (width < 20)
                        {
                            sb.Append(' ', 20 - width);
                            width = 20;
                        }
                        else
                        {
                            sb.Append(' ');
                            width++;
                        }

                        if (attribute.Description != null)
                        {
                            sb.Append(attribute.Description);
                        }

                        if (attribute.Default != null)
                        {
                            sb.AppendLine();
                            sb.Append(' ', width);
                            sb.Append("default: ");
                            sb.Append(attribute.Default);
                        }

                        sb.AppendLine();
                    }
                }
            }

            return sb.ToString();
        }

        static bool IsHelp(string value)
        {
            return value.Equals("--help", StringComparison.OrdinalIgnoreCase) ||
                    value.Equals("-h", StringComparison.OrdinalIgnoreCase) ||
                    value.Equals("-?");
        }

        static object Convert(Type type, List<string> value)
        {
            object obj;
            if (type == typeof(string))
            {
                obj = value[0];
            }
            else if (type == typeof(int))
            {
                obj = int.Parse(value[0]);
            }
            else if (type == typeof(long))
            {
                obj = long.Parse(value[0]);
            }
            else if (type == typeof(bool))
            {
                return true;
            }
            else if (type.IsAssignableFrom(typeof(List<string>)))
            {
                return value;
            }
            else
            {
                throw new NotSupportedException(type.Name);
            }

            return obj;
        }
    }
}
