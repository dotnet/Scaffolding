// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Microsoft.DotNet.Tools.Scaffold.Aspire.Helpers
{
    internal class PackageConstants
    {
        internal class CachingPackages
        {
            internal const string AppHostRedisPackageName = "Aspire.Hosting.Redis";
            internal const string WebAppRedisPackageName = "Aspire.StackExchange.Redis";
            internal const string WebAppRedisOutputCachingPackageName = "Aspire.StackExchange.Redis.OutputCaching";
            internal static readonly Dictionary<string, string> CachingPackagesDict = new()
            {
                { "redis", WebAppRedisPackageName },
                { "redis-with-output-caching", WebAppRedisOutputCachingPackageName }
            };
        }

        internal class StoragePackages
        {
            internal const string AppHostStoragePackageName = "Aspire.Hosting.Azure.Storage";
            internal const string ApiServiceBlobsPackageName = "Aspire.Azure.Storage.Blobs";
            internal const string ApiServiceQueuesPackageName = "Aspire.Azure.Storage.Queues";
            internal const string ApiServiceTablesPackageName = "Aspire.Azure.Data.Tables";
            internal static readonly Dictionary<string, string> StoragePackagesDict = new()
            {
                { "azure-storage-queues", ApiServiceQueuesPackageName },
                { "azure-storage-blobs", ApiServiceBlobsPackageName },
                { "azure-data-tables", ApiServiceTablesPackageName }
            };
        }

        internal class DatabasePackages
        {
            internal const string AppHostPostgresPackageName = "Aspire.Hosting.PostgreSQL";
            internal const string AppHostSqlServerPackageName = "Aspire.Hosting.SqlServer";
            internal const string AppHostCosmosPackageName = "Aspire.Hosting.Azure.CosmosDB";
            internal const string ApiServicePostgresEfCorePackageName = "Aspire.Npgsql.EntityFrameworkCore.PostgreSQL";
            internal const string ApiServiceSqlServerPackageName = "Aspire.Microsoft.EntityFrameworkCore.SqlServer";
            internal const string ApiServiceCosmosPackageName = "Aspire.Microsoft.EntityFrameworkCore.Cosmos";
            internal static readonly Dictionary<string, string> DatabasePackagesAppHostDict = new()
            {
                { "npgsql-efcore", AppHostPostgresPackageName },
                { "sqlserver-efcore", AppHostSqlServerPackageName },
                { "cosmos-efcore", AppHostCosmosPackageName }
            };

            internal static readonly Dictionary<string, string> DatabasePackagesApiServiceDict = new()
            {
                { "npgsql-efcore", ApiServicePostgresEfCorePackageName },
                { "sqlserver-efcore", ApiServiceSqlServerPackageName },
                { "cosmos-efcore", ApiServiceCosmosPackageName }
            };
        }
    }
}
