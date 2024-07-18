// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.Builder;

namespace Microsoft.DotNet.Scaffolding.Core.Scaffolders;

public interface IScaffolder
{
    /// <summary>
    /// The name of the scaffolder. This will be used as the command line command to execute that scaffolder.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// The display name of the scaffolder as it will be displayed in the dotnet-scaffold interactive UI
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// The category in which the scaffolder will be organized in the dotnet-scaffold interactive UI
    /// </summary>
    string Category { get; }

    /// <summary>
    /// The description of the scaffolder as it will be displayed in the dotnet-scaffold interactive UI
    /// </summary>
    string? Description { get; }

    /// <summary>
    /// The collection of options that can be used with this scaffolder
    /// </summary>
    IEnumerable<ScaffolderOption> Options { get; }

    /// <summary>
    /// Executes the scaffolder based on the current context. Generally this will be called by the <see cref="IScaffoldRunner"/> and does not need to be called directly.
    /// </summary>
    Task ExecuteAsync(ScaffolderContext context);
}
