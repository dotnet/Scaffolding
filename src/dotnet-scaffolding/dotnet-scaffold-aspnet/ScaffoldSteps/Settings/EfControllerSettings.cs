// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;

internal class EfControllerSettings : EfWithModelStepSettings
{
    public required string ControllerType { get; set; }
    public required string ControllerName { get; set; }
}
