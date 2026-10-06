// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;

/// <summary>
/// Settings for Syncfusion Blazor Toolkit setup scaffolding.
/// </summary>
internal class SyncfusionBlazorToolkitSettings : BaseSettings
{
    /// <summary>
    /// Indicates if prerelease packages should be used.
    /// </summary>
    public bool Prerelease { get; init; }

    /// <summary>
    /// Resolved theme target file path relative to the project root (e.g.
    /// "Components/App.razor" or "wwwroot/index.html"), or null when no
    /// suitable host file was found and the theme stylesheet step should
    /// be skipped.
    /// </summary>
    public string? ThemeFile { get; set; }

    /// <summary>
    /// True when no theme host file was found and the theme stylesheet
    /// step was skipped. False when the theme was applied (or already
    /// applied on a previous run).
    /// </summary>
    public bool ThemeFileSkipped { get; set; }
}
