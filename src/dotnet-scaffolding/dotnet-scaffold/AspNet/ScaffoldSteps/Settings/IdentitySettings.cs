// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;

/// <summary>
/// Settings for Identity scaffolding steps, including database provider, context, and scenario options.
/// </summary>
internal class IdentitySettings : BaseSettings
{
    /// <summary>
    /// The database provider to use for Identity (e.g., SqlServer, Sqlite).
    /// </summary>
    public required string DatabaseProvider { get; set; }
    /// <summary>
    /// The name of the DbContext class for Identity.
    /// </summary>
    public required string DataContext { get; set; }
    /// <summary>
    /// Indicates if prerelease packages should be used.
    /// </summary>
    public bool Prerelease { get; set; }
    /// <summary>
    /// Indicates if existing files should be overwritten.
    /// </summary>
    public bool Overwrite { get; set; }
    /// <summary>
    /// Indicates if the scenario is for Blazor.
    /// </summary>
    public bool BlazorScenario { get; set; }
}
