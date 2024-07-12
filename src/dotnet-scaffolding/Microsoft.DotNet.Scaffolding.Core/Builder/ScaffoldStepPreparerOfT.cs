// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;

namespace Microsoft.DotNet.Scaffolding.Core.Builder;

public class ScaffoldStepPreparer<TStep> : ScaffoldStepPreparer where TStep : ScaffoldStep
{
    public Action<ScaffoldStepConfigurator<TStep>>? PreExecute { get; set; }
    public Action<ScaffoldStepConfigurator<TStep>>? PostExecute { get; set; }

    internal override Type GetStepType() => typeof(TStep);

    internal override void RunPreExecute(ScaffoldStep scaffoldStep, ScaffolderContext context)
        => PreExecute?.Invoke(GetConfigurator(scaffoldStep, context));

    internal override void RunPostExecute(ScaffoldStep scaffoldStep, ScaffolderContext context)
        => PostExecute?.Invoke(GetConfigurator(scaffoldStep, context));

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
