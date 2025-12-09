// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.CodeAnalysis.Elfie.Serialization;
using Microsoft.DotNet.Scaffolding.Core.Model;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Common;

internal class PackageConstants
{
    public static class EfConstants
    {
        public const string SqlServer = "sqlserver-efcore";
        public const string SQLite = "sqlite-efcore";
        public const string CosmosDb = "cosmos-efcore";
        public const string Postgres = "npgsql-efcore";
        public static readonly Package EfCorePackage = new("Microsoft.EntityFrameworkCore", IsVersionRequired: true);
        public static readonly Package EfCoreToolsPackage = new("Microsoft.EntityFrameworkCore.Tools", IsVersionRequired: true);
        public static readonly Package SqlServerPackage = new("Microsoft.EntityFrameworkCore.SqlServer", IsVersionRequired: true);
        public static readonly Package SqlitePackage = new("Microsoft.EntityFrameworkCore.Sqlite", IsVersionRequired: true);
        public static readonly Package CosmosPackage = new("Microsoft.EntityFrameworkCore.Cosmos", IsVersionRequired: true);
        public static readonly Package PostgresPackage = new("Npgsql.EntityFrameworkCore.PostgreSQL", IsVersionRequired: true);
        public const string ConnectionStringVariableName = "connectionString";

        /// <summary>
        /// Maps provider keys to their corresponding NuGet package names.
        /// </summary>
        public static readonly IDictionary<string, Package> EfPackagesDict = new Dictionary<string, Package>
        {
            { SqlServer, SqlServerPackage },
            { SQLite, SqlitePackage },
            { CosmosDb, CosmosPackage },
            { Postgres, PostgresPackage }
        };

        /// <summary>
        /// Maps provider keys to their corresponding Identity EF package names.
        /// </summary>
        public static readonly IDictionary<string, Package> IdentityEfPackagesDict = new Dictionary<string, Package>
        {
            { SqlServer, SqlServerPackage },
            { SQLite, SqlitePackage },
        };

        internal static Dictionary<string, string> UseDatabaseMethods = new()
        {
            { SqlServer, "UseSqlServer" },
            { SQLite, "UseSqlite" },
            { Postgres, "UseNpgsql" },
            { CosmosDb, "UseCosmos" }
        };
    }

    public static class AspNetCorePackages
    {
        public static readonly Package QuickGridEfAdapterPackage = new("Microsoft.AspNetCore.Components.QuickGrid.EntityFrameworkAdapter", IsVersionRequired: true);
        public static readonly Package AspNetCoreDiagnosticsEfCorePackage = new("Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore", IsVersionRequired: true);
        public static readonly Package OpenApiPackage = new("Microsoft.AspNetCore.OpenApi", IsVersionRequired: true);
        public static readonly Package AspNetCoreIdentityEfPackage = new("Microsoft.AspNetCore.Identity.EntityFrameworkCore", IsVersionRequired: true);
        public static readonly Package AspNetCoreIdentityUiPackage = new("Microsoft.AspNetCore.Identity.UI", IsVersionRequired: true);
    }
}
