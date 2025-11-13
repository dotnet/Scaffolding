// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.Scaffolding.Core.Model;

/// <summary>
/// Represents a NuGet package with a specified name and optional version.
/// </summary>
/// <param name="name">The name of the package. Cannot be null.</param>
/// <param name="version">The version of the package, or null to indicate the latest version.</param>
public class Package(string name, string? version = null)
{
    public string Name { get; } = name;

    //null version indicates latest and version specfication is not needed
    public string? Version { get; } = version;
}
