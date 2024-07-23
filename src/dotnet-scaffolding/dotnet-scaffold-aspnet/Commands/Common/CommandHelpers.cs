// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Microsoft.DotNet.Scaffolding.Helpers.General;
using Microsoft.DotNet.Scaffolding.Helpers.Services;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Commands.Common;

internal static class CommandHelpers
{
    /// <summary>
    /// Given a class name (only meant for C# classes), get a file path at the base of the project (where the .csproj is on disk)
    /// </summary>
    /// <returns>string file path</returns>
    internal static string GetNewFilePath(IAppSettings? appSettings, string className)
    {
        var newFilePath = string.Empty;
        var fileName = StringUtil.EnsureCsExtension(className);
        var baseProjectPath = Path.GetDirectoryName(appSettings?.Workspace().InputPath);
        if (!string.IsNullOrEmpty(baseProjectPath))
        {
            newFilePath = Path.Combine(baseProjectPath, $"{fileName}");
            newFilePath = StringUtil.GetUniqueFilePath(newFilePath);
        }

        return newFilePath;
    }
}
