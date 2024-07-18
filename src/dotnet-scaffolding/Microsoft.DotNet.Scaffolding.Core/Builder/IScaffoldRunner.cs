// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.Scaffolders;

namespace Microsoft.DotNet.Scaffolding.Core.Builder;

public interface IScaffoldRunner
{
    /// <summary>
    /// The collection of configured <see cref="IScaffolder"/>s that can be executed by the runner
    /// </summary>
    IEnumerable<IScaffolder>? Scaffolders { get; set; }

    /// <summary>
    /// Executes the scaffolders based on the provided arguments
    /// </summary>
    Task RunAsync(string[] args);
}
