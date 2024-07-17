// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;

namespace Microsoft.DotNet.Scaffolding.Core.Builder;

public interface IScaffoldBuilder
{
    IScaffoldBuilder WithDisplayName(string displayName);
    IScaffoldBuilder WithCategory(string category);
    IScaffoldBuilder WithDescription(string description);
    IScaffoldBuilder WithOption(ScaffolderOption option);
    IScaffoldBuilder WithStep<TStep>(Action<ScaffoldStepConfigurator<TStep>>? preExecute = null, Action<ScaffoldStepConfigurator<TStep>>? postExecute = null) where TStep : ScaffoldStep;

    IScaffolder Build(IServiceProvider serviceProvider);
}
