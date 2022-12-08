using System.Collections.Generic;

namespace Microsoft.DotNet.Scaffolding.Shared
{
    public enum DbProvider
    {
        SqlServer, SQLite, CosmosDb, Postgres, Existing
    }

    public static class EfConstants
    {
        public static string SqlServer = DbProvider.SqlServer.ToString();
        public static string SQLite = DbProvider.SQLite.ToString();
        public static string CosmosDb = DbProvider.CosmosDb.ToString();
        public static string Postgres = DbProvider.Postgres.ToString();
        public const string EfDesignPackageName = "Microsoft.EntityFrameworkCore.Design";
        public const string SqlServerPackageName = "Microsoft.EntityFrameworkCore.SqlServer";
        public const string SqlitePackageName = "Microsoft.EntityFrameworkCore.Sqlite";
        public const string CosmosPakcageName = "Microsoft.EntityFrameworkCore.Cosmos";
        public const string PostgresPackageName = "Npgsql.EntityFrameworkCore.PostgreSQL";
        public const string SQLConnectionStringFormat = "Server=(localdb)\\mssqllocaldb;Database={0};Trusted_Connection=True;MultipleActiveResultSets=true";
        public const string SQLiteConnectionStringFormat = "Data Source={0}.db";
        public const string CosmosDbConnectionStringFormat = "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
        public const string PostgresConnectionStringFormat = "server=localhost;username=postgres;database={0}";
        public static readonly IDictionary<DbProvider, string> ConnectionStringsDict = new Dictionary<DbProvider, string>
        {
            { DbProvider.SqlServer, SQLConnectionStringFormat },
            { DbProvider.SQLite, SQLiteConnectionStringFormat },
            { DbProvider.CosmosDb, CosmosDbConnectionStringFormat },
            { DbProvider.Postgres, PostgresConnectionStringFormat }
        };

        public static readonly IDictionary<DbProvider, string> EfPackagesDict = new Dictionary<DbProvider, string>
        {
            { DbProvider.SqlServer, SqlServerPackageName },
            { DbProvider.SQLite, SqlitePackageName },
            { DbProvider.CosmosDb, CosmosPakcageName },
            { DbProvider.Postgres, PostgresPackageName }
        };

        public static readonly IList<string> IdentityDbProviders = new List<string> { SqlServer, SQLite };
        public static readonly IList<string> AllDbProviders = new List<string> { SqlServer, SQLite, CosmosDb, Postgres };
    }
}
