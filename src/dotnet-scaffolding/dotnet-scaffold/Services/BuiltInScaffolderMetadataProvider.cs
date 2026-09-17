// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.CommandLine;
using Microsoft.DotNet.Scaffolding.Core.ComponentModel;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Tools.Scaffold.Helpers;

namespace Microsoft.DotNet.Tools.Scaffold.Services;

/// <summary>
/// Provides metadata for scaffolders that are built into the current process.
/// </summary>
internal sealed class BuiltInScaffolderMetadataProvider(IEnumerable<IScaffolder> scaffolders) : IScaffolderMetadataProvider
{
    private const string ComponentCommand = "dotnet-scaffold";
    private const string ComponentPackage = "Microsoft.dotnet-scaffold";

    private readonly IEnumerable<IScaffolder> _scaffolders = scaffolders;

    public IReadOnlyList<ScaffolderComponent> GetComponents()
    {
        var commands = _scaffolders
            .Select(scaffolder => scaffolder.ToCommandInfo())
            .ToList();

        DotNetToolInfo component = new()
        {
            PackageName = ComponentPackage,
            Version = ToolHelper.GetToolVersion() ?? string.Empty,
            Command = ComponentCommand,
            IsGlobalTool = true
        };

        return [new ScaffolderComponent(component, commands)];
    }
}
