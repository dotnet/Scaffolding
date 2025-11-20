// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;

namespace Microsoft.DotNet.Scaffolding.Core.Builder;

/// <summary>
/// Abstract base class for preparing scaffold steps with pre- and post-execution logic.
/// </summary>
public abstract class ScaffoldStepPreparer
{
    /// <summary>
    /// Gets the type of the scaffold step.
    /// </summary>
    internal abstract Type GetStepType();

    /// <summary>
    /// Runs pre-execution logic for the scaffold step.
    /// </summary>
    internal abstract void RunPreExecute(ScaffoldStep scaffoldStep, ScaffolderContext context);
    /// <summary>
    /// Runs post-execution logic for the scaffold step.
    /// </summary>
    internal abstract void RunPostExecute(ScaffoldStep scaffoldStep, ScaffolderContext context);
}
