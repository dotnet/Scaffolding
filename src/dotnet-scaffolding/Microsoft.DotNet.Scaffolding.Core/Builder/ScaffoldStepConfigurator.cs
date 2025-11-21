// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;

namespace Microsoft.DotNet.Scaffolding.Core.Builder;

/// <summary>
/// Configurator for a scaffold step, providing access to the step and its context.
/// </summary>
public class ScaffoldStepConfigurator<TStep> where TStep : ScaffoldStep
{
    /// <summary>
    /// The scaffold step instance.
    /// </summary>
    public required TStep Step { get; init; }
    /// <summary>
    /// The context for the scaffolder.
    /// </summary>
    public required ScaffolderContext Context { get; init; }
}
