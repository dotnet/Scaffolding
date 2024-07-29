// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Commands.API.MinimalApi;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;

internal class MinimalApiScaffolderStep : MinimalApiScaffolderStepBase<MinimalApiCommand>
{
    public MinimalApiScaffolderStep(IFileSystem fileSystem, ILogger<MinimalApiScaffolderStep> logger)
        : base(new MinimalApiCommand(fileSystem, logger))
    {

    }
}
