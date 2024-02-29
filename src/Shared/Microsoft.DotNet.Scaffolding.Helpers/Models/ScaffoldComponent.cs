// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.ExtensibilityModel;

namespace Microsoft.DotNet.Scaffolding.Helpers.Models;

/// <summary>
/// Cached data for a scaffolding component
/// </summary>
public class ScaffoldComponent
{
    public string? DisplayName { get; set; }
    public string? Name { get; set; }
    public List<Parameter>? Parameters { get; set; }
}
