// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;

namespace Microsoft.DotNet.Scaffolding.Steps;

internal abstract class ScaffoldStep
{
    public abstract Task<bool> ExecuteAsync();
    public bool ContinueOnError { get; set; }
}

internal abstract class OutputScaffoldStep : ScaffoldStep
{
    public string? Output { get; set; }
}
