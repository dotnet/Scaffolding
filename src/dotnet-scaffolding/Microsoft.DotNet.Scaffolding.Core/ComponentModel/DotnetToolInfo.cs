// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Microsoft.DotNet.Scaffolding.Core.ComponentModel;

/// <summary>
/// Info from 'dotnet tool list' and 'dotnet tool list -g'
/// </summary>
internal class DotNetToolInfo
{
    public string PackageName { get; set; } = default!;
    public string Version { get; set; } = default!;
    public string Command { get; set; } = default!;
    public bool IsGlobalTool { get; set; } = false;
    public string ToDisplayString()
    {
        return $"{Command} ({PackageName} v{Version})";
    }
}
