// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.DotNet.Scaffolding.Core.ComponentModel;
using Microsoft.DotNet.Scaffolding.Internal.CliHelpers;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Tools.Scaffold.Services;

/// <summary>
/// Service for managing .NET tools, including installation, uninstallation, and command discovery.
/// </summary>
internal class DotNetToolService : IDotNetToolService
{
    private readonly ILogger _logger;
    private readonly IEnvironmentService _environmentService;
    private readonly IFileSystem _fileSystem;

    /// <summary>
    /// Initializes a new instance of the <see cref="DotNetToolService"/> class.
    /// </summary>
    /// <param name="logger">Logger instance for logging operations.</param>
    /// <param name="environmentService">Service for environment operations.</param>
    /// <param name="fileSystem">File system abstraction.</param>
    public DotNetToolService(ILogger<DotNetToolService> logger, IEnvironmentService environmentService, IFileSystem fileSystem)
    {
        _logger = logger;
        _environmentService = environmentService;
        _fileSystem = fileSystem;
        _dotNetTools = [];
    }

    // Cached list of discovered .NET tools.
    private IList<DotNetToolInfo> _dotNetTools;

    /// <summary>
    /// Gets the list of commands provided by a specific .NET tool.
    /// </summary>
    /// <param name="dotnetTool">The .NET tool information.</param>
    /// <param name="envVars">Optional environment variables.</param>
    /// <returns>List of <see cref="CommandInfo"/> objects, or an empty list if none found.</returns>
    public async Task<List<CommandInfo>> GetCommandsAsync(DotNetToolInfo dotnetTool, IDictionary<string, string>? envVars = null)
    {
        List<CommandInfo>? commands = null;
        var runner = dotnetTool.IsGlobalTool ?
            DotnetCliRunner.Create(dotnetTool.Command, ["get-commands"], envVars) :
            DotnetCliRunner.CreateDotNet(dotnetTool.Command, ["get-commands"], envVars);

        (int exitCode, string? stdOut, _) = await runner.ExecuteAndCaptureOutputAsync();
        if (exitCode == 0 && !string.IsNullOrEmpty(stdOut))
        {
            try
            {
                string escapedJsonString = stdOut.Replace("\r", "").Replace("\n", "");
                commands = JsonSerializer.Deserialize<List<CommandInfo>>(escapedJsonString);
            }
            catch (Exception)
            {
                // Ignore deserialization errors
            }
        }

        return commands ?? [];
    }

    /// <summary>
    /// Gets a specific .NET tool by component name and optional version.
    /// </summary>
    /// <param name="componentName">The name of the component/tool.</param>
    /// <param name="version">Optional version string.</param>
    /// <returns>The matching <see cref="DotNetToolInfo"/>, or null if not found.</returns>
    public async Task<DotNetToolInfo?> GetDotNetToolAsync(string? componentName, string? version = null)
    {
        if (string.IsNullOrEmpty(componentName))
        {
            return null;
        }
        var dotnetTools = await GetDotNetToolsAsync();
        var matchingTools = dotnetTools.Where(x =>
            x.PackageName.Equals(componentName, StringComparison.OrdinalIgnoreCase) ||
            x.Command.Equals(componentName, StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrEmpty(version))
        {
            return matchingTools.FirstOrDefault();
        }
        else
        {
            return matchingTools.FirstOrDefault(x => x.Version.Equals(version));
        }
    }

    /// <summary>
    /// Gets all commands from all .NET tools in parallel.
    /// </summary>
    /// <param name="components">Optional list of components to query. If null, all tools are queried.</param>
    /// <param name="envVars">Optional environment variables.</param>
    /// <returns>List of key-value pairs of tool command and <see cref="CommandInfo"/>.</returns>
    public async Task<IList<KeyValuePair<string, CommandInfo>>> GetAllCommandsParallelAsync(IList<DotNetToolInfo>? components = null, IDictionary<string, string>? envVars = null)
    {
        if (components is null || components.Count == 0)
        {
            components = await GetDotNetToolsAsync(refresh: true, envVars);
        }

        //if any local tools are present, we need to restore them first
        //when sdks/runtimes are switched/rolled forward, local tools need to be restored before they are called
        var anyLocalTools = components.FirstOrDefault(x => !x.IsGlobalTool) is not null;
        if (anyLocalTools)
        {
            var runner = DotnetCliRunner.CreateDotNet("tool", ["restore"], envVars);
            await runner.ExecuteAndCaptureOutputAsync();
        }

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = System.Environment.ProcessorCount
        };

        var commands = new ConcurrentBag<KeyValuePair<string, CommandInfo>>();
        IEnumerable<Task> commandTasks = components.Select(async dotnetTool =>
        {
            List<CommandInfo> commandInfo = await GetCommandsAsync(dotnetTool, envVars);
            if (commandInfo != null)
            {
                foreach (var cmd in commandInfo)
                {
                    commands.Add(KeyValuePair.Create(dotnetTool.Command, cmd));
                }
            }
        });
        await Task.WhenAll(commandTasks);

        return commands.ToList();
    }

    /// <summary>
    /// Installs a .NET tool using the dotnet CLI.
    /// </summary>
    /// <param name="toolName">The name of the tool to install.</param>
    /// <param name="version">Optional version to install.</param>
    /// <param name="global">Whether to install the tool globally.</param>
    /// <param name="prerelease">Whether to allow prerelease versions.</param>
    /// <param name="addSources">Optional additional NuGet sources.</param>
    /// <param name="configFile">Optional NuGet config file path.</param>
    /// <returns>True if installation succeeded, otherwise false.</returns>
    public async Task<bool> InstallDotNetToolAsync(string toolName, string? version = null, bool global = false, bool prerelease = false, string[]? addSources = null, string? configFile = null)
    {
        if (string.IsNullOrEmpty(toolName))
        {
            return false;
        }

        var installParams = new List<string> { "install", toolName };
        if (global)
        {
            installParams.Add("-g");
        }
        else
        {
            installParams.Add("--create-manifest-if-needed");
        }

        if (!string.IsNullOrEmpty(version))
        {
            installParams.Add("--version");
            installParams.Add(version);
        }

        if (prerelease)
        {
            installParams.Add("--prerelease");
        }

        if (addSources is not null)
        {
            foreach (var source in addSources)
            {
                installParams.Add("--add-source");
                installParams.Add(source);
            }
        }

        if (!string.IsNullOrEmpty(configFile))
        {
            installParams.Add("--configfile");
            installParams.Add(configFile);
        }

        var runner = DotnetCliRunner.CreateDotNet("tool", installParams);
        (int exitCode, _, _) = await runner.ExecuteAndCaptureOutputAsync();
        return exitCode == 0;
    }

    /// <summary>
    /// Uninstalls a .NET tool using the dotnet CLI.
    /// </summary>
    /// <param name="toolName">The name of the tool to uninstall.</param>
    /// <param name="global">Whether to uninstall the tool globally.</param>
    /// <returns>True if uninstallation succeeded, otherwise false.</returns>
    public async Task<bool> UninstallDotNetToolAsync(string toolName, bool global = false)
    {
        if (string.IsNullOrEmpty(toolName))
        {
            return false;
        }

        List<string> uninstallParams = ["uninstall", toolName];
        if (global)
        {
            uninstallParams.Add("-g");
        }

        var runner = DotnetCliRunner.CreateDotNet("tool", uninstallParams);
        (int exitCode, _, _) = await runner.ExecuteAndCaptureOutputAsync();
        return exitCode == 0;
    }

    /// <summary>
    /// Gets the list of installed .NET tools, optionally refreshing the cache.
    /// </summary>
    /// <param name="refresh">Whether to refresh the tool list.</param>
    /// <param name="envVars">Optional environment variables.</param>
    /// <returns>List of <see cref="DotNetToolInfo"/> objects.</returns>
    public async Task<IList<DotNetToolInfo>> GetDotNetToolsAsync(bool refresh = false, IDictionary<string, string> ? envVars = null)
    {
        if (refresh || _dotNetTools.Count == 0)
        {
            //only want unique tools, we will try to invoke local tools over global tools
            var dotnetToolList = new List<DotNetToolInfo>();
            var runner = DotnetCliRunner.CreateDotNet("tool", ["list", "-g"], envVars);
            var localRunner = DotnetCliRunner.CreateDotNet("tool", ["list"], envVars);
            (int exitCode, string? stdOut, _) = await runner.ExecuteAndCaptureOutputAsync();
            (int localExitCode, string? localStdOut, string? localStdErr) = await localRunner.ExecuteAndCaptureOutputAsync();
            // Parse through local dotnet tools first.
            if (localExitCode == 0 && !string.IsNullOrEmpty(localStdOut))
            {
                var localDtdOutByLine = localStdOut.Split(Environment.NewLine);
                foreach (var line in localDtdOutByLine)
                {
                    var parsedDotNetTool = ParseToolInfo(line);
                    if (parsedDotNetTool is not null &&
                        IsValidDotNetTool(parsedDotNetTool))
                    {
                        dotnetToolList.Add(parsedDotNetTool);
                    }
                }

                _dotNetTools = [.. dotnetToolList];
            }

            // Parse through global tools
            if (exitCode == 0 && !string.IsNullOrEmpty(stdOut))
            {
                var stdOutByLine = stdOut.Split(Environment.NewLine);
                foreach (var line in stdOutByLine)
                {
                    var parsedDotNetTool = ParseToolInfo(line);
                    if (parsedDotNetTool is not null &&
                        IsValidDotNetTool(parsedDotNetTool) &&
                        !_dotNetTools.Any(x => x.Command.Equals(parsedDotNetTool.Command, StringComparison.OrdinalIgnoreCase)))
                    {
                        parsedDotNetTool.IsGlobalTool = true;
                        _dotNetTools.Add(parsedDotNetTool);
                    }
                }
            }
        }

        return _dotNetTools;
    }

    /// <summary>
    /// Parses a line of tool info output into a <see cref="DotNetToolInfo"/> object.
    /// </summary>
    /// <param name="line">The line to parse.</param>
    /// <returns>A <see cref="DotNetToolInfo"/> object if parsing is successful; otherwise, null.</returns>
    private static DotNetToolInfo? ParseToolInfo(string line)
    {
        var match = Regex.Match(line, @"^(\S+)\s+(\S+)\s+(\S+)");
        if (match.Success)
        {
            return new DotNetToolInfo
            {
                PackageName = match.Groups[1].Value,
                Version = match.Groups[2].Value,
                Command = match.Groups[3].Value
            };
        }

        return null;
    }

    /// <summary>
    /// Determines if a <see cref="DotNetToolInfo"/> object represents a valid .NET tool.
    /// </summary>
    /// <param name="dotnetToolInfo">The tool info to validate.</param>
    /// <returns>True if valid; otherwise, false.</returns>
    private static bool IsValidDotNetTool(DotNetToolInfo dotnetToolInfo)
    {
        return
            !dotnetToolInfo.Command.Equals("dotnet-scaffold", StringComparison.OrdinalIgnoreCase) &&
            !dotnetToolInfo.PackageName.Equals("package", StringComparison.OrdinalIgnoreCase);
    }
}
