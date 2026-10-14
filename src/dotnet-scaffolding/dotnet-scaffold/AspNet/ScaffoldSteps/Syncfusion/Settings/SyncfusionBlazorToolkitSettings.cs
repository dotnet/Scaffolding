// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    /// and the using-directive insertion was skipped. False when a file
    /// was modified successfully (or the directive was already present).
    /// </summary>
    public bool ImportsFileSkipped { get; set; }

    /// <summary>
    /// True when the @using directive is now present in <see cref="ImportsFile"/>
    /// (either it was already there, or the scaffolder added it).
    /// </summary>
    public bool ImportsApplied { get; set; }

    /// <summary>
    /// True when the imports step actually appended the directive to
    /// the target file. False when the directive was already present
    /// (a no-op) or no target file could be found.
    /// </summary>
    public bool ImportsInserted { get; set; }

    /// <summary>
    /// True when the project already had a Syncfusion.Blazor.Toolkit
    /// package reference before the scaffolder ran.
    /// </summary>
    public bool PackageAlreadyReferenced { get; set; }

    /// <summary>
    /// True when the target Program.cs already registers the
    /// <c>AddSyncfusionBlazorToolkit</c> service before the scaffolder
    /// ran. Used by the summary step to report partial configuration.
    /// </summary>
    public bool ServicesAlreadyRegistered { get; set; }

    /// <summary>
    /// True when the shared code-modification step reported
    /// AddSyncfusionBlazorToolkit() is present in Program.cs after the
    /// scaffolder ran.
    /// </summary>
    public bool ServicesApplied { get; set; }

    /// <summary>
    /// True when the Program.cs service-registration step actually
    /// appended the call (or created a new file with the call in
    /// place). False when the call was already present (a no-op) or no
    /// target file could be found.
    /// </summary>
    public bool ServicesInserted { get; set; }

    /// <summary>
    /// Project-relative path of the <c>Program.cs</c> file the
    /// scaffolder targeted on this run. <c>null</c> when the scaffolder
    /// could not find or create a suitable file.
    /// </summary>
    public string? ProgramCsFile { get; set; }

    /// <summary>
    /// True when no <c>Program.cs</c> file could be found or created
    /// and the service-registration insertion was skipped. False when
    /// a file was modified successfully (or the registration was
    /// already present).
    /// </summary>
    public bool ProgramCsFileSkipped { get; set; }

    /// <summary>
    /// True when the add-packages step actually added or updated a
    /// Syncfusion.Blazor.Toolkit PackageReference. False when the package
    /// was already referenced (no change) or the step was skipped.
    /// </summary>
    public bool PackageApplied { get; set; }

    /// <summary>
    /// Path of the WebAssembly client .csproj resolved from the server's
    /// project references. Null when the project has no client or the
    /// resolution step failed early with an actionable error.
    /// </summary>
    public string? ClientProjectPath { get; set; }

    /// <summary>
    /// Render mode actually configured in the project (e.g. InteractiveServer,
    /// InteractiveWebAssembly, InteractiveAuto, or "None" for pure SSR).
    /// Populated from <c>App.razor</c> and Program.cs diagnostics and used
    /// by the summary step to give accurate, hosting-specific guidance.
    /// </summary>
    public string DetectedRenderMode { get; set; } = "None";

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