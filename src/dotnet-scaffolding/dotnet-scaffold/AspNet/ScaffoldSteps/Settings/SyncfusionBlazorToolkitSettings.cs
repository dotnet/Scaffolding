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
}
