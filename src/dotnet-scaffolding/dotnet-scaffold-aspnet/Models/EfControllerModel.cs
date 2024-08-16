// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Models;

internal class EfControllerModel
{
    public required string ControllerType { get; set; }
    public required string ControllerName { get; set; }
    public required string ControllerOutputPath { get; set; }
    public required DbContextInfo DbContextInfo { get; init; }
    public required ModelInfo ModelInfo { get; init; }
    public required ProjectInfo ProjectInfo { get; init; }
}
