// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Internal;
using System.Reflection;

namespace Microsoft.DotNet.Scaffolding.Core.Hosting;

/// <summary>
/// Helper class for finding Aspire code modification configuration files.
/// </summary>
internal static class AspireCodeModificationHelper
{
    private const string Net11FolderName = "net11.0";

    /// <summary>
    /// Finds a code modification configuration file in the net11.0 folder structure.
    /// </summary>
    /// <param name="folderName">The subfolder name (e.g., "Caching", "Database", "Storage").</param>
    /// <param name="fileName">The JSON configuration file name.</param>
    /// <param name="assembly">The executing assembly.</param>
    /// <returns>The full path to the configuration file, or null if not found.</returns>
    internal static string? FindNet11CodeModificationConfigFile(string folderName, string fileName, Assembly assembly)
    {
        var configPath = Path.Combine(Net11FolderName, folderName, fileName);
        return GlobalToolFileFinder.FindCodeModificationConfigFile(configPath, assembly);
    }
}
