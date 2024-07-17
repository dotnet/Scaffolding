// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Helpers.Services;
using Microsoft.DotNet.Scaffolding.Helpers.Services.Environment;
using Microsoft.DotNet.Tools.Scaffold.Aspire.Commands;

namespace Microsoft.DotNet.Tools.Scaffold.Aspire.ScaffoldSteps;

internal class PlaceholderCachingStep : PlaceholderStepBase<CachingCommand>
{
    public PlaceholderCachingStep(IFileSystem fileSystem, ILogger logger, IEnvironmentService environmentService)
        : base(new CachingCommand(fileSystem, logger, environmentService))
    {
        
    }
}
