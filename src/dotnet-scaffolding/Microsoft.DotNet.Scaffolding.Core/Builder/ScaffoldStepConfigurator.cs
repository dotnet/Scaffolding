// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;

namespace Microsoft.DotNet.Scaffolding.Core.Builder;

public class ScaffoldStepConfigurator<TStep> where TStep : ScaffoldStep
{
    public required TStep Step { get; init; }
    public required ScaffolderContext Context { get; init; }
}
