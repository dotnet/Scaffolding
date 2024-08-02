// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
internal class DbContextInfo
{
    //DbContext info
    public string? DbContextClassName { get; set; }
    public string? DbContextClassPath { get; set; }
    public string? DbContextNamespace { get; set; }
    public string? DatabaseProvider { get; set; }
    public bool EfScenario { get; set; } = false;
    public string? EntitySetVariableName { get; set; }
    public string? NewDbSetStatement { get; set; }
}
