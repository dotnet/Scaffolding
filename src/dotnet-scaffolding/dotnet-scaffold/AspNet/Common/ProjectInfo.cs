// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Roslyn.Services;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Common;

/// <summary>
/// Represents information about a project, including its path, code service, target framework, and capabilities.
/// </summary>
internal class ProjectInfo
{
    /// <summary>
    /// Gets or sets the path to the project file.
    /// </summary>
    public string? ProjectPath { get; set; }
    /// <summary>
    /// Gets or sets the code service for the project.
    /// </summary>
    public CodeService? CodeService { get; set; }
    /// <summary>
    /// Gets or sets the list of code change options for the project.
    /// </summary>
    public IList<string>? CodeChangeOptions { get; set; }
    /// <summary>
    /// Gets or sets the lowest target framework for the project (if multiple are found).
    /// </summary>
    public string? LowestTargetFramework { get; set; }
    /// <summary>
    /// Gets or sets the list of project capabilities.
    /// </summary>
    public IList<string>? Capabilities { get; set; }
}
