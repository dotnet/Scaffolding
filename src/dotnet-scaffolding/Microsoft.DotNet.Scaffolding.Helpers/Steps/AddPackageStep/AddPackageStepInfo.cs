// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Helpers.Services;

namespace Microsoft.DotNet.Scaffolding.Helpers.Steps.AddPackageStep;

internal class AddPackageStepInfo
{
    public required IList<string?> PackageNames { get; init; }
    public required string ProjectPath { get; init; }
    public required ILogger Logger { get; init; }
    public bool Prerelease { get; set; } = false;
}
