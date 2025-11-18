// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Microsoft.DotNet.Scaffolding.TextTemplating.DbContext;

/// <summary>
/// Provides default connection string templates for various database providers.
/// </summary>
internal static class DbContextHelper
{
    /// <summary>
    /// Default connection string for SQL Server LocalDB.
    /// </summary>
    internal static DbContextProperties SqlServerDefaults = new()
    {
        NewDbConnectionString = "Server=(localdb)\\mssqllocaldb;Database={0};Trusted_Connection=True;MultipleActiveResultSets=true"
    };

    /// <summary>
    /// Default connection string for SQLite.
    /// </summary>
    internal static DbContextProperties SqliteDefaults = new()
    {
        NewDbConnectionString = "Data Source={0}.db"
    };

    /// <summary>
    /// Default connection string for Cosmos DB emulator.
    /// </summary>
    internal static DbContextProperties CosmosDefaults = new()
    {
        NewDbConnectionString = "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="
    };

    /// <summary>
    /// Default connection string for PostgreSQL (Npgsql).
    /// </summary>
    internal static DbContextProperties NpgsqlDefaults = new()
    {
        NewDbConnectionString = "server=localhost;username=postgres;database={0}"
    };
}
