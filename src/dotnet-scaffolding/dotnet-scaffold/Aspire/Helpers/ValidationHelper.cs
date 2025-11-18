// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.Logging;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Internal;
using Microsoft.DotNet.Scaffolding.TextTemplating.DbContext;
using Microsoft.DotNet.Tools.Scaffold.Aspire.Command;

namespace Microsoft.DotNet.Tools.Scaffold.Aspire.Helpers;

/// <summary>
/// ValidationHelper provides static methods for validating command options for caching, database, and storage scenarios in Aspire scaffolding.
/// It ensures required options are present and valid, and prepares context properties for further processing.
/// </summary>
internal static class ValidationHelper
{
    internal static bool ValidateCachingSettings(ScaffolderContext context, IScaffolderLogger logger)
    {
        string? typeValue = context.GetOptionResult<string>(AspireCliStrings.TypeCliOption);
        string? appHostProjectValue = context.GetOptionResult<string>(AspireCliStrings.AppHostCliOption);
        string? workerProjectValue = context.GetOptionResult<string>(AspireCliStrings.WorkerProjectCliOption);
        bool prereleaseValue = context.GetOptionResult<bool>(AspireCliStrings.PrereleaseCliOption);
        if (string.IsNullOrEmpty(typeValue) || !AspireCliStrings.CachingTypeCustomValues.Contains(typeValue, StringComparer.OrdinalIgnoreCase))
        {
            string cachingTypeDisplayList = string.Join(", ", AspireCliStrings.CachingTypeCustomValues.GetRange(0, AspireCliStrings.CachingTypeCustomValues.Count - 1)) +
                (AspireCliStrings.CachingTypeCustomValues.Count > 1 ? " and " : "") + AspireCliStrings.CachingTypeCustomValues[AspireCliStrings.CachingTypeCustomValues.Count - 1];
            logger.LogError("Missing/Invalid --type option.");
            logger.LogError($"Valid options : {cachingTypeDisplayList}");
            return false;
        }

        if (string.IsNullOrEmpty(appHostProjectValue))
        {
            logger.LogError("Missing/Invalid --apphost-project option.");
            return false;
        }

        if (string.IsNullOrEmpty(workerProjectValue))
        {
            logger.LogError("Missing/Invalid --project option.");
            return false;
        }

        var commandSettings = new CommandSettings
        {
            AppHostProject = appHostProjectValue,
            Project = workerProjectValue,
            Type = typeValue,
            Prerelease = prereleaseValue
        };

        context.Properties.Add(nameof(CommandSettings), commandSettings);
        return true;
    }

    internal static bool ValidateDatabaseSettings(ScaffolderContext context, IScaffolderLogger logger)
    {
        string? typeValue = context.GetOptionResult<string>(AspireCliStrings.TypeCliOption);
        string? appHostProjectValue = context.GetOptionResult<string>(AspireCliStrings.AppHostCliOption);
        string? workerProjectValue = context.GetOptionResult<string>(AspireCliStrings.WorkerProjectCliOption);
        bool prereleaseValue = context.GetOptionResult<bool>(AspireCliStrings.PrereleaseCliOption);
        if (string.IsNullOrEmpty(typeValue) || !AspireCliStrings.Database.DatabaseTypeCustomValues.Contains(typeValue, StringComparer.OrdinalIgnoreCase))
        {
            string dbTypeDisplayList = string.Join(", ", AspireCliStrings.Database.DatabaseTypeCustomValues.GetRange(0, AspireCliStrings.Database.DatabaseTypeCustomValues.Count - 1)) +
                (AspireCliStrings.Database.DatabaseTypeCustomValues.Count > 1 ? " and " : "") + AspireCliStrings.Database.DatabaseTypeCustomValues[AspireCliStrings.Database.DatabaseTypeCustomValues.Count - 1];
            logger.LogError("Missing/Invalid --type option.");
            logger.LogError($"Valid options : {dbTypeDisplayList}.");
            return false;
        }

        if (string.IsNullOrEmpty(appHostProjectValue))
        {
            logger.LogError("Missing/Invalid --apphost-project option.");
            return false;
        }

        if (string.IsNullOrEmpty(workerProjectValue))
        {
            logger.LogError("Missing/Invalid --project option.");
            return false;
        }

        var commandSettings = new CommandSettings
        {
            AppHostProject = appHostProjectValue,
            Project = workerProjectValue,
            Type = typeValue,
            Prerelease = prereleaseValue
        };

        context.Properties.Add(nameof(CommandSettings), commandSettings);
        var dbContextProperties = GetDbContextProperties(commandSettings);
        if (dbContextProperties is not null)
        {
            context.Properties.Add(nameof(DbContextProperties), dbContextProperties);
        }

        var projectBasePath = Path.GetDirectoryName(commandSettings.Project);
        if (!string.IsNullOrEmpty(projectBasePath))
        {
            context.Properties.Add(Constants.StepConstants.BaseProjectPath, projectBasePath);
        }
        return true;
    }

    internal static bool ValidateStorageSettings(ScaffolderContext context, IScaffolderLogger logger)
    {
        string? typeValue = context.GetOptionResult<string>(AspireCliStrings.TypeCliOption);
        string? appHostProjectValue = context.GetOptionResult<string>(AspireCliStrings.AppHostCliOption);
        string? workerProjectValue = context.GetOptionResult<string>(AspireCliStrings.WorkerProjectCliOption);
        bool prereleaseValue = context.GetOptionResult<bool>(AspireCliStrings.PrereleaseCliOption);
        if (string.IsNullOrEmpty(typeValue) || !AspireCliStrings.StorageTypeCustomValues.Contains(typeValue, StringComparer.OrdinalIgnoreCase))
        {
            string storageTypeDisplayList = string.Join(", ", AspireCliStrings.StorageTypeCustomValues.GetRange(0, AspireCliStrings.StorageTypeCustomValues.Count - 1)) +
                (AspireCliStrings.StorageTypeCustomValues.Count > 1 ? " and " : "") + AspireCliStrings.StorageTypeCustomValues[AspireCliStrings.StorageTypeCustomValues.Count - 1];
            logger.LogError("Missing/Invalid --type option.");
            logger.LogError($"Valid options : {storageTypeDisplayList}");
            return false;
        }

        if (string.IsNullOrEmpty(appHostProjectValue))
        {
            logger.LogError("Missing/Invalid --apphost-project option.");
            return false;
        }

        if (string.IsNullOrEmpty(workerProjectValue))
        {
            logger.LogError("Missing/Invalid --project option.");
            return false;
        }

        var commandSettings = new CommandSettings
        {
            AppHostProject = appHostProjectValue,
            Project = workerProjectValue,
            Type = typeValue,
            Prerelease = prereleaseValue
        };

        context.Properties.Add(nameof(CommandSettings), commandSettings);
        return true;
    }

    /// <summary>
    /// generate a path for DbContext, then use DbContextHelper.CreateDbContext to invoke 'NewDbContext.tt'
    /// DbContextHelper.CreateDbContext will also write the resulting templated string (class text) to disk
    /// </summary>
    private static DbContextProperties? GetDbContextProperties(CommandSettings settings)
    {
        var newDbContextPath = CreateNewDbContextPath(settings);
        if (AspireCliStrings.Database.DbContextTypeDefaults.TryGetValue(settings.Type, out var dbContextProperties) &&
            dbContextProperties is not null)
        {
            dbContextProperties.DbContextPath = newDbContextPath;
            return dbContextProperties;
        }

        return null;
    }

    private static string CreateNewDbContextPath(CommandSettings commandSettings)
    {
        if (!AspireCliStrings.Database.DbContextTypeDefaults.TryGetValue(commandSettings.Type, out var dbContextProperties) || dbContextProperties is null)
        {
            return string.Empty;
        }

        var dbContextPath = string.Empty;
        var dbContextFileName = $"{dbContextProperties.DbContextName}.cs";
        var baseProjectPath = Path.GetDirectoryName(commandSettings.Project);
        if (!string.IsNullOrEmpty(baseProjectPath))
        {
            dbContextPath = Path.Combine(baseProjectPath, dbContextFileName);
            dbContextPath = StringUtil.GetUniqueFilePath(dbContextPath);
        }

        return dbContextPath;
    }
}
