// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using Microsoft.DotNet.Tools.Scaffold.Services;

namespace Microsoft.DotNet.Tools.Scaffold.Command;

internal static class ToolCommand
{
    private const string _addSourceOption = "--add-source";
    private const string _addSourceOptionDescription = "Additional sources to use when searching for the tool package.";

    private const string _configFileOption = "--configfile";
    private const string _configFileOptionDescription = "NuGet configuration file to use.";

    private const string _prereleaseOption = "--prerelease";
    private const string _prereleaseOptionDescription = "Allow prerelease versions of the tool.";

    private const string _versionOption = "--version";
    private const string _versionOptionDescription = "Version of the tool to install.";

    private const string _globalOption = "--global";
    private const string _globalOptionDescription = "Indicates whether the tool is installed globally.";

    private const string _packageNameArgument = "packageName";
    private const string _packageNameArgumentDescription = "Name of the tool package to install or uninstall.";

    /// <summary>
    /// Gets the main tool command with its subcommands.
    /// </summary>
    /// <param name="toolManager"></param>
    /// <returns></returns>
    public static System.CommandLine.Command GetCommand(IToolManager toolManager)
    {
        var command = new System.CommandLine.Command(CliStrings.ToolCommand, CliStrings.ToolCommandDescription);
        foreach (var subcommand in GetSubCommands(toolManager))
        {
            command.AddCommand(subcommand);
        }
        return command;
    }

    /// <summary>
    /// Retrieves the subcommands for the tool command, including tool list, tool install, and tool uninstall.
    /// </summary>
    private static IEnumerable<System.CommandLine.Command> GetSubCommands(IToolManager toolManager)
    {
        System.CommandLine.Command toolListSubcommand = new(CliStrings.ToolListSubcommand, CliStrings.ToolListSubcommandDescription);
        toolListSubcommand.SetHandler(() => toolManager.ListTools());

        System.CommandLine.Command installSubcommand = GetToolInstallSubCommand();

        System.CommandLine.Command toolUninstallSubcommand = GetToolUninstallSubCommand();

        return [ toolListSubcommand, installSubcommand, toolUninstallSubcommand ];

        System.CommandLine.Command GetToolInstallSubCommand()
        {
            // arguments
            Argument<string> packageNameArg = GetPackageNameArgument();

            // options
            Option<string[]> addSourceOpt = new(_addSourceOption, description: _addSourceOptionDescription)
            {
                AllowMultipleArgumentsPerToken = true
            };
            Option<string?> configFileOpt = new(_configFileOption, description: _configFileOptionDescription);
            Option<bool> prereleaseOpt = new(_prereleaseOption, description: _prereleaseOptionDescription);
            Option<bool> globalOpt = GetGlobalOption();
            Option<string?> versionOpt = new(_versionOption, description: _versionOptionDescription);

            System.CommandLine.Command toolInstallSubcommand = new(CliStrings.ToolInstallSubcommand, CliStrings.ToolInstallSubcommandDescription)
            {
                packageNameArg,
                addSourceOpt,
                configFileOpt,
                prereleaseOpt,
                globalOpt,
                versionOpt
            };

            toolInstallSubcommand.SetHandler((string packageName, string[] addSources, string? configFile, bool prerelease, bool global, string? version) =>
            {
                toolManager.AddTool(packageName, addSources ?? [], configFile, prerelease, version, global);
            },
            packageNameArg, addSourceOpt, configFileOpt, prereleaseOpt, globalOpt, versionOpt);
            return toolInstallSubcommand;
        }

        System.CommandLine.Command GetToolUninstallSubCommand()
        {
            var packageNameArg = GetPackageNameArgument();
            var globalOpt = GetGlobalOption();
            System.CommandLine.Command toolUninstallSubcommand = new(CliStrings.ToolUninstallSubcommand, CliStrings.ToolUninstallSubcommandDescription)
            {
                packageNameArg,
                globalOpt
            };
            toolUninstallSubcommand.SetHandler((string packageName, bool global) =>
            {
                toolManager.RemoveTool(packageName, global);
            },
            packageNameArg, globalOpt);
            return toolUninstallSubcommand;
        }

        Argument<string> GetPackageNameArgument()
        {
            return new Argument<string>(_packageNameArgument)
            {
                Description = _packageNameArgumentDescription,
            };
        }

        Option<bool> GetGlobalOption()
        {
            return new Option<bool>(_globalOption, description: _globalOptionDescription);
        }
    }
}
