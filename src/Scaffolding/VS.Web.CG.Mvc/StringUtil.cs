// Copyright (c) .NET Foundation. All rights reserved.

using System;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc
{
    internal static class StringUtil
    {
        public static string ToLowerInvariantFirstChar(this string input)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            if (input != string.Empty)
            {
                return input.Substring(0, length: 1).ToLowerInvariant() + input.Substring(1);
            }
            return input;
        }
    }
}
