using System.Reflection;

namespace Microsoft.DotNet.Scaffolding.Internal;

internal static class GlobalToolFileFinder
{
    internal static string? FindCodeModificationConfigFile(string fileName, Assembly executingAssembly, string? targetFrameworkFolder = null)
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

        var frameworkFolder = targetFrameworkFolder ?? "net11.0";
        var aspnetConfigsFolder = Path.Combine(toolsFolderPath, "Aspnet", "CodeModificationConfigs", frameworkFolder);
        var result = SearchForConfigFile(aspnetConfigsFolder, fileName);
        if (result != null)
        {
            return result;
        }

        var aspireConfigsFolder = Path.Combine(toolsFolderPath, "Aspire", frameworkFolder, "CodeModificationConfigs");
        result = SearchForConfigFile(aspireConfigsFolder, fileName);
        if (result != null)
        {
            return result;
        }

        var templatesConfigsFolder = Path.Combine(toolsFolderPath, "Templates", frameworkFolder, "CodeModificationConfigs");
        return SearchForConfigFile(templatesConfigsFolder, fileName);
    }

    private static string? SearchForConfigFile(string configFolder, string fileName)
    {
        if (!Directory.Exists(configFolder))
        {
            return null;
        }

        // check for the file as a relative path (e.g., "subfolder/config.json")
        if (fileName.Contains(Path.DirectorySeparatorChar) || fileName.Contains(Path.AltDirectorySeparatorChar))
        {
            var fullPath = Path.Combine(configFolder, fileName);
            return File.Exists(fullPath) ? fullPath : null;
        }

        // Search for the file by name case-insensitively (Linux file systems are case-sensitive)
        return Directory.EnumerateFiles(configFolder, "*", SearchOption.AllDirectories)
            .FirstOrDefault(f => Path.GetFileName(f).Equals(fileName, StringComparison.OrdinalIgnoreCase));
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
