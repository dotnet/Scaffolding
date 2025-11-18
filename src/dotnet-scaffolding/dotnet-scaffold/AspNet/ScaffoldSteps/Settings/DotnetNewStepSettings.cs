// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;

/// <summary>
/// Settings for the 'dotnet new' scaffolding step, including command and naming options.
/// </summary>
internal class DotnetNewStepSettings : BaseSettings
{
    /// <summary>
    /// The name of the dotnet new command to run.
    /// </summary>
    public required string CommandName { get; set; }
    /// <summary>
    /// The name for the new item to be created.
    /// </summary>
    public required string Name { get; set; }
    /// <summary>
    /// The namespace for the new item, if specified.
    /// </summary>
    public string? NamespaceName { get; set; }
}
