// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;

namespace Microsoft.Framework.CodeGeneration.Core.Test
{
    internal static class TypeInfoHelper
    {
        public static TypeInfo TypeInfoFromType(this Type type)
        {
#if NET45
            return type.Assembly
                .DefinedTypes
                .Where(typeInfo => typeInfo.AsType() == type)
                .FirstOrDefault();
#else
            return type.GetTypeInfo();
#endif
        }
    }
}