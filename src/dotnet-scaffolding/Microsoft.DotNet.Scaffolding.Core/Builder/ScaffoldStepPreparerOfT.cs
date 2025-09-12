// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;

namespace Microsoft.DotNet.Scaffolding.Core.Builder;

/// <summary>
/// Preparer for a specific scaffold step type, allowing configuration of pre- and post-execution actions.
/// </summary>
public class ScaffoldStepPreparer<TStep> : ScaffoldStepPreparer where TStep : ScaffoldStep
{
    /// <summary>
    /// Action to run before executing the step.
    /// </summary>
    public Action<ScaffoldStepConfigurator<TStep>>? PreExecute { get; set; }
    /// <summary>
    /// Action to run after executing the step.
    /// </summary>
    public Action<ScaffoldStepConfigurator<TStep>>? PostExecute { get; set; }

    /// <inheritdoc/>
    internal override Type GetStepType() => typeof(TStep);

    /// <inheritdoc/>
    internal override void RunPreExecute(ScaffoldStep scaffoldStep, ScaffolderContext context)
        => PreExecute?.Invoke(GetConfigurator(scaffoldStep, context));

    /// <inheritdoc/>
    internal override void RunPostExecute(ScaffoldStep scaffoldStep, ScaffolderContext context)
        => PostExecute?.Invoke(GetConfigurator(scaffoldStep, context));

    /// <summary>
    /// Gets the configurator for the scaffold step and context.
    /// </summary>
    private static ScaffoldStepConfigurator<TStep> GetConfigurator(ScaffoldStep scaffoldStep, ScaffolderContext context)
    {
        Debug.Assert(scaffoldStep is TStep);

        return new ScaffoldStepConfigurator<TStep>()
        {
            Context = context,
            Step = (TStep)scaffoldStep
        };
    }
}
