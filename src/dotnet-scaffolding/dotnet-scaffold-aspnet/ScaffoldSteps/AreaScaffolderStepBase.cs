// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Commands.Settings;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;

internal abstract class AreaScaffolderStepBase<T>(T command) : ScaffoldStep where T : ICommandWithSettings<AreaCommandSettings>
{
    protected readonly T _command = command;

    public string? Project { get; set; }
    public string? Name { get; set; }

    public override async Task ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
    {
        if (Name is null || Project is null)
        {
            // TODO: Change this process to add validation once when pulling out the real steps
            throw new InvalidOperationException("Name/Project/CommandName must be set before executing the step.");
        }

        await _command.ExecuteAsync(new AreaCommandSettings()
        {
            Project = Project,
            Name = Name
        }, context);
    }
}
