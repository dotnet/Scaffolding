// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Microsoft.DotNet.Scaffolding.Helpers.Steps;

internal abstract class ScaffoldStep
{
    public abstract Task<bool> ExecuteAsync();
}

internal abstract class ScaffoldStep<TScaffoldingInfo>(TScaffoldingInfo stepInfo) : ScaffoldStep
{
    public TScaffoldingInfo StepInfo { get; } = stepInfo;
}
