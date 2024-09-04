// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Diagnostics;

namespace Microsoft.DotNet.Scaffolding.Internal.Extensions;

internal static class StringExtensions
{
    public static bool IsCSharpProject(this string projectFilePath)
    {
        return projectFilePath?.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) == true;
    }

    public static bool IsBinary(this string filePath)
    {
        return
            filePath?.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) == true ||
            filePath?.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) == true;
    }

    public static bool IsSolutionFile(this string filePath)
    {
        return
            filePath?.EndsWith(".sln", StringComparison.OrdinalIgnoreCase) == true;
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

        return (input?.ToLowerInvariant().Contains(value.ToLowerInvariant())).GetValueOrDefault();
    }

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
    /// Helper to make relative paths from physical paths. ie. calculates the relative path from 
    /// basePath to fullPath. Note that if either path is null, fullPath will be returned.
    /// Note that two paths that are equal return ".\".
    /// </summary>
    public static string? MakeRelativePath(this string? fullPath, string? basePath)
    {
        if (string.IsNullOrEmpty(basePath) || string.IsNullOrEmpty(fullPath))
        {
            return fullPath;
        }

        var separator = Path.DirectorySeparatorChar.ToString();
        var tempBasePath = basePath.EnsureTrailingBackslash();
        var tempFullPath = fullPath.EnsureTrailingBackslash();

        string relativePath = string.Empty;

        while (!string.IsNullOrEmpty(tempBasePath))
        {
            if (tempFullPath.StartsWith(tempBasePath, StringComparison.OrdinalIgnoreCase))
            {
                // Since we may have added the trailing slash we have to account for that here
                if (fullPath.Length < tempBasePath.Length)
                {
                    Debug.Assert(
                        tempBasePath.Length - fullPath.Length == 1,
                        "We are at the end. Nothing more to do. Add an empty string to handle case where the paths are equal");
                }
                else
                {
                    relativePath += fullPath.Remove(0, tempBasePath.Length);
                }

                // Two equal paths are relative by .\
                if (string.IsNullOrEmpty(relativePath))
                {
                    relativePath = "." + Path.DirectorySeparatorChar;
                }

                break;
            }
            else
            {
                tempBasePath = tempBasePath.Remove(tempBasePath.Length - 1);
                var nLastIndex = tempBasePath.LastIndexOf(separator, StringComparison.OrdinalIgnoreCase);
                if (-1 != nLastIndex)
                {
                    tempBasePath = tempBasePath.Remove(nLastIndex + 1);
                    relativePath += "..";
                    relativePath += separator;
                }
                else
                {
                    relativePath = fullPath;
                    break;
                }
            }
        }

        return relativePath;
    }

    /// <summary>
    /// Makes sure the string has a trailing backslash
    /// </summary>
    public static string EnsureTrailingBackslash(this string s)
    {
        return s.EnsureTrailingChar(Path.DirectorySeparatorChar);
    }

    /// <summary>
    /// Makes sure the string has the trailing character
    /// </summary>
    public static string EnsureTrailingChar(this string s, char ch)
    {
        return s.Length == 0 || s[s.Length - 1] != ch ? s + ch : s;
    }

    public static string WithOsPathSeparators(this string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        return text.Replace(IncorrectPathSeparator, Path.DirectorySeparatorChar);
    }

    private static readonly char IncorrectPathSeparator = Path.DirectorySeparatorChar == '\\' ? '/' : '\\';
}
