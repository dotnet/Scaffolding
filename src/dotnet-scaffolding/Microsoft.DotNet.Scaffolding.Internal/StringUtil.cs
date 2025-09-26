// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Microsoft.DotNet.Scaffolding.Internal;

internal static class StringUtil
{
    //converts Project.Namespace.SubNamespace to Project//Namespace//SubNamespace or Project\\Namespace\\SubNamespace (based on OS)
    //TODO : add string helpers for all the different little checks below.
    public static string? ToPath(string namespaceName, string? basePath, string projectRootNamespace)
    {
        if (string.IsNullOrEmpty(namespaceName) || string.IsNullOrEmpty(basePath) || string.IsNullOrEmpty(projectRootNamespace))
        {
            return string.Empty;
        }

        try
        {
            // Normalize the base path if it is a file
            if (Path.HasExtension(basePath))
            {
                basePath = Path.GetDirectoryName(basePath);
            }

            // Ensure basePath ends with a directory separator
            if (!string.IsNullOrEmpty(basePath) && !basePath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            {
                basePath += Path.DirectorySeparatorChar;
            }

            // Remove redundant project root namespace folder from basePath if present
            if (!string.IsNullOrEmpty(basePath) && basePath.EndsWith(Path.Combine(projectRootNamespace, string.Empty) + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            {
                basePath = basePath.Substring(0, basePath.Length - projectRootNamespace.Length - 1);
            }

            // Remove the project root namespace prefix from the namespaceName
            if (namespaceName.StartsWith(projectRootNamespace + ".", StringComparison.Ordinal))
            {
                namespaceName = namespaceName.Substring(projectRootNamespace.Length + 1);
            }

            // Convert namespaceName into a directory path
            string relativePath = namespaceName.Replace('.', Path.DirectorySeparatorChar);
            // Combine the base path with the relative path
            if (string.IsNullOrEmpty(basePath))
            {
                return string.Empty;
            }

            string combinedPath = Path.Combine(basePath, projectRootNamespace, relativePath);
            // Get the full path
            return Path.GetFullPath(combinedPath);
        }
        catch (Exception ex) when (ex is ArgumentException || ex is PathTooLongException || ex is NotSupportedException)
        {
            // Handle invalid path scenarios
            return string.Empty;
        }
    }

    //normalize the path separators between mac/linux and windows. 
    //replacing the other platform's path separator with the one we're on.
    public static string NormalizePathSeparators(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return path;
        }
        
        // get current platform's path separator
        string currentSeparator = Path.DirectorySeparatorChar.ToString();
        string otherSeparator = Path.DirectorySeparatorChar == '/' ? "\\" : "/";
        //replace the other platform's separator with the current one.
        return path.Replace(otherSeparator, currentSeparator);
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

    public static string GetTypeNameFromNamespace(string templateName)
    {
        string[] parts = templateName.Split('.');
        return parts[parts.Length - 1];
    }

    /// <summary>
    /// Use enumeration to get file name if they already exist on disk.
    /// For example, if File.cs exists, try File1.cs and so forth.
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    public static string GetUniqueFilePath(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return filePath;
        }

        var directory = Path.GetDirectoryName(filePath);
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
        var fileExtension = Path.GetExtension(filePath);

        if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(fileNameWithoutExtension))
        {
            return filePath;
        }

        int count = 1;
        string newFilePath;
        do
        {
            newFilePath = Path.Combine(directory, $"{fileNameWithoutExtension}{count}{fileExtension}");
            count++;
        } while (File.Exists(newFilePath));

        return newFilePath;
    }

    public static string EnsureCsExtension(string filePath)
    {
        if (!filePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
        {
            filePath += ".cs";
        }
        return filePath;
    }

    /// <summary>
    /// Converts a string representation of an array "[item1,item2,item3]" to a string array.
    /// </summary>
    /// <param name="arrayString">String representation of an array, e.g. "[item1,item2,item3]"</param>
    /// <returns>Array of strings, or empty array if input is invalid</returns>
    internal static string[] ConvertStringToArray(string arrayString)
    {
        if (string.IsNullOrEmpty(arrayString))
        {
            return Array.Empty<string>();
        }

        // Remove the brackets and any whitespace
        string trimmed = arrayString.Trim();
        if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
        {
            trimmed = trimmed.Substring(1, trimmed.Length - 2);
        }

        // Handle empty array case
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return Array.Empty<string>();
        }

        // Split by comma and trim each item
        return trimmed.Split(',')
                     .Select(item => item.Trim())
                     .ToArray();
    }
}
