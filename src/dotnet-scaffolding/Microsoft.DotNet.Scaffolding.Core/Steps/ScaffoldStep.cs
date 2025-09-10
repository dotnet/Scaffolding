// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;

namespace Microsoft.DotNet.Scaffolding.Core.Steps;

/// <summary>
/// Represents an abstract step in the scaffolding process.
/// </summary>
public abstract class ScaffoldStep
{
    /// <summary>
    /// Executes the step logic asynchronously.
    /// </summary>
    /// <param name="context">The scaffolder context for the current operation.</param>
    /// <param name="cancellationToken">A cancellation token for the operation.</param>
    /// <returns>True if the step executed successfully; otherwise, false.</returns>
    public abstract Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets or sets a value indicating whether to continue execution if this step fails.
    /// </summary>
    public bool ContinueOnError { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to skip this step during execution.
    /// </summary>
    public bool SkipStep { get; set; }
}
