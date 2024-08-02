// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Common;

internal class PackageConstants
{
    public static class EfConstants
    {
        public const string SqlServer = "sqlserver-efcore";
        public const string SQLite = "sqlite-efcore";
        public const string CosmosDb = "cosmos-efcore";
        public const string Postgres = "npgsql-efcore";
        public const string EfToolsPackageName = "Microsoft.EntityFrameworkCore.Tools";
        public const string EfCorePackageName = "Microsoft.EntityFrameworkCore";
        public const string SqlServerPackageName = "Microsoft.EntityFrameworkCore.SqlServer";
        public const string SqlitePackageName = "Microsoft.EntityFrameworkCore.Sqlite";
        public const string CosmosPakcageName = "Microsoft.EntityFrameworkCore.Cosmos";
        public const string PostgresPackageName = "Npgsql.EntityFrameworkCore.PostgreSQL";
        public static readonly IDictionary<string, string> EfPackagesDict = new Dictionary<string, string>
        {
            { SqlServer, SqlServerPackageName },
            { SQLite, SqlitePackageName },
            { CosmosDb, CosmosPakcageName },
            { Postgres, PostgresPackageName }
        };

        internal static Dictionary<string, string> UseDatabaseMethods = new()
        {
            { Postgres, "UsePostgres" },
            { SqlServer, "UseSqlServer" },
            { SQLite, "UseSqlite" },
            { CosmosDb, "UseCosmos" }
        };
    }

    public static class AspNetCorePackages
    {
        public const string QuickGridEfAdapterPackageName = "Microsoft.AspNetCore.Components.QuickGrid.EntityFrameworkAdapter";
        public const string AspNetCoreDiagnosticsEfCorePackageName = "Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore";
        public const string OpenApiPackageName = "Microsoft.AspNetCore.OpenApi";
    }
}
