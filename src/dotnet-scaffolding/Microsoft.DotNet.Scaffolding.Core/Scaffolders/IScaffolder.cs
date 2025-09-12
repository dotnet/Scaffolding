// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.Builder;

namespace Microsoft.DotNet.Scaffolding.Core.Scaffolders;

/// <summary>
/// Represents a scaffolder that can be executed to perform code generation or other scaffolding tasks.
/// </summary>
public interface IScaffolder
{
    /// <summary>
    /// Gets the name of the scaffolder. This will be used as the command line command to execute that scaffolder.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the display name of the scaffolder as it will be displayed in the dotnet-scaffold interactive UI.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Gets the categories in which the scaffolder will be organized in the dotnet-scaffold interactive UI (at least one default 'All').
    /// </summary>
    IEnumerable<string> Categories { get; }

    /// <summary>
    /// Gets the description of the scaffolder as it will be displayed in the dotnet-scaffold interactive UI.
    /// </summary>
    string? Description { get; }

    /// <summary>
    /// Gets the collection of options that can be used with this scaffolder.
    /// </summary>
    IEnumerable<ScaffolderOption> Options { get; }

    /// <summary>
    /// Executes the scaffolder based on the current context. Generally this will be called by the <see cref="IScaffoldRunner"/> and does not need to be called directly.
    /// </summary>
    /// <param name="context">The context for the scaffolder execution.</param>
    Task ExecuteAsync(ScaffolderContext context);
}
