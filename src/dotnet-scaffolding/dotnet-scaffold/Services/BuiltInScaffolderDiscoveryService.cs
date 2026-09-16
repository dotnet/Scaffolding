// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.CommandLine;
using Microsoft.DotNet.Scaffolding.Core.ComponentModel;
using Microsoft.DotNet.Tools.Scaffold.Helpers;

namespace Microsoft.DotNet.Tools.Scaffold.Services;

/// <summary>
/// Provides metadata for scaffolders that are built into the current process.
/// </summary>
internal class BuiltInScaffolderDiscoveryService(IScaffoldRunner scaffoldRunner)
{
    private const string ComponentCommand = "dotnet-scaffold";
    private const string ComponentPackage = "Microsoft.dotnet-scaffold";

    private readonly IScaffoldRunner _scaffoldRunner = scaffoldRunner;

    public DotNetToolInfo Component { get; } = new()
    {
        PackageName = ComponentPackage,
        Version = ToolHelper.GetToolVersion() ?? string.Empty,
        Command = ComponentCommand,
        IsGlobalTool = true
    };

    public IList<KeyValuePair<string, CommandInfo>> GetCommands()
    {
        if (_scaffoldRunner.Scaffolders is null)
        {
            return [];
        }

        return _scaffoldRunner.Scaffolders
            .SelectMany(category => category.Value)
            .Select(scaffolder => KeyValuePair.Create(ComponentCommand, scaffolder.ToCommandInfo()))
            .ToList();
    }
}
