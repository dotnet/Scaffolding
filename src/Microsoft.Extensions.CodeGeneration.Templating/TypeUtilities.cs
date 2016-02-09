// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;

namespace Microsoft.Extensions.CodeGeneration.Templating
{
    public class TypeUtilities 
    {
        public static bool IsTypePrimitive(Type type)  => IsInteger(type) || IsNonIntegerPrimitive(type);
        
        private static bool IsNonIntegerPrimitive(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;

            return (type == typeof(bool))
                   || (type == typeof(byte[]))
                   || (type == typeof(DateTime))
                   || (type == typeof(DateTimeOffset))
                   || (type == typeof(decimal))
                   || (type == typeof(double))
                   || (type == typeof(float))
                   || (type == typeof(Guid))
                   || (type == typeof(string))
                   || (type == typeof(TimeSpan))
                   || type.GetTypeInfo().IsEnum;
        }
        private static bool IsInteger(Type type) 
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            return (type == typeof(int))
                   || (type == typeof(long))
                   || (type == typeof(short))
                   || (type == typeof(byte))
                   || (type == typeof(uint))
                   || (type == typeof(ulong))
                   || (type == typeof(ushort))
                   || (type == typeof(sbyte))
                   || (type == typeof(char));    
        }
    }    
} 