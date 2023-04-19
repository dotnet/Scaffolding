// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Templating
{
    public class TypeUtilities
    {
        public static bool IsTypePrimitive(Type type) => IsInteger(type) || IsNonIntegerPrimitive(type);

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

        public static bool IsNullable(Type t)
        {
            if (Nullable.GetUnderlyingType(t) == null
              && (t.IsPrimitive || t.IsValueType))
            {
                return false;
            }

            return true;
        }
    }
}
