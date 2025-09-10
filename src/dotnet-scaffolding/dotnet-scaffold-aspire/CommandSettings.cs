// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.Tools.Scaffold.Aspire;

/// <summary>
/// Represents settings used for scaffold commands, including project type, paths, and prerelease flag.
/// </summary>
internal class CommandSettings
{
    /// <summary>
    /// Gets or sets the type of the scaffold operation (e.g., database type, storage type).
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Gets or sets the path to the AppHost project.
    /// </summary>
    public required string AppHostProject { get; init; }

    /// <summary>
    /// Gets or sets the path to the target project for scaffolding.
    /// </summary>
    public required string Project { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether to use prerelease packages.
    /// </summary>
    public bool Prerelease { get; init; }
}
