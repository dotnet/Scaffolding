// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;

namespace Microsoft.DotNet.Scaffolding.Core.Builder;

public abstract class ScaffoldStepPreparer
{
    internal abstract Type GetStepType();

    internal abstract void RunPreExecute(ScaffoldStep scaffoldStep, ScaffolderContext context);
    internal abstract void RunPostExecute(ScaffoldStep scaffoldStep, ScaffolderContext context);
}
