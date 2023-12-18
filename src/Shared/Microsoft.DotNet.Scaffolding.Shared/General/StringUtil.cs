// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;

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
            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(value))
            {
                return false;
            }

            return (input?.ToLowerInvariant().Contains(value?.ToLowerInvariant())).GetValueOrDefault();
        }

        //converts Project.Namespace.SubNamespace to Project//Namespace//SubNamespace or Project\\Namespace\\SubNamespace (based on OS)
        public static string ToPath(string input, string basePath)
        {
            string path = string.Empty;
            if (!string.IsNullOrEmpty(basePath) && !string.IsNullOrEmpty(input) && input.Contains("."))
            {
                input = input.Replace(".", Path.DirectorySeparatorChar.ToString());
                try
                {
                    basePath = Path.GetDirectoryName(basePath);
                    var combinedPath = Path.Combine(basePath, input);
                    path = Path.GetFullPath(combinedPath);
                }
                //invalid path
                catch (Exception ex) when (ex is ArgumentException || ex is PathTooLongException || ex is NotSupportedException)
                {}
            }

            return path;
        }

        public static string ToNamespace(string path)
        {
            string ns = path;
            if (!string.IsNullOrEmpty(ns))
            {
                ns = Path.ChangeExtension(ns, null);
                if (ns.Contains(Path.DirectorySeparatorChar))
                {
                    ns = ns.Replace(Path.DirectorySeparatorChar.ToString(), ".");
                }
            }

            return ns;
        }
    }
}
