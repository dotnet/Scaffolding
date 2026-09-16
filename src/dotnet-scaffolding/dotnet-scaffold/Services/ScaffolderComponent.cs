// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.ComponentModel;

namespace Microsoft.DotNet.Tools.Scaffold.Services;

/// <summary>
/// Describes a scaffolding component and the commands it provides.
/// </summary>
internal sealed class ScaffolderComponent(
    DotNetToolInfo component,
    IReadOnlyList<CommandInfo> commands)
{
    public DotNetToolInfo Component { get; } = component;

    public IReadOnlyList<CommandInfo> Commands { get; } = commands;
}
