// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers.Syncfusion;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Syncfusion.Settings;

/// <summary>
/// Settings for Syncfusion Blazor Toolkit setup scaffolding.
/// </summary>
internal class SyncfusionBlazorToolkitSettings : BaseSettings
{
    /// <summary>
    /// Project-relative path of the <c>_Imports.razor</c> file the
    /// scaffolder targeted on this run. <c>null</c> when the scaffolder
    /// could not find or create a suitable file.
    /// </summary>
    public string? ImportsFile { get; set; }

    /// <summary>
    /// True when no <c>_Imports.razor</c> file could be found or created
    /// and the using-directive insertion was skipped.
    /// </summary>
    public bool ImportsFileSkipped { get; set; }

    /// <summary>
    /// True when the @using directive is present in <see cref="ImportsFile"/>
    /// after the imports step (it was already there, or the scaffolder added it).
    /// </summary>
    public bool ImportsApplied { get; set; }

    /// <summary>
    /// True when the Toolkit service registration was already in the entry
    /// point before the scaffolder ran.
    /// </summary>
    public bool ServicesAlreadyRegistered { get; set; }

    /// <summary>
    /// True when the Toolkit service registration is in the entry point
    /// after the scaffolder ran (verified, not assumed).
    /// </summary>
    public bool ServicesApplied { get; set; }

    /// <summary>
    /// Project-relative path of the entry point (usually <c>Program.cs</c>) the
    /// scaffolder targeted. <c>null</c> when no safe entry point was found.
    /// </summary>
    public string? ProgramCsFile { get; set; }

    /// <summary>
    /// True when no entry point could be found and the service-registration
    /// insertion was skipped.
    /// </summary>
    public bool ProgramCsFileSkipped { get; set; }

    /// <summary>
    /// True when the Toolkit package is referenced after the scaffolder ran,
    /// confirmed from the project's evaluated package references.
    /// </summary>
    public bool PackageApplied { get; set; }

    /// <summary>
    /// Path of the WebAssembly client .csproj resolved from the server's
    /// project references. Null when the project has no client or the
    /// resolution step failed early with an actionable error.
    /// </summary>
    public string? ClientProjectPath { get; set; }

    /// <summary>
    /// The application kind, decided once by the validate step from evaluated
    /// project state and reused by every later step instead of re-reading the
    /// project file.
    /// </summary>
    public BlazorAppKind AppKind { get; set; } = BlazorAppKind.NotBlazor;

    /// <summary>
    /// Interactive hosting the entry point has actually registered (services and
    /// endpoint render modes), plus the global render mode set in <c>App.razor</c>.
    /// "No global render mode" is not "no interactivity": components may opt in per page.
    /// </summary>
    public BlazorHostingFacts Hosting { get; set; } = BlazorHostingFacts.Unknown;

    /// <summary>
    /// What this run actually changed in each project it touched. Used by the summary
    /// so it never reports a modified project as a no-op.
    /// </summary>
    public ProjectChanges Server { get; } = new();

    /// <summary>
    /// Same as <see cref="Server"/> for the referenced WebAssembly client project, when there is one.
    /// </summary>
    public ProjectChanges Client { get; } = new();

    /// <summary>
    /// Detected project layout (single-project Blazor Web App, standalone
    /// Blazor WebAssembly, server or client side of a Blazor Web App Auto
    /// split, or Hybrid/MAUI — which is explicitly unsupported).
    /// </summary>
    public SyncfusionBlazorToolkitLayout Layout { get; set; } = SyncfusionBlazorToolkitLayout.SingleProject;
}

/// <summary>
/// Layouts supported by the Syncfusion Blazor Toolkit scaffolder. The
/// multi-project variants are detected when the server project references
/// a Microsoft.NET.Sdk.BlazorWebAssembly project (the canonical Blazor
/// Web App Auto template shape).
/// </summary>
internal enum SyncfusionBlazorToolkitLayout
{
    /// <summary>
    /// Single-project Blazor Web App / Server / standalone WASM.
    /// </summary>
    SingleProject = 0,

    /// <summary>
    /// Server side of a Blazor Web App Auto layout. Program.cs /
    /// <c>AddSyncfusionBlazorToolkit()</c> live here; the @using
    /// directive should also be added here so it is available to any
    /// Components folder authored on the server.
    /// </summary>
    AutoServer = 1,

    /// <summary>
    /// Client (.Client) side of a Blazor Web App Auto layout. The
    /// @using directive and the static assets live here.
    /// </summary>
    AutoClient = 2,

    /// <summary>
    /// Explicitly unsupported: Hybrid / MAUI Blazor Hybrid layouts.
    /// Scaffolder fails fast in this case because MauiProgram.cs is not
    /// a supported host.
    /// </summary>
    UnsupportedHybrid = 99,
}

/// <summary>
/// Facts recorded while the scaffolder ran against one project.
/// </summary>
internal sealed class ProjectChanges
{
    /// <summary>Toolkit version referenced before the scaffolder ran; <c>null</c> when it was not referenced.</summary>
    public string? PackageVersionBefore { get; set; }

    /// <summary>Toolkit version referenced after the scaffolder ran; <c>null</c> when it is not referenced.</summary>
    public string? PackageVersionAfter { get; set; }

    /// <summary>True when the add-package step changed the reference (added it or changed its version).</summary>
    public bool PackageChanged =>
        !string.Equals(PackageVersionBefore, PackageVersionAfter, StringComparison.OrdinalIgnoreCase);

    /// <summary>True when the Toolkit is referenced after the run.</summary>
    public bool PackageReferenced => PackageVersionAfter is not null;

    /// <summary>True when the service-registration step wrote to the entry point.</summary>
    public bool ServicesInserted { get; set; }

    /// <summary>True when the imports step wrote to (or created) <c>_Imports.razor</c>.</summary>
    public bool ImportsInserted { get; set; }

    /// <summary>True when anything in this project was modified by the run.</summary>
    public bool AnyChange => PackageChanged || ServicesInserted || ImportsInserted;
}
