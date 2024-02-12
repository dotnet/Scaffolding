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
        public static string ToPath(string namespaceName, string basePath, string projectRootNamespace)
        {
            string path = string.Empty;
            if (!string.IsNullOrEmpty(basePath) && !string.IsNullOrEmpty(namespaceName))
            {
                namespaceName = RemovePrefix(namespaceName, basePath, projectRootNamespace);
                namespaceName = namespaceName.Replace(".", Path.DirectorySeparatorChar.ToString());
                try
                {
                    basePath = Path.HasExtension(basePath) ? Path.GetDirectoryName(basePath) : basePath;
                    var combinedPath = Path.Combine(basePath, namespaceName);
                    path = Path.GetFullPath(combinedPath);
                }
                //invalid path
                catch (Exception ex) when (ex is ArgumentException || ex is PathTooLongException || ex is NotSupportedException)
                {}
            }

            return path;
        }

        //remove prefix from namespace, used to remove the project name from namespace when creating the path
        public static string RemovePrefix(string projectNamespace, string basePath, string prefix)
        {
            string[] namespaceParts = projectNamespace.Split('.');
            string[] basePathParts = basePath.Split(new char[] { Path.DirectorySeparatorChar } , StringSplitOptions.RemoveEmptyEntries);
            if (namespaceParts.Length > 0 && namespaceParts[0] == prefix && basePathParts[basePathParts.Length - 1] == prefix)
            {
                projectNamespace = string.Join(".", namespaceParts, 1, namespaceParts.Length - 1);
            }

            return projectNamespace;
        }

        /// <summary>
        /// expecting unrooted paths (no drive letter)
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string ToNamespace(string path)
        {
            string ns = path ?? "";
            if (!string.IsNullOrEmpty(ns))
            {
                ns = Path.HasExtension(ns) ? Path.GetDirectoryName(ns) : ns;
                if (ns.Contains(Path.DirectorySeparatorChar))
                {
                    ns = ns.Replace(Path.DirectorySeparatorChar.ToString(), ".");
                }

                if (ns.Last() == '.')
                {
                    ns = ns.Remove(ns.Length - 1);
                }
            }

            return ns;
        }

        /// <summary>
        /// get the full file path without extension, not just the file name
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string GetFilePathWithoutExtension(string input)
        {
            string path = string.Empty;
            if (!string.IsNullOrEmpty(input))
            {
                path = Path.ChangeExtension(input, null);
                if (path.Contains(Path.DirectorySeparatorChar))
                {
                    path = path.Replace(Path.DirectorySeparatorChar.ToString(), ".").TrimEnd();
                }

                if (path.EndsWith("."))
                {
                    path = path.Remove(path.Length - 1);
                }
            }
            return path;
        }

        internal static string GetTypeNameFromNamespace(string templateName)
        {
            string[] parts = templateName.Split('.');
            return parts[parts.Length - 1];
        }

        internal static string NormalizeLineEndings(string text)
        {
            //change all line endings to "\n" and then replace them with the appropriate ending
            return text.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", Environment.NewLine);
        }
    }
}
