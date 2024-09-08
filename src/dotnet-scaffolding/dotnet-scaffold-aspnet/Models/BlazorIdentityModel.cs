// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Models;

internal class BlazorIdentityModel
{
    public required DbContextInfo DbContextInfo { get; init; }
    public required ProjectInfo ProjectInfo { get; init; }
    public required string BlazorIdentityNamespace { get; init; }
    public string? BlazorLayoutNamespace { get; set; }
    public required string UserClassName { get; internal set; }
    public required string UserClassNamespace { get; internal set; }
    public string? DbContextNamespace { get; set; }
    public required string DbContextName { get; set; }
    //Database type eg. SQL Server or SQLite
    public string? DatabaseProvider { get; set; }
    public required string BaseOutputPath { get; set; }
    public bool Overwrite { get; set; }
}
