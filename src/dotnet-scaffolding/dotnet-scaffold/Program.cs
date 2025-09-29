// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.CommandLine;
using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.Hosting;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.Command;
using Microsoft.DotNet.Tools.Scaffold.Interactive.AppBuilder;
using Microsoft.DotNet.Tools.Scaffold.Services;
using Microsoft.Extensions.DependencyInjection;

IScaffoldRunnerBuilder builder = Host.CreateScaffoldBuilder();

// add non-interactive option
ScaffolderOption<bool> nonInteractiveScaffoldOption = GetNonInteractiveOption();
builder.AddOption(nonInteractiveScaffoldOption);
Option nonInteractiveOption = nonInteractiveScaffoldOption.ToCliOption();

// resolve IToolManager from DI
builder.Services.AddSingleton<IFileSystem, FileSystem>();
builder.Services.AddSingleton<IEnvironmentService, EnvironmentService>();
builder.Services.AddSingleton<IDotNetToolService, DotNetToolService>();
builder.Services.AddSingleton<IToolManager, ToolManager>();
builder.Services.AddSingleton<IToolManifestService, ToolManifestService>();
var serviceProvider = ((ServiceCollection)builder.Services).BuildServiceProvider();
var toolManager = serviceProvider.GetRequiredService<IToolManager>();

// add tool command with the list subcommands
builder.AddNonScaffoldCommand(ToolCommand.GetCommand(toolManager));

var runner = builder.Build();

// add handler for routing the interactive tool through the Spectre.Console experience, all others
// are routed through System.CommandLine experience
builder.AddHandler(async (context) =>
{
    var nonInteractive = context.ParseResult.GetValue(nonInteractiveOption);
    if (nonInteractive is true)
    {
        //context.Console.WriteLine("Non-Interactive mode is not yet implemented. Use \"dotnet scaffold\" for the interactive experience.");
        context.ExitCode = 0;
    }
    else
    {
        var builder = new ScaffoldCommandAppBuilder(context.ParseResult.Tokens.Select(t => t.Value).ToArray());
        var app = builder.Build();
        await app.RunAsync();
    }
});

await runner.RunAsync(args);

static ScaffolderOption<bool> GetNonInteractiveOption()
{
    return new ScaffolderOption<bool>
    {
        DisplayName = "Run scaffolder in non-Interactive mode",
        CliOption = CliStrings.NonInteractiveCliOption,
        Required = false
    };
}
