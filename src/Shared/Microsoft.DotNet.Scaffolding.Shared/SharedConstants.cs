using System.Collections.Generic;

namespace Microsoft.DotNet.Scaffolding.Shared
{
    public enum DbType
    {
        SqlServer, SQLite, CosmosDb, Postgres
    }

    public static class EfConstants
    {
        public static string SqlServer = DbType.SqlServer.ToString();
        public static string SQLite = DbType.SQLite.ToString();
        public static string CosmosDb = DbType.CosmosDb.ToString();
        public static string Postgres = DbType.Postgres.ToString();
        public const string EfDesignPackageName = "Microsoft.EntityFrameworkCore.Design";
        public const string SqlServerPackageName = "Microsoft.EntityFrameworkCore.SqlServer";
        public const string SqlitePackageName = "Microsoft.EntityFrameworkCore.Sqlite";
        public const string CosmosPakcageName = "Microsoft.EntityFrameworkCore.Cosmos";
        public const string PostgresPackageName = "Npgsql.EntityFrameworkCore.PostgreSQL";
        public const string SQLConnectionStringFormat = "Server=(localdb)\\mssqllocaldb;Database={0};Trusted_Connection=True;MultipleActiveResultSets=true";
        public const string SQLiteConnectionStringFormat = "Data Source={0}.db";
        public const string CosmosDbConnectionStringFormat = "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
        public const string PostgresConnectionStringFormat = "server=localhost;username=postgres;database={0}";
        public static readonly IDictionary<DbType, string> ConnectionStringsDict = new Dictionary<DbType, string>
        {
            { DbType.SqlServer, SQLConnectionStringFormat },
            { DbType.SQLite, SQLiteConnectionStringFormat },
            { DbType.CosmosDb, CosmosDbConnectionStringFormat },
            { DbType.Postgres, PostgresConnectionStringFormat }
        };

        public static readonly IDictionary<DbType, string> EfPackagesDict = new Dictionary<DbType, string>
        {
            { DbType.SqlServer, SqlServerPackageName },
            { DbType.SQLite, SqlitePackageName },
            { DbType.CosmosDb, CosmosPakcageName },
            { DbType.Postgres, PostgresPackageName }
        };

        public static readonly IList<string> IdentityDbTypes = new List<string> { EfConstants.SqlServer, EfConstants.SQLite };
        public static readonly IList<string> AllDbTypes = new List<string> { EfConstants.SqlServer, EfConstants.SQLite, EfConstants.CosmosDb, EfConstants.Postgres };
    }
}
