// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Models;

/// <summary>
/// Represents properties related to a Blazor CRUD application's component and routing configuration.
/// Used to determine which Blazor features and render modes are present or needed in Program.cs.
/// </summary>
internal class BlazorCrudAppProperties
{
    /// <summary>
    /// Indicates if WebApplication.CreateBuilder.AddRazorComponents() exists in Program.cs.
    /// </summary>
    public bool AddRazorComponentsExists { get; set; }

    /// <summary>
    /// Indicates if AddRazorComponents().AddInteractiveServerComponents() exists in Program.cs.
    /// </summary>
    public bool InteractiveServerComponentsExists { get; set; }

    /// <summary>
    /// Indicates if AddRazorComponents().AddInteractiveWebAssemblyComponents() exists in Program.cs.
    /// </summary>
    public bool InteractiveWebAssemblyComponentsExists { get; set; }

    /// <summary>
    /// Indicates if WebApplication.MapRazorComponents() exists in Program.cs.
    /// </summary>
    public bool MapRazorComponentsExists { get; set; }

    /// <summary>
    /// Indicates if MapRazorComponents().AddInteractiveServerRenderMode() is needed in Program.cs.
    /// </summary>
    public bool InteractiveServerRenderModeNeeded { get; set; }

    /// <summary>
    /// Indicates if MapRazorComponents().AddInteractiveWebAssemblyRenderMode() is needed in Program.cs.
    /// </summary>
    public bool InteractiveWebAssemblyRenderModeNeeded { get; set; }

    /// <summary>
    /// Indicates if the HeadOutlet is registered globally.
    /// </summary>
    public bool IsHeadOutletGlobal { get; set; }

    /// <summary>
    /// Indicates if routes are registered globally.
    /// </summary>
    public bool AreRoutesGlobal { get; set; }
}
