// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Framework.CodeGeneration.EntityFramework
{
    internal static class TypeUtil
    {
        private static readonly Dictionary<Type, string> _knownDefaultValues = new Dictionary<Type, string>
        {
            { typeof(sbyte), "0" },
            { typeof(byte), "0" },
            { typeof(short), "0" },
            { typeof(ushort), "0" },
            { typeof(int), "0" },
            { typeof(uint), "0" },
            { typeof(long), "0" },
            { typeof(ulong), "0" },
            { typeof(float), "0.0f" },
            { typeof(double), "0.0" },
            { typeof(bool), "false" },
            { typeof(char), "'\0'" },
            { typeof(decimal), "0" }
        };

        public static string GetDefaultValue(Type type)
        {
            string value;
            if (_knownDefaultValues.TryGetValue(type, out value))
            {
                return value;
            }
            return "null";
        }
    }
}