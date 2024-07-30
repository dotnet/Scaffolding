// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.DotNet.Scaffolding.Core;
using Microsoft.DotNet.Scaffolding.Core.ComponentModel;
using Microsoft.DotNet.Scaffolding.Core.Services;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Tools.Scaffold.Services;

internal class DotNetToolService : IDotNetToolService
{
    private readonly ILogger _logger;
    private readonly IEnvironmentService _environmentService;
    private readonly IFileSystem _fileSystem;
    public DotNetToolService(ILogger<DotNetToolService> logger, IEnvironmentService environmentService, IFileSystem fileSystem)
    {
        _logger = logger;
        _environmentService = environmentService;
        _fileSystem = fileSystem;
        _dotNetTools = [];
    }

    private IList<DotNetToolInfo> _dotNetTools;
    public List<CommandInfo> GetCommands(string dotnetToolName)
    {
        List<CommandInfo>? commands = null;
        var dotnetTools = GetDotNetTools();
        if (dotnetTools.FirstOrDefault(x => x.Command.Equals(dotnetToolName, StringComparison.OrdinalIgnoreCase)) != null)
        {
            var runner = DotnetCliRunner.Create(dotnetToolName, ["get-commands"]);
            var exitCode = runner.ExecuteAndCaptureOutput(out var stdOut, out _);
            if (exitCode == 0 && !string.IsNullOrEmpty(stdOut))
            {
                try
                {
                    string escapedJsonString = stdOut.Replace("\r", "").Replace("\n", "");
                    commands = JsonSerializer.Deserialize<List<CommandInfo>>(escapedJsonString);
                }
                catch (Exception)
                {

                }
            }
        }

        return commands ?? [];
    }

    public DotNetToolInfo? GetDotNetTool(string? componentName, string? version = null)
    {
        if (string.IsNullOrEmpty(componentName))
        {
            return null;
        }
        var dotnetTools = GetDotNetTools();
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

    public IList<KeyValuePair<string, CommandInfo>> GetAllCommandsParallel(IList<DotNetToolInfo>? components = null)
    {
        if (components is null || components.Count == 0)
        {
            components = GetDotNetTools();
        }

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = System.Environment.ProcessorCount
        };

        var commands = new ConcurrentBag<KeyValuePair<string, CommandInfo>>();
        Parallel.ForEach(components, options, dotnetTool =>
        {
            var commandInfo = GetCommands(dotnetTool.Command);
            if (commandInfo != null)
            {
                foreach (var cmd in commandInfo)
                {
                    commands.Add(KeyValuePair.Create(dotnetTool.Command, cmd));
                }
            }
        });

        return commands.ToList();
    }

    public bool InstallDotNetTool(string toolName, string? version = null, bool prerelease = false, string[]? addSources = null, string? configFile = null)
    {
        if (string.IsNullOrEmpty(toolName))
        {
            return false;
        }

        var installParams = new List<string> { "install", "-g", toolName };
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
        var exitCode = runner.ExecuteAndCaptureOutput(out _, out _);
        return exitCode == 0;
    }

    public bool UninstallDotNetTool(string toolName)
    {
        if (string.IsNullOrEmpty(toolName))
        {
            return false;
        }
        var runner = DotnetCliRunner.CreateDotNet("tool", ["uninstall", "-g", toolName]);
        var exitCode = runner.ExecuteAndCaptureOutput(out _, out _);
        return exitCode == 0;
    }

    public IList<DotNetToolInfo> GetDotNetTools(bool refresh = false)
    {
        if (refresh || _dotNetTools.Count == 0)
        {
            var dotnetToolList = new List<DotNetToolInfo>();
            var runner = DotnetCliRunner.CreateDotNet("tool", ["list", "-g"]);
            var exitCode = runner.ExecuteAndCaptureOutput(out var stdOut, out _);
            if (exitCode == 0 && !string.IsNullOrEmpty(stdOut))
            {
                var stdOutByLine = stdOut.Split(System.Environment.NewLine);
                foreach (var line in stdOutByLine)
                {
                    var parsedDotNetTool = ParseToolInfo(line);
                    if (parsedDotNetTool != null &&
                        !parsedDotNetTool.Command.Equals("dotnet-scaffold", StringComparison.OrdinalIgnoreCase) &&
                        !parsedDotNetTool.PackageName.Equals("package", StringComparison.OrdinalIgnoreCase))
                    {
                        dotnetToolList.Add(parsedDotNetTool);
                    }
                }

                _dotNetTools = dotnetToolList;
            }
        }

        return _dotNetTools;
    }

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
}
