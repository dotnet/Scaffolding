using System.Diagnostics;
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

        // Use provided target framework folder or default to net11.0
        var tfmFolder = targetFrameworkFolder ?? "net11.0";
        
        // Search in Aspnet folder first
        var aspnetConfigFolder = Path.Combine(toolsFolderPath, "Aspnet", "CodeModificationConfigs", tfmFolder);
        var result = SearchForConfigFile(aspnetConfigFolder, fileName);
        if (result != null)
        {
            return result;
        }

        // Search in Aspire folder
        var aspireConfigFolder = Path.Combine(toolsFolderPath, "Aspire", tfmFolder, "CodeModificationConfigs");
        result = SearchForConfigFile(aspireConfigFolder, fileName);
        if (result != null)
        {
            return result;
        }

        // Fallback: Search in old Templates folder for backward compatibility
        var templatesConfigFolder = Path.Combine(toolsFolderPath, "Templates", tfmFolder, "CodeModificationConfigs");
        return SearchForConfigFile(templatesConfigFolder, fileName);
    }

    private static string? SearchForConfigFile(string configFolder, string fileName)
    {
        if (!Directory.Exists(configFolder))
        {
            return null;
        }

        // Search for the file by name (case-insensitive)
        var files = Directory.EnumerateFiles(configFolder, "*.json", SearchOption.AllDirectories);
        var matchedFile = files.FirstOrDefault(x => Path.GetFileName(x).Equals(fileName, StringComparison.OrdinalIgnoreCase));
        
        if (matchedFile != null)
        {
            return matchedFile;
        }
        
        // Also check for the file as a relative path (e.g., "subfolder/config.json")
        if (fileName.Contains(Path.DirectorySeparatorChar) || fileName.Contains(Path.AltDirectorySeparatorChar))
        {
            var fullPath = Path.Combine(configFolder, fileName);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }
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
