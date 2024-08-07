using System.Diagnostics;
using System.Reflection;

namespace Microsoft.DotNet.Scaffolding.Internal;

internal static class GlobalToolFileFinder
{
    internal static string? FindCodeModificationConfigFile(string fileName, Assembly executingAssembly)
    {
        var assemblyDirectory = Path.GetDirectoryName(executingAssembly?.Location);
        if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(assemblyDirectory))
        {
            return null;
        }

        var toolsFolderPath = FindFolderWithToolsFolder(assemblyDirectory);
        if (string.IsNullOrEmpty(toolsFolderPath))
        {
            return null;
        }

        var codeModificationConfigFolder = Path.Combine(toolsFolderPath, "CodeModificationConfigs");
        if (Directory.Exists(codeModificationConfigFolder))
        {
            var files = Directory.EnumerateFiles(codeModificationConfigFolder, "*.json", SearchOption.AllDirectories);
            return files.FirstOrDefault(x => Path.GetFileName(x).Equals(fileName, StringComparison.OrdinalIgnoreCase));
        }

        return null;
    }

    private static string? FindFolderWithToolsFolder(string startPath)
    {
        DirectoryInfo? directory = new(startPath);
        while (directory != null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "tools")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }
}
