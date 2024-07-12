// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.DotNet.Scaffolding.ComponentModel;
using Microsoft.DotNet.Scaffolding.Helpers.General;
using Microsoft.DotNet.Scaffolding.Helpers.Services.Environment;

namespace Microsoft.DotNet.Scaffolding.Helpers.Services;

internal class DotNetToolService : IDotNetToolService
{
    private readonly IEnvironmentService _environmentService;
    private readonly IFileSystem _fileSystem;
    public DotNetToolService(IEnvironmentService environmentService, IFileSystem fileSystem)
    {
        _environmentService = environmentService;
        _fileSystem = fileSystem;
    }

    private IList<DotNetToolInfo>? _dotNetTools;
    public IList<DotNetToolInfo> GlobalDotNetTools
    {
        get
        {
            _dotNetTools ??= GetDotNetTools();
            return _dotNetTools;
        }
    }

    public List<CommandInfo> GetCommands(string dotnetToolName)
    {
        List<CommandInfo>? commands = null;
        if (GlobalDotNetTools.FirstOrDefault(x => x.Command.Equals(dotnetToolName, StringComparison.OrdinalIgnoreCase)) != null)
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

        var matchingTools = GlobalDotNetTools.Where(x => x.PackageName.Equals(componentName, StringComparison.OrdinalIgnoreCase));
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
            components = GlobalDotNetTools;
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

    public bool InstallDotNetTool(string toolName, string? version = null, bool prerelease = false)
    {
        if (string.IsNullOrEmpty(toolName))
        {
            return false;
        }

        var installParams = new List<string> { "install", "-g", toolName };
        if (!string.IsNullOrEmpty(version))
        {
            installParams.Add("-v");
            installParams.Add(version);
        }

        if (prerelease)
        {
            installParams.Add("--prerelease");
        }
        
        var runner = DotnetCliRunner.CreateDotNet("tool", installParams);
        var exitCode = runner.ExecuteAndCaptureOutput(out _, out _);
        return exitCode == 0;
    }

    public IList<DotNetToolInfo> GetDotNetTools()
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
        }

        return dotnetToolList;
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
