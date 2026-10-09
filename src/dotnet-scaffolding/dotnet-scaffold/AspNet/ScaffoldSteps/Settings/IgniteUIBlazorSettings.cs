// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;

/// <summary>
/// Settings for the Ignite UI for Blazor scaffolding steps.
/// </summary>
internal class IgniteUIBlazorSettings : BaseSettings
{
    /// <summary>
    /// The requested theme ('bootstrap', 'material', 'fluent' or 'indigo'), or null when '--theme' was omitted: the
    /// theme already linked in the host page is then kept, otherwise 'bootstrap' is used.
    /// </summary>
    public string? Theme { get; set; }
    /// <summary>
    /// The requested theme variant ('light' or 'dark'), or null when '--theme-variant' was omitted: the variant already
    /// linked in the host page is then kept, otherwise 'light' is used.
    /// </summary>
    public string? ThemeVariant { get; set; }
    /// <summary>
    /// Indicates if prerelease package versions should be installed.
    /// </summary>
    public bool Prerelease { get; set; }
}
