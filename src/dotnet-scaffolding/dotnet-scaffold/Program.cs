// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.CommandLine;
using Microsoft.DotNet.Tools.Scaffold;
using Microsoft.DotNet.Tools.Scaffold.Command;
using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.Hosting;

IScaffoldRunnerBuilder builder = Host.CreateScaffoldBuilder();

ScaffolderOption<bool> nonInteractiveScaffoldOption = GetNonInteractiveOption();
builder.AddOption(nonInteractiveScaffoldOption);
Option nonInteractiveOption = nonInteractiveScaffoldOption.ToCliOption();

var runner = builder.Build();

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
        CliOption = CliOptions.NonInteractiveCliOption,
        Required = false
    };
}
