// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Commands.Settings;

internal class DotnetNewCommandSettings : ICommandSettings
{
    public required string CommandName { get; set; }
    public string? Project { get; set; }
    public required string Name { get; set; }
}
