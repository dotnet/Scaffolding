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

        // Use provided target framework folder if present, otherwise prefer net11.0 as default
        // Instead of only checking a single tfm folder, search all available TFM subfolders under each CodeModificationConfigs root.

        // Search in Aspnet CodeModificationConfigs (all TFM subfolders)
        var aspnetConfigsRoot = Path.Combine(toolsFolderPath, "Aspnet", "CodeModificationConfigs");
        var result = SearchForConfigFileInRoot(aspnetConfigsRoot, fileName);
        if (result != null)
        {
            return result;
        }

        // Search in Aspire (TFM folder layout differs: Aspire/<tfm>/CodeModificationConfigs)
        var aspireRoot = Path.Combine(toolsFolderPath, "Aspire");
        result = SearchForConfigFileInAspireRoot(aspireRoot, fileName);
        if (result != null)
        {
            return result;
        }

        // Fallback: Search in old Templates folder for backward compatibility
        var templatesRoot = Path.Combine(toolsFolderPath, "Templates");
        return SearchForConfigFileInRoot(templatesRoot, fileName);
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

    // Search all TFM subfolders under a CodeModificationConfigs root (e.g. Aspnet/CodeModificationConfigs/<tfm>/...)
    private static string? SearchForConfigFileInRoot(string rootFolder, string fileName)
    {
        if (!Directory.Exists(rootFolder))
        {
            return null;
        }

        // First check for the file directly under the root (some older layouts may place it there)
        var direct = SearchForConfigFile(rootFolder, fileName);
        if (direct != null)
        {
            return direct;
        }

        // Enumerate subfolders (TFM folders) and search each one
        foreach (var subDir in Directory.EnumerateDirectories(rootFolder))
        {
            var candidate = SearchForConfigFile(subDir, fileName);
            if (candidate != null)
            {
                return candidate;
            }
        }

        return null;
    }

    // Aspire layout is Aspire/<tfm>/CodeModificationConfigs/<files...>
    private static string? SearchForConfigFileInAspireRoot(string aspireRoot, string fileName)
    {
        if (!Directory.Exists(aspireRoot))
        {
            return null;
        }

        foreach (var tfmDir in Directory.EnumerateDirectories(aspireRoot))
        {
            var configsFolder = Path.Combine(tfmDir, "CodeModificationConfigs");
            var candidate = SearchForConfigFile(configsFolder, fileName);
            if (candidate != null)
            {
                return candidate;
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
