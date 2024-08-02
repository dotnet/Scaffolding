// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Models;

internal class MinimalApiModel
{
    //Minimal API info
    public bool OpenAPI { get; set; }
    public bool UseTypedResults { get; set; } = true;
    //Endpoints class info
    public string? EndpointsClassName { get; set; }
    public string EndpointsFileName { get; set; } = default!;
    public string? EndpointsPath { get; set; }
    public string? EndpointsNamespace { get; set; }
    public string? EndpointsMethodName { get; set; }
    public required DbContextInfo DbContextInfo { get; init; }
    public required ModelInfo ModelInfo { get; init; }
    public required ProjectInfo ProjectInfo { get; init; }
}
