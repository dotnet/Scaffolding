// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace Microsoft.Extensions.CodeGeneration.EntityFramework
{
    internal static class TypeUtil
    {
        // Taken from list of known keywords : https://msdn.microsoft.com/en-us/library/x53a06bb.aspx
        private static Dictionary<Type, string> _knownTypesList = new Dictionary<Type, string>()
        {
            { typeof(int), "int" },
            { typeof(bool), "bool" },
            { typeof(byte), "byte" },
            { typeof(char), "char" },
            { typeof(decimal), "decimal" },
            { typeof(double), "double" },
            { typeof(float), "float" },
            { typeof(long), "long" },
            { typeof(sbyte), "sbyte" },
            { typeof(short), "short" },
            { typeof(string), "string" },
            { typeof(uint), "uint" },
            { typeof(ulong), "ulong" },
            { typeof(ushort), "ushort" },
        };

        /// <summary>
        /// An approach to get meaninful short type names for a given type in C#.
        /// Handles most known cases, refer to tests in TypeUtilTests.cs
        /// Other potential approach is to use CodeDom (CodeTypeReference)
        /// with the help of RegEx however
        /// CodeDom is not available in CoreClr and hence I chose this approach.
        /// Of course the method only works for C# language.
        /// </summary>
        public static string GetShortTypeName(Type type)
        {
            if (_knownTypesList.ContainsKey(type))
            {
                return _knownTypesList[type];
            }

            var typeInfo = type.GetTypeInfo();

            if (typeInfo.IsValueType)
            {
                var underlyingType = Nullable.GetUnderlyingType(type);
                if (underlyingType != null)
                {
                    return GetShortTypeName(underlyingType) + "?";
                }
            }

            if (type.IsConstructedGenericType)
            {
                Type[] genericArgs = type.GetGenericArguments();
                Debug.Assert(genericArgs.Length > 0);

                StringBuilder result = new StringBuilder();

                var genericName = typeInfo.Name;
                var backTickIndex = genericName.IndexOf('`');
                if (backTickIndex > 0)
                {
                    result.Append(genericName.Substring(0, backTickIndex));
                }
                else
                {
                    result.Append(genericName);
                }

                result.Append("<");
                for (int i = 0; i < genericArgs.Length; i++)
                {
                    result.Append(GetShortTypeName(genericArgs[i]));
                    if (i != (genericArgs.Length - 1))
                    {
                        result.Append(", ");
                    }
                }
                result.Append(">");

                return result.ToString();
            }

            return type.Name;
        }
    }
}
