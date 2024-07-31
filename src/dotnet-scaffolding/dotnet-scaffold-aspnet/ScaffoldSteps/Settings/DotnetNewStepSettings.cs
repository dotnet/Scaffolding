// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;

internal class DotnetNewStepSettings : BaseSettings
{
    public required string CommandName { get; set; }
    public required string Name { get; set; }
}
