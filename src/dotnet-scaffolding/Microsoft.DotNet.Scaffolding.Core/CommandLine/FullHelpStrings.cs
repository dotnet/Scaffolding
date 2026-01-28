// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.Scaffolding.Core.CommandLine;

/// <summary>
/// Contains string constants for the full-help command output.
/// </summary>
internal static class FullHelpStrings
{
    // Full Help Command
    internal const string FullHelpCommandName = "full-help";
    internal const string FullHelpCommandDescription = "Display help for all commands and subcommands";

    // Root Command
    internal const string RootCommandDescription = "dotnet scaffold - A CLI tool for scaffolding ASP.NET and Aspire projects.";
    internal const string RootCommandUsage = "dotnet scaffold [command] [options]";

    // Help Options
    internal const string FullHelpOptionDescription = "Display help for all commands and subcommands";
    internal const string HelpOptionDescription = "Show help and usage information";

    // Category Commands
    internal const string AspireCategoryName = "aspire";
    internal const string AspireCategoryDescription = "Commands related to Aspire project scaffolding";
    internal const string AspNetCategoryName = "aspnet";
    internal const string AspNetCategoryDescription = "Commands related to ASP.NET project scaffolding";

    // Tool Commands
    internal const string ToolCategoryName = "tool";
    internal const string ToolCategoryDescription = "Commands for managing scaffold tools";

    internal const string ToolInstallCommandName = "install";
    internal const string ToolInstallCommandDescription = "Install a scaffold tool";
    internal const string ToolInstallPackageNameArgument = "<PACKAGE_NAME>";
    internal const string ToolInstallPackageNameDescription = "Available packages are dotnet-scaffold-aspnet and dotnet-scaffold-aspire.";

    internal const string ToolListCommandName = "list";
    internal const string ToolListCommandDescription = "List installed scaffold tools";

    internal const string ToolUninstallCommandName = "uninstall";
    internal const string ToolUninstallCommandDescription = "Uninstall a scaffold tool";
    internal const string ToolUninstallPackageNameDescription = "Package name to uninstall";

    // Tool Options
    internal const string ToolAddSourceOption = "--add-source <SOURCE>";
    internal const string ToolAddSourceDescription = "Additional sources for tool installation";
    internal const string ToolConfigFileOption = "--configfile <FILE>";
    internal const string ToolConfigFileDescription = "NuGet configuration file";
    internal const string ToolPrereleaseOption = "--prerelease";
    internal const string ToolPrereleaseDescription = "Allow prerelease versions";
    internal const string ToolGlobalOption = "--global";
    internal const string ToolGlobalDescription = "Install tool globally";
    internal const string ToolVersionOption = "--version <VERSION_NUMBER>";
    internal const string ToolVersionDescription = "Specific version to install";
    internal const string ToolUninstallGlobalDescription = "Uninstall globally";

    // Section Headers
    internal const string DescriptionHeader = "Description:";
    internal const string UsageHeader = "Usage:";
    internal const string OptionsHeader = "Options:";
    internal const string CommandsHeader = "Commands:";
    internal const string ArgumentsHeader = "Arguments:";

    // Formatting
    internal const string RequiredMarker = "(Required)";
}
