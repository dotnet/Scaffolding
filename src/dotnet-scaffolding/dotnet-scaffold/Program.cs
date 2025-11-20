// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.CommandLine;
using Microsoft.DotNet.Scaffolding.CodeModification;
using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.Hosting;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Scaffolding.TextTemplating;
using Microsoft.DotNet.Tools.Scaffold.Aspire;
using Microsoft.DotNet.Tools.Scaffold.AspNet;
using Microsoft.DotNet.Tools.Scaffold.Command;
using Microsoft.DotNet.Tools.Scaffold.Interactive.AppBuilder;
using Microsoft.Extensions.DependencyInjection;

IScaffoldRunnerBuilder builder = Host.CreateScaffoldBuilder();

ConfigureServices(builder.Services);
ConfigureSharedSteps(builder.Services);

ScaffolderOption<bool> nonInteractiveScaffoldOption = GetNonInteractiveOption();
builder.AddOption(nonInteractiveScaffoldOption);
Option nonInteractiveOption = nonInteractiveScaffoldOption.ToCliOption();

AspireCommandService aspireCommandService = new(builder);
aspireCommandService.AddScaffolderCommands();
ConfigureCommandSteps(builder.Services, aspireCommandService);

AspNetCommandService aspNetCommandService = new(builder);
aspNetCommandService.AddScaffolderCommands();
ConfigureCommandSteps(builder.Services, aspNetCommandService);

IScaffoldRunner runner = builder.Build();

// add handler for routing the interactive tool through the Spectre.Console experience, all others
// are routed through System.CommandLine experience
builder.AddHandler(async (context) =>
{
    var nonInteractive = context.ParseResult.GetValue(nonInteractiveOption);
    if (nonInteractive is true)
    {
        context.Console.WriteLine("Non-Interactive mode is not yet implemented. Use \"dotnet scaffold\" for the interactive experience.");
        context.ExitCode = 1;
    }
    else
    {
        ScaffoldCommandAppBuilder appBuilder = new(runner, [.. context.ParseResult.Tokens.Select(t => t.Value)]);
        ScaffoldCommandApp app = appBuilder.Build(aspNetCommandService.AzCliErrors);
        await app.RunAsync();
    }
});

var telemetryWrapper = builder.ServiceProvider?.GetRequiredService<IFirstPartyToolTelemetryWrapper>();
telemetryWrapper?.ConfigureFirstTimeTelemetry();
await runner.RunAsync(args);
telemetryWrapper?.Flush();

static void ConfigureServices(IServiceCollection services)
{
    services.AddSingleton<IFileSystem, FileSystem>();
    services.AddSingleton<IEnvironmentService, EnvironmentService>();
    //TODO figure out the telemetry story here
    services.AddTelemetry("dotnetScaffoldAspire");
    services.AddTelemetry("dotnetScaffoldAspnet");
    services.AddSingleton<IFirstPartyToolTelemetryWrapper, FirstPartyToolTelemetryWrapper>();
}

static void ConfigureSharedSteps(IServiceCollection services)
{
    services.AddTransient<CodeModificationStep>();
    services.AddTransient<AddPackagesStep>();
    services.AddTransient<TextTemplatingStep>();
}

static void ConfigureCommandSteps(IServiceCollection services, ICommandService commandService)
{
    Type[] stepTypes = commandService.GetScaffoldSteps();
    if (stepTypes is not null)
    {
        foreach (Type stepType in stepTypes)
        {
            services.AddTransient(stepType);
        }
    }
}

static ScaffolderOption<bool> GetNonInteractiveOption()
{
    return new ScaffolderOption<bool>
    {
        DisplayName = "Run scaffolder in non-Interactive mode",
        CliOption = CliStrings.NonInteractiveCliOption,
        Required = false
    };
}
