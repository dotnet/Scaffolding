// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Microsoft.DotNet.Scaffolding.Helpers.General;
internal class DbContextProperties
{
    public string DbContextName { get; set; } = "NewDbContext";
    public required string AddDbMethod { get; set; }
    public string? AddDbContextMethod { get; set; }
    public string? DbName { get; set; }
    public string? DbType { get; set; }
    public string? DbSetStatement { get; set; }
}
