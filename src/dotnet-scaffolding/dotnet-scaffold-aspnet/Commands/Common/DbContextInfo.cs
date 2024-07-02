// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.IO;
using Microsoft.DotNet.Scaffolding.Helpers.General;
using Microsoft.DotNet.Scaffolding.Helpers.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Commands.Common;
internal class DbContextInfo
{
    //DbContext info
    public bool CreateDbContext { get; set; } = false;
    public string? DbContextClassName { get; set; }
    public string? DbContextClassPath { get; set; }
    public string? DbContextNamespace { get; set; }
    public string? DatabaseProvider { get; set; }
    public bool EfScenario { get; set; } = false;
    public string? EntitySetVariableName { get; set; }
    public string? NewDbSetStatement { get; set; }

    internal void AddDbContext(ProjectInfo projectInfo, ILogger logger, IFileSystem fileSystem)
    {
        if (CreateDbContext && !string.IsNullOrEmpty(DatabaseProvider))
        {
            AspNetDbContextHelper.DatabaseTypeDefaults.TryGetValue(DatabaseProvider, out var dbContextProperties);
            if (dbContextProperties != null &&
                !string.IsNullOrEmpty(DbContextClassName) &&
                !string.IsNullOrEmpty(DbContextClassPath))
            {
                var projectBasePath = Path.GetDirectoryName(projectInfo.AppSettings?.Workspace()?.InputPath);
                dbContextProperties.DbContextName = DbContextClassName;
                dbContextProperties.DbSetStatement = NewDbSetStatement;
                logger.LogMessage($"Adding new DbContext '{dbContextProperties.DbContextName}'...");
                DbContextHelper.CreateDbContext(dbContextProperties, DbContextClassPath, projectBasePath, fileSystem);
            }
        }
    }
}
