// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.Scaffolding.CodeModification.Helpers;
using Microsoft.DotNet.Scaffolding.Roslyn.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Common;

internal static class ClassAnalyzers
{
    internal static DbContextInfo GetDbContextInfo(
        string projectPath,
        ISymbol? existingDbContextClass,
        string dbContextClassName,
        string dbProvider,
        string? modelName = null)
    {
        var dbContextInfo = new DbContextInfo();
        dbContextInfo.EfScenario = true;
        dbContextInfo.DatabaseProvider = dbProvider;
        if (existingDbContextClass is not null)
        {
            dbContextInfo.DbContextClassName = existingDbContextClass.Name;
            dbContextInfo.DbContextClassPath = existingDbContextClass.Locations.FirstOrDefault()?.SourceTree?.FilePath;
            dbContextInfo.DbContextNamespace = existingDbContextClass.ContainingNamespace.ToDisplayString();
            dbContextInfo.EntitySetVariableName = string.IsNullOrEmpty(modelName) ?
                string.Empty : EfDbContextHelpers.GetEntitySetVariableName(existingDbContextClass, modelName);
        }
        //properties for creating a new DbContext
        else
        {
            dbContextInfo.NewDbSetStatement = string.IsNullOrEmpty(modelName) ?
                string.Empty : $"public DbSet<{modelName}> {modelName} {{ get; set; }} = default!;";
            dbContextInfo.DbContextClassName = dbContextClassName;
            dbContextInfo.DbContextClassPath = CommandHelpers.GetNewFilePath(projectPath, dbContextClassName);
            dbContextInfo.DatabaseProvider = dbProvider;
            dbContextInfo.EntitySetVariableName = modelName;
        }

        if (!string.IsNullOrEmpty(dbContextInfo.DbContextNamespace) &&
            dbContextInfo.DbContextNamespace.Equals(Constants.GlobalNamespace, StringComparison.OrdinalIgnoreCase))
        {
            dbContextInfo.DbContextNamespace = string.Empty;
        }

        return dbContextInfo;
    }

    internal static DbContextInfo GetIdentityDbContextInfo(
        string projectPath,
        ISymbol? existingDbContextClass,
        string dbContextClassName,
        string dbProvider)
    {
        DbContextInfo dbContextInfo = new()
        {
            EfScenario = true,
            DatabaseProvider = dbProvider
        };

        if (existingDbContextClass is not null)
        {
            dbContextInfo.DbContextClassName = existingDbContextClass.Name;
            dbContextInfo.DbContextClassPath = existingDbContextClass.Locations.FirstOrDefault()?.SourceTree?.FilePath;
            dbContextInfo.DbContextNamespace = existingDbContextClass.ContainingNamespace.ToDisplayString();
            dbContextInfo.EntitySetVariableName = string.Empty;
        }
        //properties for creating a new DbContext
        else
        {
            dbContextInfo.NewDbSetStatement = string.Empty;
            dbContextInfo.DbContextClassName = dbContextClassName;
            dbContextInfo.DbContextClassPath = AspNetDbContextHelper.GetIdentityDataContextPath(projectPath, dbContextClassName);
            dbContextInfo.DbContextNamespace = $"{Path.GetFileNameWithoutExtension(projectPath)}.Data";
            dbContextInfo.DatabaseProvider = dbProvider;
            dbContextInfo.EntitySetVariableName = string.Empty;
        }

        //check for '<global namespace>' and remove it (for classes that don't define a namespace)
        if (!string.IsNullOrEmpty(dbContextInfo.DbContextNamespace) &&
            dbContextInfo.DbContextNamespace.Equals(Constants.GlobalNamespace, StringComparison.OrdinalIgnoreCase))
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
            modelInfo.ModelNamespace.Equals(Constants.GlobalNamespace, StringComparison.OrdinalIgnoreCase))
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

    /// <summary>
    /// Check if atleast one property and a primary key property were found for given --model class.
    /// </summary>
    /// <param name="modelInfo"></param>
    /// <param name="logger"></param>
    /// <returns></returns>
    internal static bool ValidateModelForCrudScaffolders(ModelInfo modelInfo, ILogger logger)
    {
        if (modelInfo is null || string.IsNullOrEmpty(modelInfo.ModelTypeName))
        {
            return false;
        }
        else if (modelInfo.ModelProperties is null || modelInfo.ModelProperties.Count == 0)
        {
            logger.LogError($"No properties found for --model '{modelInfo.ModelTypeName}'");
            return false;
        }
        else if (string.IsNullOrEmpty(modelInfo.PrimaryKeyName))
        {
            logger.LogError($"No primary key found for --model '{modelInfo.ModelTypeName}'");
            return false;
        }

        return true;
    }

    internal static ProjectInfo GetProjectInfo(string projectPath, ILogger logger)
    {
        var projectInfo = new ProjectInfo()
        {
            CodeService = new CodeService(logger, projectPath),
            ProjectPath = projectPath
        };

        return projectInfo;
    }
}
