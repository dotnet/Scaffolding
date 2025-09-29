// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;

/// <summary>
/// Settings for EF controller scaffolding steps, including controller type and name.
/// </summary>
internal class EfControllerSettings : EfWithModelStepSettings
{
    /// <summary>
    /// The type of controller to scaffold (e.g., API, MVC).
    /// </summary>
    public required string ControllerType { get; set; }
    /// <summary>
    /// The name of the controller to scaffold.
    /// </summary>
    public required string ControllerName { get; set; }
}
