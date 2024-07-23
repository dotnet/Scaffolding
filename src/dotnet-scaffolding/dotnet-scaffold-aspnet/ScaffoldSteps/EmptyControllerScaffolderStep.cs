// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Helpers.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Commands;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;

internal class EmptyControllerScaffolderStep : EmptyControllerScaffolderStepBase<EmptyControllerCommand>
{
    public EmptyControllerScaffolderStep(IFileSystem fileSystem, ILogger<EmptyControllerScaffolderStep> logger)
        : base(new EmptyControllerCommand(fileSystem, logger))
    {

    }
}
