// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Models;
internal class BlazorCrudAppProperties
{
    //if WebApplication.CreateBuilder.AddRazorComponents() exists in Program.cs
    public bool AddRazorComponentsExists { get; set; }
    // if AddRazorComponents().AddInteractiveServerComponents() exists in Program.cs
    public bool InteractiveServerComponentsExists { get; set; }
    // if AddRazorComponents().AddInteractiveWebAssemblyComponents() exists in Program.cs
    public bool InteractiveWebAssemblyComponentsExists { get; set; }
    // if WebApplication.MapRazorComponents<App>() exists in Program.cs
    public bool MapRazorComponentsExists { get; set; }
    // if MapRazorComponents<App>().AddInteractiveServerRenderMode() is needed in Program.cs
    public bool InteractiveServerRenderModeNeeded { get; set; }
    // if MapRazorComponents<App>().AddInteractiveWebAssemblyRenderMode() is needed in Program.cs
    public bool InteractiveWebAssemblyRenderModeNeeded { get; set; }
    public bool IsHeadOutletGlobal { get; set; }
    public bool AreRoutesGlobal { get; set; }
    
    // if Routes.razor exists (should be prioritized over App.razor)
    public bool HasRoutesRazor { get; set; }
    // if App.razor exists  
    public bool HasAppRazor { get; set; }
    // if an existing NotFound page was found
    public bool HasExistingNotFound { get; set; }
    // the route from an existing NotFound page (default: "/not-found")
    public string ExistingNotFoundRoute { get; set; } = "/not-found";
}
