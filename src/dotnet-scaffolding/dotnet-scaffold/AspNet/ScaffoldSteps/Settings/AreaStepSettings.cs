// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;

/// <summary>
/// Settings for an area scaffolding step, including the area name.
/// </summary>
internal class AreaStepSettings : BaseSettings
{
    /// <summary>
    /// The name of the area to scaffold.
    /// </summary>
    public required string Name { get; set; }
}
