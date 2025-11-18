// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.Scaffolding.CodeModification.Helpers;
using Microsoft.DotNet.Scaffolding.Core.Logging;
using Microsoft.DotNet.Scaffolding.Roslyn.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Common;

/// <summary>
/// Provides static methods for analyzing classes related to Entity Framework DbContext and model classes.
/// </summary>
internal static class ClassAnalyzers
{
    /// <summary>
    /// Gets information about a DbContext, either existing or to be created, including its name, path, namespace, and entity set variable.
    /// </summary>
    /// <param name="projectPath">The path to the project.</param>
    /// <param name="existingDbContextClass">The symbol representing an existing DbContext class, or null if creating a new one.</param>
    /// <param name="dbContextClassName">The name of the DbContext class to use or create.</param>
    /// <param name="dbProvider">The database provider (e.g., SQL Server, SQLite).</param>
    /// <param name="modelInfo">Optional model information for generating entity set variable or DbSet statement.</param>
    /// <returns>A <see cref="DbContextInfo"/> object with details about the DbContext.</returns>
    internal static DbContextInfo GetDbContextInfo(
        string projectPath,
        ISymbol? existingDbContextClass,
        string dbContextClassName,
        string dbProvider,
        ModelInfo? modelInfo = null)
    {
        var dbContextInfo = new DbContextInfo();
        dbContextInfo.EfScenario = true;
        dbContextInfo.DatabaseProvider = dbProvider;
        if (existingDbContextClass is not null)
        {
            dbContextInfo.DbContextClassName = existingDbContextClass.Name;
            dbContextInfo.DbContextClassPath = existingDbContextClass.Locations.FirstOrDefault()?.SourceTree?.FilePath;
            dbContextInfo.DbContextNamespace = existingDbContextClass.ContainingNamespace.ToDisplayString();
            dbContextInfo.EntitySetVariableName = modelInfo is null ?
                string.Empty : EfDbContextHelpers.GetEntitySetVariableName(existingDbContextClass, modelInfo.ModelTypeName, modelInfo.ModelFullName);
        }
        //properties for creating a new DbContext
        else
        {
            dbContextInfo.NewDbSetStatement = modelInfo is null ?
                string.Empty : $"public DbSet<{modelInfo.ModelFullName}> {modelInfo.ModelTypeName} {{ get; set; }} = default!;";
            dbContextInfo.DbContextClassName = dbContextClassName;
            dbContextInfo.DbContextClassPath = CommandHelpers.GetNewFilePath(projectPath, dbContextClassName);
            dbContextInfo.DatabaseProvider = dbProvider;
            dbContextInfo.EntitySetVariableName = modelInfo?.ModelTypeName;
        }

        if (!string.IsNullOrEmpty(dbContextInfo.DbContextNamespace) &&
            dbContextInfo.DbContextNamespace.Equals(Constants.GlobalNamespace, StringComparison.OrdinalIgnoreCase))
        {
            dbContextInfo.DbContextNamespace = string.Empty;
        }

        return dbContextInfo;
    }

    /// <summary>
    /// Gets information about an Identity DbContext, either existing or to be created, including its name, path, and namespace.
    /// </summary>
    /// <param name="projectPath">The path to the project.</param>
    /// <param name="existingDbContextClass">The symbol representing an existing Identity DbContext class, or null if creating a new one.</param>
    /// <param name="dbContextClassName">The name of the Identity DbContext class to use or create.</param>
    /// <param name="dbProvider">The database provider (e.g., SQL Server, SQLite).</param>
    /// <returns>A <see cref="DbContextInfo"/> object with details about the Identity DbContext.</returns>
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

    /// <summary>
    /// Gets information about a model class, including its name, namespace, full name, primary key, and properties.
    /// </summary>
    /// <param name="modelClassSymbol">The symbol representing the model class.</param>
    /// <returns>A <see cref="ModelInfo"/> object with details about the model class.</returns>
    internal static ModelInfo GetModelClassInfo(ISymbol modelClassSymbol)
    {
        var modelInfo = new ModelInfo();
        modelInfo.ModelTypeName = modelClassSymbol.Name;
        modelInfo.ModelNamespace = modelClassSymbol.ContainingNamespace.ToDisplayString();
        if (!string.IsNullOrEmpty(modelInfo.ModelNamespace))
        {
            if (modelInfo.ModelNamespace.Equals(Constants.GlobalNamespace, StringComparison.OrdinalIgnoreCase))
            {
                modelInfo.ModelNamespace = string.Empty;
                modelInfo.ModelFullName = modelInfo.ModelTypeName;
            }
            else
            {
                modelInfo.ModelFullName = $"{modelInfo.ModelNamespace}.{modelInfo.ModelTypeName}";
            }
        }
        else
        {
            modelInfo.ModelFullName = modelInfo.ModelTypeName;
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
    /// <param name="modelInfo">The model information to validate.</param>
    /// <param name="logger">The logger to use for error messages.</param>
    /// <returns>True if the model is valid for CRUD scaffolders; otherwise, false.</returns>
    internal static bool ValidateModelForCrudScaffolders(ModelInfo modelInfo, IScaffolderLogger logger)
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

    /// <summary>
    /// Gets information about a project, including its path, code service, lowest target framework, and capabilities.
    /// </summary>
    /// <param name="projectPath">The path to the project.</param>
    /// <param name="logger">The logger to use for initialization and diagnostics.</param>
    /// <returns>A <see cref="ProjectInfo"/> object with details about the project.</returns>
    internal static ProjectInfo GetProjectInfo(string projectPath, ILogger logger)
    {
        /* to use MSBuildProjectService, we need to initialize the MsBuildInitializer
         * that is because MSBuildLocator.Register fails if Microsoft.Build assemblies are already pulled in
         * using MSBuildProjectService does that unfortunately so this initialization cannot happen in that helper service
         * unlike ICodeService, also no chance for a wasted op because at this point in the scaffolder,
         * we will definitely to have MSBuild initialized.*/
        new MsBuildInitializer(logger).Initialize();
        var codeService = new CodeService(logger, projectPath);
        var msBuildProject = new MSBuildProjectService(projectPath);
        var lowestTFM = msBuildProject.GetLowestTargetFramework();
        var capabilities = msBuildProject.GetProjectCapabilities().ToList();
        var projectInfo = new ProjectInfo()
        {
            CodeService = codeService,
            ProjectPath = projectPath,
            LowestTargetFramework = lowestTFM,
            Capabilities = capabilities
        };

        return projectInfo;
    }
}
