// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Models;

/// <summary>
/// Represents the model for Ignite UI for Blazor scaffolding, containing the resolved project
/// layout (host page, _Imports.razor, WebAssembly client project) and the theme stylesheet to link.
/// </summary>
internal class IgniteUIBlazorModel
{
    /// <summary>
    /// Gets the project information.
    /// </summary>
    public required ProjectInfo ProjectInfo { get; init; }
    /// <summary>
    /// Gets the path to the project file (.csproj).
    /// </summary>
    public required string ProjectPath { get; init; }
    /// <summary>
    /// Gets the project directory.
    /// </summary>
    public required string BaseOutputPath { get; init; }
    /// <summary>
    /// Gets the selected theme name ('bootstrap', 'material', 'fluent' or 'indigo').
    /// </summary>
    public required string Theme { get; init; }
    /// <summary>
    /// Gets the selected theme variant ('light' or 'dark').
    /// </summary>
    public required string ThemeVariant { get; init; }
    /// <summary>
    /// Gets the project-relative stylesheet path to link, e.g. '_content/IgniteUI.Blazor/themes/light/bootstrap.css'.
    /// </summary>
    public required string StylesheetPath { get; init; }
    /// <summary>
    /// Gets a value indicating whether the target project is a standalone Blazor WebAssembly project
    /// (as opposed to a Blazor Web App / Blazor Server project hosted by ASP.NET Core).
    /// </summary>
    public required bool IsWebAssemblyProject { get; init; }
    /// <summary>
    /// Gets a value indicating whether the target project is a .NET MAUI Blazor Hybrid app, whose services are
    /// registered in MauiProgram.cs and whose host page is wwwroot/index.html.
    /// </summary>
    public bool IsMauiBlazorHybridProject { get; init; }
    /// <summary>
    /// Gets the resolved host page that contains the &lt;head&gt; element (Components/App.razor,
    /// Pages/_Host.cshtml, Pages/_Layout.cshtml or wwwroot/index.html), or null when none was found.
    /// </summary>
    public string? HostPagePath { get; init; }
    /// <summary>
    /// Gets the _Imports.razor file that should receive the '@using IgniteUI.Blazor.Controls' directive.
    /// The file is created when it does not exist yet.
    /// </summary>
    public required string ImportsFilePath { get; init; }
    /// <summary>
    /// Gets the Blazor WebAssembly client project referenced by a Blazor Web App server project, or null when
    /// the project has no client (Blazor Server, server-only Blazor Web App, or standalone Blazor WebAssembly).
    /// </summary>
    public string? ClientProjectPath { get; init; }
    /// <summary>
    /// Gets the _Imports.razor of the Blazor WebAssembly client project, or null when there is no client project.
    /// </summary>
    public string? ClientImportsFilePath { get; init; }
}
