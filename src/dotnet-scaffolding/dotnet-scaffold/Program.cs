// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.CommandLine;
using Microsoft.Build.Locator;
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
using Spectre.Console;

// Register MSBuildLocator as early as possible — BEFORE any code below
// is JIT-compiled. The Syncfusion Blazor Toolkit scaffolder and the
// shared code-modification step both go through `CodeService` and
// `Microsoft.CodeAnalysis.MSBuild.MSBuildWorkspace`, whose JIT
// transitively pulls `Microsoft.Build` into the process. Once
// `Microsoft.Build` is loaded, `MSBuildLocator.RegisterMSBuildPath`
// throws "MSBuild assemblies were already loaded". Doing the
// registration here, as the very first executable statement, makes
// sure the MSBuild resolver is in place before any other code can
// cause `Microsoft.Build` to be loaded.
//
// `MSBuildLocator.RegisterDefaults()` enumerates the system's
// installed MSBuild instances (Visual Studio, Build Tools, the .NET
// SDK, etc.) and wires the appropriate one. `IsRegistered` is a
// guard so we don't double-register.
if (!MSBuildLocator.IsRegistered)
{
    MSBuildLocator.RegisterDefaults();
}

// Handle --full-help option before building the runner
// This allows users to invoke: dotnet scaffold --full-help
if (args.Contains("--full-help", StringComparer.OrdinalIgnoreCase))
{
    // Convert --full-help to the full-help subcommand
    args = ["full-help"];
}

IScaffoldRunnerBuilder builder = Host.CreateScaffoldBuilder();

ConfigureServices(builder.Services);
ConfigureSharedSteps(builder.Services);

AspireCommandService aspireCommandService = new(builder);
aspireCommandService.AddScaffolderCommands();
ConfigureCommandSteps(builder.Services, aspireCommandService);

AspNetCommandService aspNetCommandService = new(builder);
aspNetCommandService.AddScaffolderCommands();
ConfigureCommandSteps(builder.Services, aspNetCommandService);

IScaffoldRunner runner = builder.Build();

// add handler for routing the interactive tool through the Spectre.Console experience, all others
// are routed through System.CommandLine experience
builder.AddHandler(async (parseResult, cancellationToken) =>
{
    ScaffoldCommandAppBuilder appBuilder = new(runner, [.. parseResult.Tokens.Select(t => t.Value)]);
    ScaffoldCommandApp app = appBuilder.Build();
    return await app.RunAsync();
});

var telemetryWrapper = builder.ServiceProvider?.GetRequiredService<IFirstPartyToolTelemetryWrapper>();
telemetryWrapper?.ConfigureFirstTimeTelemetry();
int exitCode = await runner.RunAsync(args);
telemetryWrapper?.Flush();
return exitCode;

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
