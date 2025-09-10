// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;

/// <summary>
/// Settings for Minimal API scaffolding steps, including endpoints and OpenAPI options.
/// </summary>
internal class MinimalApiSettings : EfWithModelStepSettings
{
    /// <summary>
    /// The endpoints file or class name for the minimal API.
    /// </summary>
    public string? Endpoints { get; set; }
    /// <summary>
    /// Indicates if OpenAPI should be enabled for the minimal API.
    /// </summary>
    public bool OpenApi { get; set; } = true;
}
