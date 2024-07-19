// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Collections.Generic;
using Microsoft.DotNet.Scaffolding.Helpers.General;
using Microsoft.DotNet.Tools.Scaffold.Aspire.Helpers;

namespace Microsoft.DotNet.Tools.Scaffold.Aspire.Commands;

internal static class GetCmdsHelper
{
    internal static DatabaseProperties SqlServerDefaults = new()
    {
        AspireDbType = "sqlserver",
        AspireDbName = "sqldb",
        AspireAddDbMethod = "AddSqlServer",
        AspireAddDbContextMethod = "AddSqlServerDbContext",
    };

    internal static DatabaseProperties NpgsqlDefaults = new()
    {
        AspireDbType = "postgresql",
        AspireDbName = "postgresqldb",
        AspireAddDbMethod = "AddPostgres",
        AspireAddDbContextMethod = "AddNpgsqlDbContext",
    };

    internal static Dictionary<string, DatabaseProperties?> DatabaseTypeDefaults = new()
    {
        { "npgsql-efcore", NpgsqlDefaults },
        { "sqlserver-efcore", SqlServerDefaults }
    };

    internal static Dictionary<string, DbContextProperties?> DbContextTypeDefaults = new()
    {
        { "npgsql-efcore", DbContextHelper.NpgsqlDefaults },
        { "sqlserver-efcore", DbContextHelper.SqlServerDefaults }
    };

    internal static List<string> CachingTypeCustomValues = ["redis", "redis-with-output-caching"];
    internal static List<string> DatabaseTypeCustomValues = [.. DatabaseTypeDefaults.Keys];
    internal static List<string> StorageTypeCustomValues = ["azure-storage-queues", "azure-storage-blobs", "azure-data-tables"];
}
