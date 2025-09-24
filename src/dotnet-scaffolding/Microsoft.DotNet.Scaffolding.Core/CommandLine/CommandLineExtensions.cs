// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.ComponentModel;

namespace Microsoft.DotNet.Scaffolding.Core.CommandLine;

/// <summary>
/// Provides extension methods for building and handling command-line commands for scaffolders.
/// </summary>
internal static class CommandLineExtensions
{
    /// <summary>
    /// Builds the root command and adds all scaffolder commands to it.
    /// </summary>
    /// <param name="scaffoldRunner">The scaffold runner containing scaffolders.</param>
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

        if (scaffoldRunner.Options is not null)
        {
            foreach (ScaffolderOption option in scaffoldRunner.Options)
            {
                rootCommand.AddOption(option.ToCliOption());
            }
        }

        rootCommand.AddGetCommandsCommand(commandInfo);

        ((ScaffoldRunner)scaffoldRunner).RootCommand = rootCommand;
    }

    /// <summary>
    /// Converts a scaffolder to a System.CommandLine command.
    /// </summary>
    /// <param name="scaffolder">The scaffolder to convert.</param>
    /// <returns>The created command.</returns>
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

    /// <summary>
    /// Creates a scaffolder context from the parse result.
    /// </summary>
    /// <param name="scaffolder">The scaffolder.</param>
    /// <param name="parseResult">The parse result.</param>
    /// <returns>The created context.</returns>
    private static ScaffolderContext CreateContext(this IScaffolder scaffolder, ParseResult parseResult)
    {
        var context = new ScaffolderContext(scaffolder);

        foreach (var option in context.Scaffolder.Options)
        {
            context.OptionResults[option] = parseResult.GetValue(option.ToCliOption());
        }

        return context;
    }

    /// <summary>
    /// Converts a scaffolder to a CommandInfo object for metadata.
    /// </summary>
    /// <param name="scaffolder">The scaffolder.</param>
    /// <returns>The command info.</returns>
    private static CommandInfo ToCommandInfo(this IScaffolder scaffolder)
    {
        var commandInfo = new CommandInfo
        {
            Name = scaffolder.Name,
            DisplayName = scaffolder.DisplayName,
            DisplayCategories = scaffolder.Categories.ToList(),
            Description = scaffolder.Description,
            Parameters = scaffolder.Options.Select(o => o.ToParameter()).ToArray()
        };

        return commandInfo;
    }

    /// <summary>
    /// Adds a 'get-commands' command to the root command for listing all available commands as JSON.
    /// </summary>
    /// <param name="rootCommand">The root command.</param>
    /// <param name="commandInfo">The list of command info objects.</param>
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
