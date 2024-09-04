// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Microsoft.DotNet.Scaffolding.Internal;

internal static class StringUtil
{
    //converts Project.Namespace.SubNamespace to Project//Namespace//SubNamespace or Project\\Namespace\\SubNamespace (based on OS)
    public static string ToPath(string namespaceName, string basePath, string projectRootNamespace)
    {
        string path = string.Empty;
        if (string.IsNullOrEmpty(namespaceName) || string.IsNullOrEmpty(basePath))
        {
            return string.Empty;
        }

        namespaceName = RemovePrefix(namespaceName, basePath, projectRootNamespace);
        namespaceName = namespaceName.Replace(".", Path.DirectorySeparatorChar.ToString());
        try
        {
            basePath = Path.HasExtension(basePath) ? Path.GetDirectoryName(basePath) ?? basePath : basePath;
            var combinedPath = Path.Combine(basePath, namespaceName);
            path = Path.GetFullPath(combinedPath);
        }
        //invalid path
        catch (Exception ex) when (ex is ArgumentException || ex is PathTooLongException || ex is NotSupportedException)
        { }

        return path;
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

    //remove prefix from namespace, used to remove the project name from namespace when creating the path
    public static string RemovePrefix(string projectNamespace, string basePath, string prefix)
    {
        string[] namespaceParts = projectNamespace.Split('.');
        string[] basePathParts = basePath.Split(new char[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
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
            ns = Path.HasExtension(ns) ? Path.GetDirectoryName(ns) ?? ns : ns;
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

    public static string GetTypeNameFromNamespace(string templateName)
    {
        string[] parts = templateName.Split('.');
        return parts[parts.Length - 1];
    }

    public static string NormalizeLineEndings(string text)
    {
        //change all line endings to "\n" and then replace them with the appropriate ending
        return text.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", System.Environment.NewLine);
    }

    public static List<string> ToStringList(this List<string?> listWithNullableStrings)
    {
        return listWithNullableStrings.ConvertAll(s => s ?? string.Empty).Where(s => s.Equals(string.Empty)).ToList();
    }

    public static Dictionary<string, string> ParseArguments(List<string> args)
    {
        var arguments = new Dictionary<string, string>();
        if (args != null && args.Count > 0)
        {
            string currentKey = string.Empty;
            foreach (var arg in args)
            {
                if (arg.StartsWith("--") || arg.StartsWith("-"))
                {
                    currentKey = arg;
                    arguments[currentKey] = string.Empty;
                }
                else if (!string.IsNullOrEmpty(currentKey))
                {
                    arguments[currentKey] = arg;
                    currentKey = string.Empty;
                }
            }
        }

        return arguments;
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
}
