// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Helpers.Services;
using Microsoft.DotNet.Scaffolding.Helpers.Services.Environment;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Commands;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;

internal class AreaScaffolderStep : AreaScaffolderStepBase<AreaCommand>
{
    public AreaScaffolderStep(IFileSystem fileSystem, ILogger logger, IEnvironmentService environmentService)
        : base(new AreaCommand(fileSystem, logger, environmentService))
    {

    }
}
