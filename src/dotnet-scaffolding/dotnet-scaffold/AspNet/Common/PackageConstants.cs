// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Common;

/// <summary>
/// Contains constants for NuGet package names and EF Core provider mappings used in ASP.NET scaffolding.
/// </summary>
internal class PackageConstants
{
    /// <summary>
    /// Constants and mappings related to Entity Framework Core providers and packages.
    /// </summary>
    public static class EfConstants
    {
        public const string SqlServer = "sqlserver-efcore";
        public const string SQLite = "sqlite-efcore";
        public const string CosmosDb = "cosmos-efcore";
        public const string Postgres = "npgsql-efcore";
        public const string EfCorePackageName = "Microsoft.EntityFrameworkCore";
        public const string EfCoreToolsPackageName = "Microsoft.EntityFrameworkCore.Tools";
        public const string SqlServerPackageName = "Microsoft.EntityFrameworkCore.SqlServer";
        public const string SqlitePackageName = "Microsoft.EntityFrameworkCore.Sqlite";
        public const string CosmosPakcageName = "Microsoft.EntityFrameworkCore.Cosmos";
        public const string PostgresPackageName = "Npgsql.EntityFrameworkCore.PostgreSQL";
        public const string ConnectionStringVariableName = "connectionString";
        /// <summary>
        /// Maps provider keys to their corresponding NuGet package names.
        /// </summary>
        public static readonly IDictionary<string, string> EfPackagesDict = new Dictionary<string, string>
        {
            { SqlServer, SqlServerPackageName },
            { SQLite, SqlitePackageName },
            { CosmosDb, CosmosPakcageName },
            { Postgres, PostgresPackageName }
        };

        /// <summary>
        /// Maps provider keys to their corresponding Identity EF package names.
        /// </summary>
        public static readonly IDictionary<string, string> IdentityEfPackagesDict = new Dictionary<string, string>
        {
            { SqlServer, SqlServerPackageName },
            { SQLite, SqlitePackageName },
        };

        /// <summary>
        /// Maps provider keys to their corresponding UseDatabase method names.
        /// </summary>
        internal static Dictionary<string, string> UseDatabaseMethods = new()
        {
            { SqlServer, "UseSqlServer" },
            { SQLite, "UseSqlite" },
            { Postgres, "UseNpgsql" },
            { CosmosDb, "UseCosmos" }
        };
    }

    /// <summary>
    /// Constants for ASP.NET Core related NuGet package names.
    /// </summary>
    public static class AspNetCorePackages
    {
        public const string QuickGridEfAdapterPackageName = "Microsoft.AspNetCore.Components.QuickGrid.EntityFrameworkAdapter";
        public const string AspNetCoreDiagnosticsEfCorePackageName = "Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore";
        public const string OpenApiPackageName = "Microsoft.AspNetCore.OpenApi";
        public const string AspNetCoreIdentityEfPackageName = "Microsoft.AspNetCore.Identity.EntityFrameworkCore";
        public const string AspNetCoreIdentityUiPackageName = "Microsoft.AspNetCore.Identity.UI";
        public const string AspNetCoreComponentsWebAssemblyAuthenticationPackageName = "Microsoft.AspNetCore.Components.WebAssembly.Authentication";
        public const string AspNetCoreAuthenticationJwtBearerPackageName = "Microsoft.AspNetCore.Authentication.JwtBearer";
        public const string AspNetCoreAuthenticationOpenIdConnectPackageName = "Microsoft.AspNetCore.Authentication.OpenIdConnect";
        public const string MicrosoftIdentityWebPackageName = "Microsoft.Identity.Web";
    }
}
