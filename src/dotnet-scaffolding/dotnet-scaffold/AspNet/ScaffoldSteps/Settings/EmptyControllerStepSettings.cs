// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;

/// <summary>
/// Settings for the empty controller scaffolding step, including whether to add actions.
/// </summary>
internal class EmptyControllerStepSettings : DotnetNewStepSettings
{
    /// <summary>
    /// Indicates whether actions should be added to the controller.
    /// </summary>
    public required bool Actions { get; init; }
}
