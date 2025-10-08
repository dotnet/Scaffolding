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
using Microsoft.DotNet.Tools.Scaffold.Aspire.ScaffoldSteps;
using Microsoft.DotNet.Tools.Scaffold.AspNet;
using Microsoft.DotNet.Tools.Scaffold.Command;
using Microsoft.DotNet.Tools.Scaffold.Interactive.AppBuilder;
using Microsoft.Extensions.DependencyInjection;

IScaffoldRunnerBuilder builder = Host.CreateScaffoldBuilder();

ConfigureServices(builder.Services);
ConfigureSteps(builder.Services);

ScaffolderOption<bool> nonInteractiveScaffoldOption = GetNonInteractiveOption();
builder.AddOption(nonInteractiveScaffoldOption);
Option nonInteractiveOption = nonInteractiveScaffoldOption.ToCliOption();

AspireCommandService aspireCommandService = new(builder);
aspireCommandService.AddScaffolderCommands();

AspNetCommandService aspNetCommandService = new(builder);
aspNetCommandService.AddScaffolderCommands();

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
        ScaffoldCommandAppBuilder builder = new(runner, [.. context.ParseResult.Tokens.Select(t => t.Value)]);
        ScaffoldCommandApp app = builder.Build();
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

static void ConfigureSteps(IServiceCollection services)
{
    services.AddTransient<CodeModificationStep>();
    services.AddTransient<AddAspireCodeChangeStep>();
    services.AddTransient<ValidateOptionsStep>();
    services.AddTransient<AddPackagesStep>();
    services.AddTransient<WrappedAddPackagesStep>();
    services.AddTransient<TextTemplatingStep>();
    services.AddTransient<AddAspireConnectionStringStep>();
    services.AddTransient<AddAspNetConnectionStringStep>();

    var executingAssembly = System.Reflection.Assembly.GetExecutingAssembly();
    var scaffoldStepTypes = executingAssembly.GetTypes().Where(x => x.IsSubclassOf(typeof(ScaffoldStep)));
    foreach (var type in scaffoldStepTypes)
    {
        services.AddTransient(type);
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
