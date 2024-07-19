// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Helpers.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Commands.Blazor.BlazorCrud;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;

internal class BlazorCrudScaffolderStep : BlazorCrudScaffolderStepBase<BlazorCrudCommand>
{
    public BlazorCrudScaffolderStep(IFileSystem fileSystem, ILogger logger)
        : base(new BlazorCrudCommand(fileSystem, logger))
    {

    }
}
