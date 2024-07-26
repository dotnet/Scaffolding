// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Commands.Settings;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;

internal abstract class MinimalApiScaffolderStepBase<T>(T command) : ScaffoldStep where T : ICommandWithSettings<MinimalApiSettings>
{
    protected readonly T _command = command;

    public string? Project { get; set; }
    public bool Prerelease { get; set; }
    public string? Endpoints { get; set; }
    public bool OpenApi { get; set; } = true;
    public string? DatabaseProvider { get; set; }
    public string? DataContext { get; set; }
    public string? Model { get; set; }
    public override async Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
    {
        if (Project is null || Model is null)
        {
            // TODO: Change this process to add validation once when pulling out the real steps
            throw new InvalidOperationException("Project/Model must be set before executing the step.");
        }

        var exitCode = await _command.ExecuteAsync(new MinimalApiSettings()
        {
            Project = Project,
            Model = Model,
            DatabaseProvider = DatabaseProvider,
            DataContext = DataContext,
            OpenApi = OpenApi,
            Endpoints = Endpoints,
            Prerelease = Prerelease
        }, context);

        return exitCode == 0;
    }
}
