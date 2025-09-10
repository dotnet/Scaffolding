// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Common;

/// <summary>
/// Represents information about a DbContext class, including its name, path, namespace, provider, and related properties.
/// </summary>
internal class DbContextInfo
{
    /// <summary>
    /// Gets or sets the name of the DbContext class.
    /// </summary>
    public string? DbContextClassName { get; set; }
    /// <summary>
    /// Gets or sets the file path of the DbContext class.
    /// </summary>
    public string? DbContextClassPath { get; set; }
    /// <summary>
    /// Gets or sets the namespace of the DbContext class.
    /// </summary>
    public string? DbContextNamespace { get; set; }
    /// <summary>
    /// Gets or sets the database provider (e.g., SQL Server, SQLite).
    /// </summary>
    public string? DatabaseProvider { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether this is an EF scenario.
    /// </summary>
    public bool EfScenario { get; set; } = false;
    /// <summary>
    /// Gets or sets the entity set variable name for the DbContext.
    /// </summary>
    public string? EntitySetVariableName { get; set; }
    /// <summary>
    /// Gets or sets the new DbSet statement for the DbContext.
    /// </summary>
    public string? NewDbSetStatement { get; set; }
}
