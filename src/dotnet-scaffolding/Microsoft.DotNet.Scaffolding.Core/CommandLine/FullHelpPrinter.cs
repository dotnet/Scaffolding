// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;

namespace Microsoft.DotNet.Scaffolding.Core.CommandLine;

/// <summary>
/// Provides functionality to print comprehensive help for all scaffold commands and subcommands.
/// </summary>
internal static class FullHelpPrinter
{
    /// <summary>
    /// Prints full help output for the scaffold CLI, including all categories and their subcommands.
    /// </summary>
    /// <param name="scaffolders">Dictionary of scaffolder categories and their scaffolders.</param>
    public static void PrintFullHelp(IReadOnlyDictionary<ScaffolderCatagory, IEnumerable<IScaffolder>>? scaffolders)
    {
        var helpText = GenerateFullHelpText(scaffolders);
        Console.Write(helpText);
    }

    /// <summary>
    /// Generates the full help text for the scaffold CLI, including all categories and their subcommands.
    /// This method can be used for testing without writing to console.
    /// </summary>
    /// <param name="scaffolders">Dictionary of scaffolder categories and their scaffolders.</param>
    /// <returns>The complete help text as a string.</returns>
    internal static string GenerateFullHelpText(IReadOnlyDictionary<ScaffolderCatagory, IEnumerable<IScaffolder>>? scaffolders)
    {
        var sb = new StringBuilder();

        // Root command description
        sb.AppendLine(FullHelpStrings.DescriptionHeader);
        sb.AppendLine($"  {FullHelpStrings.RootCommandDescription}");
        sb.AppendLine();

        sb.AppendLine(FullHelpStrings.UsageHeader);
        sb.AppendLine($"  {FullHelpStrings.RootCommandUsage}");
        sb.AppendLine();

        sb.AppendLine(FullHelpStrings.OptionsHeader);
        sb.AppendLine($"  --{FullHelpStrings.FullHelpCommandName}     {FullHelpStrings.FullHelpOptionDescription}");
        sb.AppendLine($"  -h, --help      {FullHelpStrings.HelpOptionDescription}");
        sb.AppendLine();

        sb.AppendLine(FullHelpStrings.CommandsHeader);

        // Print category summaries
        if (scaffolders is not null)
        {
            foreach (var category in scaffolders.Keys.OrderBy(c => c.ToString()))
            {
                var categoryName = GetCategoryName(category);
                var categoryDesc = GetCategoryDescription(category);
                sb.AppendLine($"  {categoryName,-16} {categoryDesc}");
            }
        }
        sb.AppendLine($"  {FullHelpStrings.ToolCategoryName,-16} {FullHelpStrings.ToolCategoryDescription}");
        sb.AppendLine();

        // Print detailed help for each category
        if (scaffolders is not null)
        {
            foreach (var kvp in scaffolders.OrderBy(k => k.Key.ToString()))
            {
                PrintCategoryHelp(sb, kvp.Key, kvp.Value);
            }
        }

        // Print tool commands help
        PrintToolCommandsHelp(sb);

        return sb.ToString();
    }

    private static void PrintCategoryHelp(StringBuilder sb, ScaffolderCatagory category, IEnumerable<IScaffolder> scaffolders)
    {
        var categoryName = GetCategoryName(category);
        var categoryDesc = GetCategoryDescription(category);

        sb.AppendLine(new string('=', 60));
        sb.AppendLine($"dotnet scaffold {categoryName}");
        sb.AppendLine(new string('=', 60));
        sb.AppendLine();
        sb.AppendLine(FullHelpStrings.DescriptionHeader);
        sb.AppendLine($"  {categoryDesc}");
        sb.AppendLine();
        sb.AppendLine(FullHelpStrings.UsageHeader);
        sb.AppendLine($"  dotnet scaffold {categoryName} [command] [options]");
        sb.AppendLine();
        sb.AppendLine(FullHelpStrings.CommandsHeader);

        foreach (var scaffolder in scaffolders.OrderBy(s => s.Name))
        {
            var desc = scaffolder.Description ?? scaffolder.DisplayName;
            sb.AppendLine($"  {scaffolder.Name,-30} {desc}");
        }
        sb.AppendLine();

        // Print detailed help for each scaffolder
        foreach (var scaffolder in scaffolders.OrderBy(s => s.Name))
        {
            PrintScaffolderHelp(sb, categoryName, scaffolder);
        }
    }

    private static void PrintScaffolderHelp(StringBuilder sb, string categoryName, IScaffolder scaffolder)
    {
        sb.AppendLine(new string('-', 60));
        sb.AppendLine($"dotnet scaffold {categoryName} {scaffolder.Name}");
        sb.AppendLine(new string('-', 60));
        sb.AppendLine();

        if (!string.IsNullOrEmpty(scaffolder.Description))
        {
            sb.AppendLine(FullHelpStrings.DescriptionHeader);
            sb.AppendLine($"  {scaffolder.Description}");
            sb.AppendLine();
        }

        sb.AppendLine(FullHelpStrings.UsageHeader);
        sb.AppendLine($"  dotnet scaffold {categoryName} {scaffolder.Name} [options]");
        sb.AppendLine();

        var options = scaffolder.Options.ToList();
        if (options.Count > 0)
        {
            sb.AppendLine(FullHelpStrings.OptionsHeader);
            foreach (var option in options)
            {
                var optionName = option.CliOption ?? $"--{ToKebabCase(option.DisplayName)}";
                var required = option.Required ? $" {FullHelpStrings.RequiredMarker}" : "";
                var desc = option.Description ?? option.DisplayName;
                sb.AppendLine($"  {optionName,-30} {desc}{required}");
            }
            sb.AppendLine();
        }

        var examples = scaffolder.Examples.ToList();
        if (examples.Count > 0)
        {
            sb.AppendLine("Examples:");
            foreach (var (example, description) in examples)
            {
                if (!string.IsNullOrEmpty(description))
                {
                    sb.AppendLine($"  {description}");
                }
                sb.AppendLine($"    {example}");
                sb.AppendLine();
            }
        }
    }

    private static void PrintToolCommandsHelp(StringBuilder sb)
    {
        sb.AppendLine(new string('=', 60));
        sb.AppendLine($"dotnet scaffold {FullHelpStrings.ToolCategoryName}");
        sb.AppendLine(new string('=', 60));
        sb.AppendLine();
        sb.AppendLine(FullHelpStrings.DescriptionHeader);
        sb.AppendLine($"  {FullHelpStrings.ToolCategoryDescription}");
        sb.AppendLine();
        sb.AppendLine(FullHelpStrings.UsageHeader);
        sb.AppendLine($"  dotnet scaffold {FullHelpStrings.ToolCategoryName} [command] [options]");
        sb.AppendLine();
        sb.AppendLine(FullHelpStrings.CommandsHeader);
        sb.AppendLine($"  {FullHelpStrings.ToolInstallCommandName,-16} {FullHelpStrings.ToolInstallCommandDescription}");
        sb.AppendLine($"  {FullHelpStrings.ToolListCommandName,-16} {FullHelpStrings.ToolListCommandDescription}");
        sb.AppendLine($"  {FullHelpStrings.ToolUninstallCommandName,-16} {FullHelpStrings.ToolUninstallCommandDescription}");
        sb.AppendLine();

        // tool install
        sb.AppendLine(new string('-', 60));
        sb.AppendLine($"dotnet scaffold {FullHelpStrings.ToolCategoryName} {FullHelpStrings.ToolInstallCommandName}");
        sb.AppendLine(new string('-', 60));
        sb.AppendLine();
        sb.AppendLine(FullHelpStrings.DescriptionHeader);
        sb.AppendLine($"  {FullHelpStrings.ToolInstallCommandDescription}");
        sb.AppendLine();
        sb.AppendLine(FullHelpStrings.UsageHeader);
        sb.AppendLine($"  dotnet scaffold {FullHelpStrings.ToolCategoryName} {FullHelpStrings.ToolInstallCommandName} {FullHelpStrings.ToolInstallPackageNameArgument} [options]");
        sb.AppendLine();
        sb.AppendLine(FullHelpStrings.ArgumentsHeader);
        sb.AppendLine($"  {FullHelpStrings.ToolInstallPackageNameArgument,-25} {FullHelpStrings.ToolInstallPackageNameDescription}");
        sb.AppendLine();
        sb.AppendLine(FullHelpStrings.OptionsHeader);
        sb.AppendLine($"  {FullHelpStrings.ToolAddSourceOption,-25} {FullHelpStrings.ToolAddSourceDescription}");
        sb.AppendLine($"  {FullHelpStrings.ToolConfigFileOption,-25} {FullHelpStrings.ToolConfigFileDescription}");
        sb.AppendLine($"  {FullHelpStrings.ToolPrereleaseOption,-25} {FullHelpStrings.ToolPrereleaseDescription}");
        sb.AppendLine($"  {FullHelpStrings.ToolGlobalOption,-25} {FullHelpStrings.ToolGlobalDescription}");
        sb.AppendLine($"  {FullHelpStrings.ToolVersionOption,-25} {FullHelpStrings.ToolVersionDescription}");
        sb.AppendLine();

        // tool list
        sb.AppendLine(new string('-', 60));
        sb.AppendLine($"dotnet scaffold {FullHelpStrings.ToolCategoryName} {FullHelpStrings.ToolListCommandName}");
        sb.AppendLine(new string('-', 60));
        sb.AppendLine();
        sb.AppendLine(FullHelpStrings.DescriptionHeader);
        sb.AppendLine($"  {FullHelpStrings.ToolListCommandDescription}");
        sb.AppendLine();
        sb.AppendLine(FullHelpStrings.UsageHeader);
        sb.AppendLine($"  dotnet scaffold {FullHelpStrings.ToolCategoryName} {FullHelpStrings.ToolListCommandName}");
        sb.AppendLine();

        // tool uninstall
        sb.AppendLine(new string('-', 60));
        sb.AppendLine($"dotnet scaffold {FullHelpStrings.ToolCategoryName} {FullHelpStrings.ToolUninstallCommandName}");
        sb.AppendLine(new string('-', 60));
        sb.AppendLine();
        sb.AppendLine(FullHelpStrings.DescriptionHeader);
        sb.AppendLine($"  {FullHelpStrings.ToolUninstallCommandDescription}");
        sb.AppendLine();
        sb.AppendLine(FullHelpStrings.UsageHeader);
        sb.AppendLine($"  dotnet scaffold {FullHelpStrings.ToolCategoryName} {FullHelpStrings.ToolUninstallCommandName} {FullHelpStrings.ToolInstallPackageNameArgument} [options]");
        sb.AppendLine();
        sb.AppendLine(FullHelpStrings.ArgumentsHeader);
        sb.AppendLine($"  {FullHelpStrings.ToolInstallPackageNameArgument,-25} {FullHelpStrings.ToolUninstallPackageNameDescription}");
        sb.AppendLine();
        sb.AppendLine(FullHelpStrings.OptionsHeader);
        sb.AppendLine($"  {FullHelpStrings.ToolGlobalOption,-25} {FullHelpStrings.ToolUninstallGlobalDescription}");
        sb.AppendLine();
    }

    private static string GetCategoryName(ScaffolderCatagory category)
    {
        return category switch
        {
            ScaffolderCatagory.Aspire => FullHelpStrings.AspireCategoryName,
            ScaffolderCatagory.AspNet => FullHelpStrings.AspNetCategoryName,
            _ => category.ToString().ToLowerInvariant()
        };
    }

    private static string GetCategoryDescription(ScaffolderCatagory category)
    {
        return category switch
        {
            ScaffolderCatagory.Aspire => FullHelpStrings.AspireCategoryDescription,
            ScaffolderCatagory.AspNet => FullHelpStrings.AspNetCategoryDescription,
            _ => $"Commands related to {category} project scaffolding"
        };
    }

    private static string ToKebabCase(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        var sb = new StringBuilder();
        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];
            if (char.IsUpper(c))
            {
                if (i > 0)
                {
                    sb.Append('-');
                }
                sb.Append(char.ToLowerInvariant(c));
            }
            else if (c == ' ')
            {
                sb.Append('-');
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }
}
