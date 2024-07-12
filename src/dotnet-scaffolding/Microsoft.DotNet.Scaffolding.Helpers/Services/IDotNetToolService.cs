// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.ComponentModel;

namespace Microsoft.DotNet.Scaffolding.Helpers.Services;

internal interface IDotNetToolService
{
    IList<KeyValuePair<string, CommandInfo>> GetAllCommandsParallel(IList<DotNetToolInfo>? components = null);
    DotNetToolInfo? GetDotNetTool(string? componentName, string? version = null);
    IList<DotNetToolInfo> GetDotNetTools(bool refresh = false);
    bool InstallDotNetTool(string toolName, string? version = null, bool prerelease = false);
    List<CommandInfo> GetCommands(string dotnetToolName);
}
