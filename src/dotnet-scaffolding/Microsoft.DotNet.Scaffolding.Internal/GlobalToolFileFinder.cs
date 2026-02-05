using System.Diagnostics;
using System.Reflection;
using Microsoft.DotNet.Scaffolding.Helpers;

namespace Microsoft.DotNet.Scaffolding.Internal;

internal static class GlobalToolFileFinder
{
    internal static string? FindCodeModificationConfigFile(string fileName, Assembly executingAssembly, string? projectPath = null)
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

        // Get target framework folder (net8.0, net9.0, net10.0, net11.0)
        string targetFrameworkFolder = "net11.0"; //TODO call GetTargetFrameworkFolder(projectPath); when other target frameworks are enabled
        var codeModificationConfigFolder = Path.Combine(toolsFolderPath, "Templates", targetFrameworkFolder, "CodeModificationConfigs");
        if (Directory.Exists(codeModificationConfigFolder))
        {
            var filePath = Path.Combine(codeModificationConfigFolder, fileName);
            if (File.Exists(filePath))
            {
                return filePath;
            }
        }

        return null;
    }

    private static string GetTargetFrameworkFolder(string? projectPath)
    {
        if (string.IsNullOrEmpty(projectPath))
        {
            return "net11.0";
        }

        TargetFramework? targetFramework = TargetFrameworkHelpers.GetTargetFrameworkForProject(projectPath);

        return targetFramework switch
        {
            TargetFramework.Net8 => "net8.0",
            TargetFramework.Net9 => "net9.0",
            TargetFramework.Net10 => "net10.0",
            _ => "net11.0"
        };
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
