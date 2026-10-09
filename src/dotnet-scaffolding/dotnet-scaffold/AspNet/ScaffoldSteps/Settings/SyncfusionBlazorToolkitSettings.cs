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
    /// Project-relative path of the _Imports.razor file where the
    /// <c>@using Syncfusion.Blazor.Toolkit</c> directive should be
    /// inserted, in OS-native separator form (e.g.
    /// "Components\_Imports.razor" on Windows or "Components/_Imports.razor"
    /// on Linux). Null when no _Imports.razor file was found in any
    /// conventional location and the using-directive insertion should be
    /// skipped.
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

    /// <summary>
    /// Detected project layout. <see cref="SyncfusionBlazorToolkitLayout.SingleProject"/>
    /// for plain Blazor Web App / standalone WASM and the Auto variants for
    /// Blazor Web App with a .Client sibling. Used by the summary step to
    /// print multi-project guidance when the scaffolder is run on the
    /// server side of an Auto layout.
    /// </summary>
    public SyncfusionBlazorToolkitLayout Layout { get; set; } = SyncfusionBlazorToolkitLayout.SingleProject;

    /// <summary>
    /// When the current project is the server side of an Auto layout
    /// (Program.cs / DI here, components in <c>.Client</c>), this holds
    /// the absolute path of the sibling .Client .csproj. Null when no
    /// sibling was found.
    /// </summary>
    public string? SiblingClientProject { get; set; }
}

/// <summary>
/// Layouts supported by the Syncfusion Blazor Toolkit scaffolder. The
/// multi-project variants are detected when the current project has a
/// sibling <c>&lt;name&gt;.Client.csproj</c> project — the canonical
/// <c>dotnet new blazor --interactivity Auto</c> template shape.
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
    /// directive should be added to the .Client project's _Imports.razor.
    /// </summary>
    AutoServer = 1,

    /// <summary>
    /// Client (.Client) side of a Blazor Web App Auto layout. The
    /// @using directive and the static assets live here.
    /// </summary>
    AutoClient = 2,
}
