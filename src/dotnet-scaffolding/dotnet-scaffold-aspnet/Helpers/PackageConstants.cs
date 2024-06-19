// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Collections.Generic;
using Microsoft.DotNet.Scaffolding.Helpers.General;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;

internal class PackageConstants
{
    public static class EfConstants
    {
        public const string SqlServer = "sqlserver-efcore";
        public const string SQLite = "sqlite-efore";
        public const string CosmosDb = "cosmos-efcore";
        public const string Postgres = "npgsql-efcore";
        public const string EfToolsPackageName = "Microsoft.EntityFrameworkCore.Tools";
        public const string EfCorePackageName = "Microsoft.EntityFrameworkCore";
        public const string SqlServerPackageName = "Microsoft.EntityFrameworkCore.SqlServer";
        public const string SqlitePackageName = "Microsoft.EntityFrameworkCore.Sqlite";
        public const string CosmosPakcageName = "Microsoft.EntityFrameworkCore.Cosmos";
        public const string PostgresPackageName = "Npgsql.EntityFrameworkCore.PostgreSQL";
        public const string SQLConnectionStringFormat = "Server=(localdb)\\mssqllocaldb;Database={0};Trusted_Connection=True;MultipleActiveResultSets=true";
        public const string SQLiteConnectionStringFormat = "Data Source={0}.db";
        public const string CosmosDbConnectionStringFormat = "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
        public const string PostgresConnectionStringFormat = "server=localhost;username=postgres;database={0}";

        public static readonly IDictionary<string, string> ConnectionStringsDict = new Dictionary<string, string>
        {
            { SqlServer, SQLConnectionStringFormat },
            { SQLite, SQLiteConnectionStringFormat },
            { CosmosDb, CosmosDbConnectionStringFormat },
            { Postgres, PostgresConnectionStringFormat }
        };

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
}
