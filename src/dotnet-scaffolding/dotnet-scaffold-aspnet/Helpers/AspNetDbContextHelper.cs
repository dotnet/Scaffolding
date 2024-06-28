// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.Scaffolding.Helpers.General;
using Microsoft.DotNet.Scaffolding.Helpers.Roslyn;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Commands.Common;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;

internal class AspNetDbContextHelper
{
    internal static DbContextProperties SqlServerDefaults = new()
    {
        AddDbMethod = "AddSqlServer",
    };

    internal static DbContextProperties SqliteDefaults = new()
    {
        AddDbMethod = "AddSqlite",
    };

    internal static DbContextProperties CosmosDefaults = new()
    {
        AddDbMethod = "AddCosmos",
    };

    internal static DbContextProperties NpgsqlDefaults = new()
    {
        AddDbMethod = "AddPostgres",
    };

    internal static Dictionary<string, DbContextProperties?> DatabaseTypeDefaults = new()
    {
        { PackageConstants.EfConstants.Postgres, NpgsqlDefaults },
        { PackageConstants.EfConstants.SqlServer, SqlServerDefaults },
        { PackageConstants.EfConstants.SQLite, SqliteDefaults },
        { PackageConstants.EfConstants.CosmosDb, CosmosDefaults }
    };

    internal static CodeModifierConfig AddDbContextChanges(DbContextInfo dbContextInfo, CodeModifierConfig configToEdit)
    {
        if (dbContextInfo.EfScenario)
        {
            var programCsFile = configToEdit.Files?.FirstOrDefault(x =>
                !string.IsNullOrEmpty(x.FileName) &&
                x.FileName.Equals("Program.cs", StringComparison.OrdinalIgnoreCase) &&
                x.Options is not null &&
                x.Options.Contains(CodeChangeOptionStrings.EfScenario));

            var globalMethod = programCsFile?.Methods?["Global"];
            var addDbContextChange = globalMethod?.CodeChanges?.FirstOrDefault(x => x.Block.Contains("builder.Services.AddDbContext", StringComparison.OrdinalIgnoreCase));
            if (dbContextInfo.CreateDbContext &&
                addDbContextChange is not null &&
                !string.IsNullOrEmpty(dbContextInfo.DatabaseProvider) &&
                PackageConstants.EfConstants.UseDatabaseMethods.TryGetValue(dbContextInfo.DatabaseProvider, out var useDbMethod))
            {
                addDbContextChange.Block = string.Format(addDbContextChange.Block, dbContextInfo.DbContextClassName, useDbMethod);
            }

            if (string.IsNullOrEmpty(dbContextInfo.EntitySetVariableName) &&
                !dbContextInfo.CreateDbContext &&
                !string.IsNullOrEmpty(dbContextInfo.DbContextClassName) &&
                globalMethod != null)
            {
                var addDbStatementCodeChange = new CodeFile()
                {
                    FileName = StringUtil.EnsureCsExtension(dbContextInfo.DbContextClassName),
                    Options = [CodeChangeOptionStrings.EfScenario],
                    ClassProperties = [new CodeBlock
                    {
                        Block = dbContextInfo.NewDbSetStatement
                    }]
                };

                configToEdit.Files = configToEdit.Files?.Append(addDbStatementCodeChange).ToArray();
            }
        }

        return configToEdit;
    }

}
