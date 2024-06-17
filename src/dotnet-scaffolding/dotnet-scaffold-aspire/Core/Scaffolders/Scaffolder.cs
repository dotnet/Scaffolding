// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Steps;

namespace Microsoft.DotNet.Tools.Scaffold.Aspire.Core.Scaffolders;

internal abstract class Scaffolder
{
    protected readonly List<ScaffoldStep> _steps = [];
    public virtual async Task ExecuteAsync()
    {
        foreach (var step in _steps)
        {
            try
            {
                await step.ExecuteAsync();
                if (step is OutputScaffoldStep outputStep)
                {
                    // No, don't do this
                    // Instead, build up some sort of a
                    // property bag of outputs that can be queried
                    // by subsequent steps

                    Output = outputStep.Output;
                }
            }
            catch
            {
                if (!step.ContinueOnError)
                {
                    return;
                }
            }
        }
    }
}

