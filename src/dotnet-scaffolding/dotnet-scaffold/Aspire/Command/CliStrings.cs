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
    internal const string CachingTypeDescription = "The type of caching to add. Only 'redis' (for distributed caching) and 'redis-with-output-caching' (for response caching) are available.";

    internal static List<string> CachingTypeCustomValues = ["redis", "redis-with-output-caching"];

    // aspire caching examples
    internal const string CachingExample1 = "dotnet scaffold aspire caching --type redis --apphost-project C:/MyApp/MyApp.AppHost/MyApp.AppHost.csproj --project C:/MyApp/MyApp.Web/MyApp.Web.csproj";
    internal const string CachingExample1Description = "Add Redis distributed caching to an Aspire web project:";
    internal const string CachingExample2 = "dotnet scaffold aspire caching --type redis-with-output-caching --apphost-project C:/MyApp/AppHost.csproj --project C:/MyApp/WebApi.csproj --prerelease";
    internal const string CachingExample2Description = "Add Redis with output caching using prerelease packages:";


    // aspire database
    internal class Database
    {

        internal const string DatabaseTitle = "database";
        internal const string DatabaseDescription = "Modify Aspire project to make it database ready.";
        internal const string DatabaseTypeOption = "Database type";
        internal const string DatabaseTypeDescription = "The type of database to add. Use 'npgsql-efcore' for PostgreSQL with EF Core or 'sqlserver-efcore' for SQL Server with EF Core.";

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

        internal static List<string> DatabaseTypeCustomValues = [.. DatabaseTypeDefaults.Keys];

        // aspire database examples
        internal const string DatabaseExample1 = "dotnet scaffold aspire database --type sqlserver-efcore --apphost-project C:/MyApp/MyApp.AppHost/MyApp.AppHost.csproj --project C:/MyApp/MyApp.Api/MyApp.Api.csproj";
        internal const string DatabaseExample1Description = "Add SQL Server with Entity Framework Core to an Aspire API project:";
        internal const string DatabaseExample2 = "dotnet scaffold aspire database --type npgsql-efcore --apphost-project C:/MyApp/AppHost.csproj --project C:/MyApp/WebApp.csproj";
        internal const string DatabaseExample2Description = "Add PostgreSQL with Entity Framework Core:";
    }

    // aspire storage
    internal const string StorageTitle = "storage";
    internal const string StorageDescription = "Modify Aspire project to make it storage ready.";
    internal const string StorageTypeOption = "Storage type";
    internal const string StorageTypeDescription = "The type of Azure storage to add. Options: 'azure-storage-queues' for queue messaging, 'azure-storage-blobs' for blob storage, or 'azure-data-tables' for table storage.";
    internal const string TypeCliOption = "--type";

    internal static List<string> StorageTypeCustomValues = ["azure-storage-queues", "azure-storage-blobs", "azure-data-tables"];

    // aspire storage examples
    internal const string StorageExample1 = "dotnet scaffold aspire storage --type azure-storage-blobs --apphost-project C:/MyApp/MyApp.AppHost/MyApp.AppHost.csproj --project C:/MyApp/MyApp.Worker/MyApp.Worker.csproj";
    internal const string StorageExample1Description = "Add Azure Blob Storage to an Aspire worker project:";
    internal const string StorageExample2 = "dotnet scaffold aspire storage --type azure-storage-queues --apphost-project C:/MyApp/AppHost.csproj --project C:/MyApp/MessageProcessor.csproj";
    internal const string StorageExample2Description = "Add Azure Storage Queues for message processing:";

    // options
    internal const string AppHostProjectOption = "Aspire App host project file";
    internal const string AppHostProjectDescription = "Absolute path to the Aspire App host project file (.csproj) that orchestrates your distributed application.";
    internal const string AppHostCliOption = "--apphost-project";

    internal const string ProjectOption = "Web or worker project file";
    internal const string ProjectOptionDescription = "Absolute path to the web or worker project file (.csproj) to be modified. This project should be referenced by the App host.";
    internal static string WorkerProjectCliOption = "--project";

    internal const string PrereleaseOption = "Include Prerelease packages?";
    internal const string PrereleaseDescription = "Include prerelease package versions when installing Aspire components. Useful for testing preview features.";
    internal const string PrereleaseCliOption = "--prerelease";
}
