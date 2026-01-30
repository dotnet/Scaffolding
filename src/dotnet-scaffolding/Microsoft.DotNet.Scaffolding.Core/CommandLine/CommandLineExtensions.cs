// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.CommandLine;
using System.CommandLine.Parsing;
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
        foreach (KeyValuePair<ScaffolderCatagory,IEnumerable<IScaffolder>> categoryWithScaffolder in scaffoldRunner.Scaffolders)
        {
            Command categoryCommand = GetCategoryCommand(categoryWithScaffolder.Key);
            foreach (var scaffolder in categoryWithScaffolder.Value)
            {
                categoryCommand.Subcommands.Add(scaffolder.ToCommand());
                commandInfo.Add(scaffolder.ToCommandInfo());
            }
            rootCommand.Subcommands.Add(categoryCommand);
        }

        if (scaffoldRunner.Options is not null)
        {
            foreach (ScaffolderOption option in scaffoldRunner.Options)
            {
                rootCommand.Options.Add(option.ToCliOption());
            }
        }

        // Add --full-help option
        rootCommand.AddFullHelpOption(scaffoldRunner.Scaffolders);

        rootCommand.AddGetCommandsCommand(commandInfo);

        ((ScaffoldRunner)scaffoldRunner).RootCommand = rootCommand;

        static Command GetCategoryCommand(ScaffolderCatagory catagory)
        {
            if (catagory == ScaffolderCatagory.Aspire)
            {
                return new Command(FullHelpStrings.AspireCategoryName, FullHelpStrings.AspireCategoryDescription);
            }
            else
            {
                return new Command(FullHelpStrings.AspNetCategoryName, FullHelpStrings.AspNetCategoryDescription);
            }
        }
    }

    /// <summary>
    /// Converts a scaffolder to a System.CommandLine command.
    /// </summary>
    /// <param name="scaffolder">The scaffolder to convert.</param>
    /// <returns>The created command.</returns>
    private static Command ToCommand(this IScaffolder scaffolder)
    {
        Command command = new(scaffolder.Name, scaffolder.Description);

        foreach (var option in scaffolder.Options)
        {
            command.Options.Add(option.ToCliOption());
        }

        // Add examples to the command description if any exist
        if (scaffolder.Examples.Any())
        {
            var examplesText = new System.Text.StringBuilder();
            examplesText.AppendLine(scaffolder.Description ?? string.Empty);
            examplesText.AppendLine();
            examplesText.AppendLine("Examples:");
            foreach (var (example, description) in scaffolder.Examples)
            {
                if (!string.IsNullOrEmpty(description))
                {
                    examplesText.AppendLine($"  {description}");
                }
                examplesText.AppendLine($"    {example}");
                examplesText.AppendLine();
            }
            command.Description = examplesText.ToString();
        }

        command.SetAction(async (ParseResult parseResult, CancellationToken cancellationToken) =>
        {
            var context = scaffolder.CreateContext(parseResult);
            await scaffolder.ExecuteAsync(context);
            return 0;
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
            context.OptionResults[option] = option.GetValue(parseResult);
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
    /// Adds a '--full-help' option to the root command for displaying comprehensive help for all commands.
    /// </summary>
    /// <param name="rootCommand">The root command.</param>
    /// <param name="scaffolders">The scaffolders dictionary.</param>
    private static void AddFullHelpOption(this RootCommand rootCommand, IReadOnlyDictionary<ScaffolderCatagory, IEnumerable<IScaffolder>> scaffolders)
    {
        Command fullHelpCommand = new(FullHelpStrings.FullHelpCommandName, FullHelpStrings.FullHelpCommandDescription);
        fullHelpCommand.SetAction((ParseResult parseResult, CancellationToken cancellationToken) =>
        {
            FullHelpPrinter.PrintFullHelp(scaffolders);
            return Task.FromResult(0);
        });

        rootCommand.Subcommands.Add(fullHelpCommand);
    }

    /// <summary>
    /// Adds a 'get-commands' command to the root command for listing all available commands as JSON.
    /// </summary>
    /// <param name="rootCommand">The root command.</param>
    /// <param name="commandInfo">The list of command info objects.</param>
    private static void AddGetCommandsCommand(this RootCommand rootCommand, List<CommandInfo> commandInfo)
    {
        Command getCommandsCommand = new("get-commands");
        getCommandsCommand.SetAction((ParseResult parseResult, CancellationToken cancellationToken) =>
        {
            var json = System.Text.Json.JsonSerializer.Serialize(commandInfo);

            // This probably shouldn't be a direct Console.WriteLine call in the long run.
            Console.WriteLine(json);
            return Task.FromResult(0);
        });

        rootCommand.Subcommands.Add(getCommandsCommand);
    }
}
