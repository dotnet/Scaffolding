// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Internal;
using Microsoft.DotNet.Scaffolding.TextTemplating.DbContext;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using Constants = Microsoft.DotNet.Scaffolding.Internal.Constants;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;

/// <summary>
/// Helper methods for working with ASP.NET DbContext and related code generation.
/// </summary>
internal class AspNetDbContextHelper
{
    /// <summary>
    /// Default DbContext properties for each supported database provider.
    /// </summary>
    internal static Dictionary<string, DbContextProperties?> DbContextTypeDefaults = new()
    {
        { PackageConstants.EfConstants.Postgres, DbContextHelper.NpgsqlDefaults },
        { PackageConstants.EfConstants.SqlServer, DbContextHelper.SqlServerDefaults },
        { PackageConstants.EfConstants.SQLite, DbContextHelper.SqliteDefaults },
        { PackageConstants.EfConstants.CosmosDb, DbContextHelper.CosmosDefaults }
    };

    /// <summary>
    /// Default IdentityDbContext properties for supported providers.
    /// </summary>
    internal static Dictionary<string, DbContextProperties?> IdentityDbContextTypeDefaults = new()
    {
        { PackageConstants.EfConstants.SqlServer, DbContextHelper.SqlServerDefaults },
        { PackageConstants.EfConstants.SQLite, DbContextHelper.SqliteDefaults }
    };

    /// <summary>
    /// Gets code modifier properties for a given DbContextInfo.
    /// </summary>
    internal static Dictionary<string, string> GetDbContextCodeModifierProperties(DbContextInfo dbContextInfo)
    {
        var dbContextProperties = new Dictionary<string, string>();
        if (dbContextInfo.EfScenario)
        {
            if (!string.IsNullOrEmpty(dbContextInfo.DatabaseProvider) &&
                PackageConstants.EfConstants.UseDatabaseMethods.TryGetValue(dbContextInfo.DatabaseProvider, out var useDbMethod))
            {
                string useDbMethodFull = dbContextInfo.DatabaseProvider.Equals(PackageConstants.EfConstants.CosmosDb, StringComparison.OrdinalIgnoreCase) ?
                    $"{useDbMethod}(connectionString, \"{dbContextInfo.DbContextClassName}\")" :
                    $"{useDbMethod}(connectionString)"; 
                dbContextProperties.Add(Constants.CodeModifierPropertyConstants.UseDbMethod, useDbMethodFull);
            }

            if (!string.IsNullOrEmpty(dbContextInfo.DbContextClassName))
            {
                dbContextProperties.Add(Constants.CodeModifierPropertyConstants.DbContextName, dbContextInfo.DbContextClassName);
                dbContextProperties.Add(Constants.CodeModifierPropertyConstants.ConnectionStringName, dbContextInfo.DbContextClassName);
            }

            if (!string.IsNullOrEmpty(dbContextInfo.DbContextNamespace))
            {
                dbContextProperties.Add(Constants.CodeModifierPropertyConstants.DbContextNamespace, dbContextInfo.DbContextNamespace);
            }
        }

        return dbContextProperties;
    }

    /// <summary>
    /// Gets DbContextProperties for a given project and DbContextInfo.
    /// </summary>
    internal static DbContextProperties? GetDbContextProperties(string projectPath, DbContextInfo dbContextInfo)
    {
        var projectBasePath = Path.GetDirectoryName(projectPath);
        if (!string.IsNullOrEmpty(dbContextInfo.DatabaseProvider) &&
            DbContextTypeDefaults.TryGetValue(dbContextInfo.DatabaseProvider, out var dbContextProperties) &&
            dbContextProperties is not null &&
            !string.IsNullOrEmpty(dbContextInfo.DbContextClassName) &&
            !string.IsNullOrEmpty(dbContextInfo.DbContextClassPath) &&
            !string.IsNullOrEmpty(projectBasePath))
        {
            dbContextProperties.DbContextName = dbContextInfo.DbContextClassName;
            dbContextProperties.DbSetStatement = dbContextInfo.NewDbSetStatement;
            dbContextProperties.DbContextPath = dbContextInfo.DbContextClassPath;
            return dbContextProperties;
        }

        return null;
    }

    /// <summary>
    /// Gets the file path for a new Identity data context class.
    /// </summary>
    internal static string GetIdentityDataContextPath(string projectPath, string className)
    {
        var newFilePath = string.Empty;
        var fileName = StringUtil.EnsureCsExtension(className);
        var baseProjectPath = Path.GetDirectoryName(projectPath);
        if (!string.IsNullOrEmpty(baseProjectPath))
        {
            newFilePath = Path.Combine(baseProjectPath, "Data", fileName);
            newFilePath = StringUtil.GetUniqueFilePath(newFilePath);
        }

        return newFilePath;
    }
}
