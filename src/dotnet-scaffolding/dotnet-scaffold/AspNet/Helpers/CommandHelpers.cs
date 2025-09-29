// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Internal;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;

/// <summary>
/// Helper methods for command-related file and path operations.
/// </summary>
internal static class CommandHelpers
{
    /// <summary>
    /// Given a class name (only meant for C# classes), get a file path at the base of the project (where the .csproj is on disk)
    /// </summary>
    /// <param name="projectPath">The path to the project file.</param>
    /// <param name="className">The name of the class for which to generate the file path.</param>
    /// <returns>string file path</returns>
    internal static string GetNewFilePath(string projectPath, string className)
    {
        var newFilePath = string.Empty;
        var fileName = StringUtil.EnsureCsExtension(className);
        var baseProjectPath = Path.GetDirectoryName(projectPath);
        if (!string.IsNullOrEmpty(baseProjectPath))
        {
            newFilePath = Path.Combine(baseProjectPath, $"{fileName}");
            newFilePath = StringUtil.GetUniqueFilePath(newFilePath);
        }

        return newFilePath;
    }
}
