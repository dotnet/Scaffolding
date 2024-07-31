// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Microsoft.DotNet.Scaffolding.TextTemplating.DbContext;

internal class DbContextProperties
{
    public string DbContextName { get; set; } = "NewDbContext";
    public string? DbContextPath { get; set; }
    public string? DbSetStatement { get; set; }
    public string? NewDbConnectionString { get; set; }
}
