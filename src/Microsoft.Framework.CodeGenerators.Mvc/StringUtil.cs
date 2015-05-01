// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.CodeGeneration;

namespace Microsoft.Framework.CodeGenerators.Mvc
{
    internal static class StringUtil
    {
        public static string ToLowerInvariantFirstChar([NotNull]this string input)
        {
            if (input != string.Empty)
            {
                return input.Substring(0, length: 1).ToLowerInvariant() + input.Substring(1);
            }
            return input;
        }
    }
}