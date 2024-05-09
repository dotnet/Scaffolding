// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.Scaffolding.ComponentModel;

/// <summary>
/// What we want components to initialize and return (to stdout serialized as json) when 'get-commands' is invoked.
/// Should be serialized as 'List<CommandInfo>'
/// </summary>
public class CommandInfo
{
    public required string Name { get; init; }
    public required string DisplayName { get; init; }
    public required string DisplayCategory { get; init; }
    public string? Description { get; set; }
    public required Parameter[] Parameters { get; init; }
}
