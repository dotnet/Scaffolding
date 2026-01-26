// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.TextTemplating.DbContext;
using Microsoft.DotNet.Tools.Scaffold.Aspire.Helpers;

namespace Microsoft.DotNet.Tools.Scaffold.Aspire.Command;

internal class AspireCliStrings
{
    // dotnet scaffold categories
    internal const string AspireCategory = "Aspire";

    // aspire caching
    internal const string CachingTitle = "caching";
    internal const string CachingDescription = "Modify Aspire project to make it caching ready.";
    internal const string CachingTypeOption = "Caching type";
    internal const string CachingTypeDescription = "Types of caching";

    internal static List<string> CachingTypeCustomValues = ["redis", "redis-with-output-caching"];

    // aspire database
    internal class Database
    {

        internal const string DatabaseTitle = "database";
        internal const string DatabaseDescription = "Modify Aspire project to make it database ready.";
        internal const string DatabaseTypeOption = "Database type";
        internal const string DatabaseTypeDescription = "Types of database";

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

        internal static List<string> DatabaseTypeCustomValues = DatabaseTypeDefaults.Keys.ToList();
    }

    // aspire storage
    internal const string StorageTitle = "storage";
    internal const string StorageDescription = "Modify Aspire project to make it storage ready.";
    internal const string StorageTypeOption = "Storage type";
    internal const string StorageTypeDescription = "Types of storage";
    internal const string TypeCliOption = "--type";

    internal static List<string> StorageTypeCustomValues = ["azure-storage-queues", "azure-storage-blobs", "azure-data-tables"];

    // options
    internal const string AppHostProjectOption = "Aspire App host project file";
    internal const string AppHostProjectDescription = "Aspire App host project for the scaffolding";
    internal const string AppHostCliOption = "--apphost-project";

    internal const string ProjectOption = "Web or worker project file";
    internal const string ProjectOptionDescription = "Web or worker project associated with the Aspire App host";
    internal static string WorkerProjectCliOption = "--project";

    internal const string PrereleaseOption = "Include Prerelease packages?";
    internal const string PrereleaseDescription = "Include prerelease package versions when installing latest Aspire components";
    internal const string PrereleaseCliOption = "--prerelease";
}
