// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;

/// <summary>
/// Settings for the Ignite UI for Blazor scaffolding steps.
/// </summary>
internal class IgniteUIBlazorSettings : BaseSettings
{
    /// <summary>
    /// Which Ignite UI package set to add: 'Lite', 'GridLite' or 'All'.
    /// </summary>
    public required string Package { get; set; }
    /// <summary>
    /// The theme to link ('bootstrap', 'material', 'fluent' or 'indigo').
    /// </summary>
    public required string Theme { get; set; }
    /// <summary>
    /// The theme variant to link ('light' or 'dark').
    /// </summary>
    public required string ThemeVariant { get; set; }
    /// <summary>
    /// Indicates if prerelease package versions should be installed.
    /// </summary>
    public bool Prerelease { get; set; }
}
