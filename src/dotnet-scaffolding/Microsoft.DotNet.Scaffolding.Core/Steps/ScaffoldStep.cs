// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;

namespace Microsoft.DotNet.Scaffolding.Core.Steps;

public abstract class ScaffoldStep
{
    public abstract Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default);
    public bool ContinueOnError { get; set; }
    public bool SkipStep { get; set; }
}
