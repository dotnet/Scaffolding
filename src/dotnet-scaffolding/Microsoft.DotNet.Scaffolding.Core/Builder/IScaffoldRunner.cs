// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using System.CommandLine.Invocation;

namespace Microsoft.DotNet.Scaffolding.Core.Builder;

/// <summary>
/// Interface for executing scaffolders and managing their collection.
/// </summary>
public interface IScaffoldRunner
{
    /// <summary>
    /// The collection of configured <see cref="IScaffolder"/>s that can be executed by the runner
    /// </summary>
    IEnumerable<IScaffolder>? Scaffolders { get; set; }

    IEnumerable<ScaffolderOption>? Options { get; set; }

    /// <summary>
    /// Executes the scaffolders based on the provided arguments
    /// </summary>
    Task RunAsync(string[] args);

    /// <summary>
    /// Adds Handler to the RootCommand doing the action passed in the handle parameter
    /// </summary>
    void AddHandler(Func<InvocationContext, Task> handle);
}
