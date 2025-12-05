// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// PackageConstants contains constant values for package names and mappings used in Aspire scaffolding.
// It organizes package names for caching, storage, and database scenarios, and provides lookup dictionaries for each.

using Microsoft.DotNet.Scaffolding.Core.Model;

namespace Microsoft.DotNet.Tools.Scaffold.Aspire.Helpers
{
    internal class PackageConstants
    {
        internal class CachingPackages
        {
            internal static readonly Package AppHostRedisPackage = new("Aspire.Hosting.Redis");
            internal static readonly Package WebAppRedisPackage = new("Aspire.StackExchange.Redis");
            internal static readonly Package WebAppRedisOutputCachingPackage = new("Aspire.StackExchange.Redis.OutputCaching");
            internal static readonly Dictionary<string, Package> CachingPackagesDict = new()
            {
                { "redis", WebAppRedisPackage },
                { "redis-with-output-caching", WebAppRedisOutputCachingPackage }
            };
        }

        internal class StoragePackages
        {
            internal static readonly Package AppHostStoragePackage = new("Aspire.Hosting.Azure.Storage");
            internal static readonly Package ApiServiceBlobsPackage = new("Aspire.Azure.Storage.Blobs");
            internal static readonly Package ApiServiceQueuesPackage = new("Aspire.Azure.Storage.Queues");
            internal static readonly Package ApiServiceTablesPackage = new("Aspire.Azure.Data.Tables");
            internal static readonly Dictionary<string, Package> StoragePackagesDict = new()
            {
                { "azure-storage-queues", ApiServiceQueuesPackage },
                { "azure-storage-blobs", ApiServiceBlobsPackage },
                { "azure-data-tables", ApiServiceTablesPackage }
            };
        }

        internal class DatabasePackages
        {
            internal static readonly Package AppHostPostgresPackage = new("Aspire.Hosting.PostgreSQL");
            internal static readonly Package AppHostSqlServerPackage = new("Aspire.Hosting.SqlServer");
            internal static readonly Package AppHostCosmosPackage = new("Aspire.Hosting.Azure.CosmosDB");
            internal static readonly Package ApiServicePostgresEfCorePackage = new("Aspire.Npgsql.EntityFrameworkCore.PostgreSQL");
            internal static readonly Package ApiServiceSqlServerEfCorePackage = new("Aspire.Microsoft.EntityFrameworkCore.SqlServer");
            internal static readonly Package ApiServiceCosmosEfCorePackage = new("Aspire.Microsoft.EntityFrameworkCore.Cosmos");
            internal static readonly Dictionary<string, Package> DatabasePackagesAppHostDict = new()
            {
                { "npgsql-efcore", AppHostPostgresPackage },
                { "sqlserver-efcore", AppHostSqlServerPackage },
                { "cosmos-efcore", AppHostCosmosPackage }
            };

            internal static readonly Dictionary<string, Package> DatabasePackagesApiServiceDict = new()
            {
                { "npgsql-efcore", ApiServicePostgresEfCorePackage },
                { "sqlserver-efcore", ApiServiceSqlServerEfCorePackage },
                { "cosmos-efcore", ApiServiceCosmosEfCorePackage }
            };
        }
    }
}
