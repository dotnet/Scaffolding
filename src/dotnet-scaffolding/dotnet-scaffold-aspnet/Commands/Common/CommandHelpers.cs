// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Collections.Generic;
using System.IO;
using Microsoft.DotNet.Scaffolding.Helpers.General;
using Microsoft.DotNet.Scaffolding.Helpers.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Commands.Common;

internal static class CommandHelpers
{
    internal static void AddDbContext(DbContextInfo dbContextInfo, ILogger logger, IFileSystem fileSystem)
    {
        //need to create a DbContext
        if (dbContextInfo.CreateDbContext && !string.IsNullOrEmpty(dbContextInfo.DatabaseProvider))
        {
            AspNetDbContextHelper.DatabaseTypeDefaults.TryGetValue(dbContextInfo.DatabaseProvider, out var dbContextProperties);
            if (dbContextProperties != null &&
                !string.IsNullOrEmpty(dbContextInfo.DbContextClassName) &&
                !string.IsNullOrEmpty(dbContextInfo.DbContextClassPath))
            {
                dbContextProperties.DbContextName = dbContextInfo.DbContextClassName;
                dbContextProperties.DbSetStatement = dbContextInfo.NewDbSetStatement;
                logger.LogMessage($"Adding new DbContext '{dbContextProperties.DbContextName}'...");
                DbContextHelper.CreateDbContext(dbContextProperties, dbContextInfo.DbContextClassPath, fileSystem);
            }
        }
    }

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

    internal static void InstallPackages(ILogger logger, string projectPath, bool prerelease, List<string> packages)
    {
        foreach (var package in packages)
        {
            DotnetCommands.AddPackage(
            packageName: package,
            logger: logger,
            projectFile: projectPath,
            includePrerelease: prerelease);
        }
    }
}
