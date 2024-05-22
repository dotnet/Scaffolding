// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Microsoft.DotNet.Scaffolding.Helpers.General;
public class DbContextProperties
{
    public string DbContextName { get; } = "NewDbContext";
    public required string AddDbMethod { get; init; }
    public required string AddDbContextMethod { get; init; }
    public required string DbName { get; init; }
    public required string DbType { get; init; }
}
