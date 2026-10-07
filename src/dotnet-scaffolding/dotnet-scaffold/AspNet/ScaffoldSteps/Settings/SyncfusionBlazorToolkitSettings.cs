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
    /// Resolved theme target file path relative to the project root, in
    /// canonical (forward-slash) form (e.g. "Components/App.razor" or
    /// "wwwroot/index.html"), or null when no suitable host file was
    /// found and the theme stylesheet step should be skipped.
    /// </summary>
    public string? ThemeFile { get; set; }

    /// <summary>
    /// True when no theme host file was found and the theme stylesheet
    /// step was skipped. False when the theme was applied (or already
    /// applied on a previous run).
    /// </summary>
    public bool ThemeFileSkipped { get; set; }

    /// <summary>
    /// Project-relative path of the _Imports.razor file where the
    /// <c>@using Syncfusion.Blazor.Toolkit</c> directive should be
    /// inserted, in canonical (forward-slash) form
    /// (e.g. "Components/_Imports.razor" or "_Imports.razor"). Null when
    /// no _Imports.razor file was found in any conventional location and
    /// the using-directive insertion should be skipped.
    /// </summary>
    public string? ImportsFile { get; set; }

    /// <summary>
    /// True when no _Imports.razor file was found and the using-directive
    /// insertion was skipped. False when the using directive was applied
    /// (or already present from a previous run).
    /// </summary>
    public bool ImportsFileSkipped { get; set; }

    /// <summary>
    /// True when the project already had a Syncfusion.Blazor.Toolkit
    /// package reference before the scaffolder ran. Used by the summary
    /// step to report a no-op (idempotent) outcome.
    /// </summary>
    public bool PackageAlreadyReferenced { get; set; }

    /// <summary>
    /// True when the target Program.cs already registers the
    /// <c>AddSyncfusionBlazorToolkit</c> service before the scaffolder
    /// ran. Used by the summary step to report partial configuration.
    /// </summary>
    public bool ServicesAlreadyRegistered { get; set; }
}
