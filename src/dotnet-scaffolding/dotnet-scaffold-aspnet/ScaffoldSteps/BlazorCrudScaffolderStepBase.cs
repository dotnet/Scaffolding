// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Commands.Settings;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;

internal abstract class BlazorCrudScaffolderStepBase<T>(T command) : ScaffoldStep where T : ICommandWithSettings<BlazorCrudSettings>
{
    protected readonly T _command = command;

    public string? Project { get; set; }
    public bool Prerelease { get; set; }
    public string? DatabaseProvider { get; set; }
    public string? DataContext { get; set; }
    public string? Model { get; set; } 
    public string? Page { get; set; }
    public override async Task ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
    {
        if (Project is null || Model is null || Page is null)
        {
            // TODO: Change this process to add validation once when pulling out the real steps
            throw new InvalidOperationException("Name/Model/Page must be set before executing the step.");
        }

        await _command.ExecuteAsync(new BlazorCrudSettings()
        {
            Project = Project,
            Model = Model,
            DatabaseProvider = DatabaseProvider,
            DataContext = DataContext,
            Prerelease = Prerelease,
            Page = Page
        }, context);
    }
}
