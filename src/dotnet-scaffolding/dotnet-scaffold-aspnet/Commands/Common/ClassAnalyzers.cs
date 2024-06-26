// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.Scaffolding.Helpers.Roslyn;
using Microsoft.DotNet.Scaffolding.Helpers.Services;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Commands.Common;

internal static class ClassAnalyzers
{
    internal static DbContextInfo GetDbContextInfo(
        ISymbol? existingDbContextClass,
        IAppSettings? appSettings,
        string dbContextClassName,
        string dbProvider,
        string modelName)
    {
        var dbContextInfo = new DbContextInfo();
        dbContextInfo.EfScenario = true;
        dbContextInfo.DatabaseProvider = dbProvider;
        if (existingDbContextClass is not null)
        {
            dbContextInfo.DbContextClassName = existingDbContextClass.Name;
            dbContextInfo.DbContextClassPath = existingDbContextClass.Locations.FirstOrDefault()?.SourceTree?.FilePath;
            dbContextInfo.DbContextNamespace = existingDbContextClass.ContainingNamespace.ToDisplayString();
            dbContextInfo.EntitySetVariableName = EfDbContextHelpers.GetEntitySetVariableName(existingDbContextClass, modelName);
        }
        //properties for creating a new DbContext
        else
        {
            dbContextInfo.CreateDbContext = true;
            dbContextInfo.NewDbSetStatement = $"public DbSet<{modelName}> {modelName} {{ get; set; }} = default!;";
            dbContextInfo.DbContextClassName = dbContextClassName;
            dbContextInfo.DbContextClassPath = CommandHelpers.GetNewFilePath(appSettings, dbContextClassName);
            dbContextInfo.DatabaseProvider = dbProvider;
            dbContextInfo.EntitySetVariableName = modelName;
        }

        if (!string.IsNullOrEmpty(dbContextInfo.DbContextNamespace) &&
            dbContextInfo.DbContextNamespace.Equals(Helpers.Constants.GlobalNamespace, StringComparison.OrdinalIgnoreCase))
        {
            dbContextInfo.DbContextNamespace = string.Empty;
        }

        return dbContextInfo;
    }

    internal static ModelInfo GetModelClassInfo(ISymbol modelClassSymbol)
    {
        var modelInfo = new ModelInfo();
        modelInfo.ModelTypeName = modelClassSymbol.Name;
        modelInfo.ModelNamespace = modelClassSymbol.ContainingNamespace.ToDisplayString();
        if (!string.IsNullOrEmpty(modelInfo.ModelNamespace) &&
            modelInfo.ModelNamespace.Equals(Helpers.Constants.GlobalNamespace, StringComparison.OrdinalIgnoreCase))
        {
            modelInfo.ModelNamespace = string.Empty;
        }

        var efModelProperties = EfDbContextHelpers.GetModelProperties(modelClassSymbol);
        if (efModelProperties != null)
        {
            modelInfo.PrimaryKeyName = efModelProperties.PrimaryKeyName;
            modelInfo.PrimaryKeyShortTypeName = efModelProperties.PrimaryKeyShortTypeName;
            modelInfo.PrimaryKeyTypeName = efModelProperties.PrimaryKeyTypeName;
            modelInfo.ModelProperties = efModelProperties.AllModelProperties;
        }

        return modelInfo;
    }

    internal static ProjectInfo GetProjectInfo(string projectPath, ILogger logger)
    {
        var projectInfo = new ProjectInfo();
        var workspaceSettings = new WorkspaceSettings
        {
            InputPath = projectPath
        };

        var projectAppSettings = new AppSettings();
        projectAppSettings.AddSettings("workspace", workspaceSettings);
        var codeService = new CodeService(projectAppSettings, logger);
        projectInfo.CodeService = codeService;
        projectInfo.AppSettings = projectAppSettings;
        return projectInfo;
    }
}
