// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.DotNet.Scaffolding.Shared
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

        /// <summary>
        /// since netstandard2.0 does not have a Contains that allows StringOrdinal param, using lower invariant comparison.
        /// </summary>
        /// <returns>true if lower invariants are equal, false otherwise. false for any null scenario.</returns>
        public static bool ContainsIgnoreCase(this string input, string value)
        {
            return (input?.ToLowerInvariant().Equals(value?.ToLowerInvariant())).GetValueOrDefault();
        }
    }
}
