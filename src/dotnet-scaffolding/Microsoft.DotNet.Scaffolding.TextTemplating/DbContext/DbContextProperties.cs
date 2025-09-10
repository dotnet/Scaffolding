// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Microsoft.DotNet.Scaffolding.TextTemplating.DbContext;

/// <summary>
/// Holds properties required for generating a new DbContext class using T4 templates.
/// </summary>
internal class DbContextProperties
{
    /// <summary>
    /// The name of the DbContext class.
    /// </summary>
    public string DbContextName { get; set; } = Constants.NewDbContext;
    /// <summary>
    /// The output path for the generated DbContext file.
    /// </summary>
    public string DbContextPath { get; set; } = default!;
    /// <summary>
    /// The DbSet statement(s) to include in the DbContext.
    /// </summary>
    public string? DbSetStatement { get; set; }
    /// <summary>
    /// The connection string for the new database.
    /// </summary>
    public string? NewDbConnectionString { get; set; }
    /// <summary>
    /// Indicates if this is an IdentityDbContext.
    /// </summary>
    public bool IsIdentityDbContext { get; set; } = false;
    /// <summary>
    /// The full type name of the Identity user.
    /// </summary>
    public string FullIdentityUserName { get; set; } = "IdentityUser";
}
