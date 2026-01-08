// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Roslyn.Services;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Common;

internal class ProjectInfo
{
    public ProjectInfo(string? projectPath)
    {
        ProjectPath = projectPath;
        LowestSupportedTargetFramework = projectPath is not null ? TargetFrameworkHelpers.GetLowestCompatibleTargetFramework(projectPath) : null;
    }

    //Project info
    public string? ProjectPath { get; }
    public CodeService? CodeService { get; set; }
    public IList<string> CodeChangeOptions { get; set; } = new List<string>();
    //if multiple are found, use the lowest one
    public string? LowestSupportedTargetFramework { get; }
    public IList<string>? Capabilities { get; set; }
}
