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
    using System.Collections.Generic;
    using System.Reflection;

    static partial class TypeExtensions
    {
#if NETFX || NETFX40 || NETFX35
        internal static Assembly Assembly(this Type type)
        {
            return type.Assembly;
        }

        internal static Type BaseType(this Type type)
        {
            return type.BaseType;
        }

        internal static bool IsValueType(this Type type)
        {
            return type.IsValueType;
        }

        internal static bool IsEnum(this Type type)
        {
            return type.IsEnum;
        }

        internal static bool IsGenericType(this Type type)
        {
            return type.IsGenericType;
        }

        internal static object CreateInstance(this Type type, bool hasDefaultCtor)
        {
            return hasDefaultCtor ?
                Activator.CreateInstance(type) :
                System.Runtime.Serialization.FormatterServices.GetUninitializedObject(type);
        }
#endif
#if NETFX35 || NETFX40
        internal static T GetCustomAttribute<T>(this MemberInfo mi, bool inherit)
        {
            object[] a = mi.GetCustomAttributes(typeof(T), inherit);
            return a.Length == 0 ? default(T) : (T)a[0];
        }

        internal static IEnumerable<T> GetCustomAttributes<T>(this MemberInfo mi, bool inherit)
        {
            object[] a = mi.GetCustomAttributes(typeof(T), inherit);
            return Array.ConvertAll<object, T>(a, obj => (T)obj);
        }
#endif
#if DOTNET
        internal static Assembly Assembly(this Type type)
        {
            return type.GetTypeInfo().Assembly;
        }

        internal static Type BaseType(this Type type)
        {
            return type.GetTypeInfo().BaseType;
        }
#endif
#if DOTNET_SERIALIZATION
        internal static T GetCustomAttribute<T>(this Type type, bool inherit) where T : Attribute
        {
            return type.GetTypeInfo().GetCustomAttribute<T>(inherit);
        }

        internal static IEnumerable<T> GetCustomAttributes<T>(this Type type, bool inherit) where T : Attribute
        {
            return type.GetTypeInfo().GetCustomAttributes<T>(inherit);
        }

        internal static bool IsValueType(this Type type)
        {
            return type.GetTypeInfo().IsValueType;
        }

        internal static bool IsEnum(this Type type)
        {
            return type.GetTypeInfo().IsEnum;
        }

        internal static bool IsGenericType(this Type type)
        {
            return type.GetTypeInfo().IsGenericType;
        }

        internal static bool IsAssignableFrom(this Type type, Type from)
        {
            return type.GetTypeInfo().IsAssignableFrom(from.GetTypeInfo());
        }

        internal static object CreateInstance(this Type type, bool hasDefaultCtor)
        {
            return Activator.CreateInstance(type);
        }
#endif
    }
}