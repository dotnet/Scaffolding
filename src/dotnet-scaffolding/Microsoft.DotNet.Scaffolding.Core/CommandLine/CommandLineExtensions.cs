// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.ComponentModel;

namespace Microsoft.DotNet.Scaffolding.Core.CommandLine;

internal static class CommandLineExtensions
{
    public static void BuildRootCommand(this IScaffoldRunner scaffoldRunner)
    {
        if (scaffoldRunner.Scaffolders is null)
        {
            throw new InvalidOperationException("Scaffolders is empty.");
        }

        List<CommandInfo> commandInfo = [];
        var rootCommand = new RootCommand();
        foreach (var scaffolder in scaffoldRunner.Scaffolders)
        {
            rootCommand.AddCommand(scaffolder.ToCommand());
            commandInfo.Add(scaffolder.ToCommandInfo());
        }

        rootCommand.AddGetCommandsCommand(commandInfo);

        ((ScaffoldRunner)scaffoldRunner).RootCommand = rootCommand;
    }

    private static Command ToCommand(this IScaffolder scaffolder)
    {
        var command = new Command(scaffolder.Name);

        foreach (var option in scaffolder.Options)
        {
            command.AddOption(option.ToCliOption());
        }

        command.Handler = CommandHandler.Create(async (ParseResult parseResult) =>
        {
            var context = scaffolder.CreateContext(parseResult);

            await scaffolder.ExecuteAsync(context);
        });

        return command;
    }

    private static ScaffolderContext CreateContext(this IScaffolder scaffolder, ParseResult parseResult)
    {
        var context = new ScaffolderContext(scaffolder);

        foreach (var option in context.Scaffolder.Options)
        {
            context.OptionResults[option] = parseResult.GetValue(option.ToCliOption());
        }

        return context;
    }

    private static CommandInfo ToCommandInfo(this IScaffolder scaffolder)
    {
        var commandInfo = new CommandInfo
        {
            Name = scaffolder.Name,
            DisplayName = scaffolder.DisplayName,
            DisplayCategory = scaffolder.Category,
            Description = scaffolder.Description,
            Parameters = scaffolder.Options.Select(o => o.ToParameter()).ToArray()
        };

        return commandInfo;
    }

    private static void AddGetCommandsCommand(this RootCommand rootCommand, List<CommandInfo> commandInfo)
    {
        var getCommandsCommand = new Command("get-commands");

        getCommandsCommand.SetHandler(() =>
        {
            var json = System.Text.Json.JsonSerializer.Serialize(commandInfo);

            // This probably shouldn't be a direct Console.WriteLine call in the long run.
            Console.WriteLine(json);
        });

        rootCommand.AddCommand(getCommandsCommand);
    }
}
