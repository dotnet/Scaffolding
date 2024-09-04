// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;

internal class BlazorIdentitySettings : BaseSettings
{
    public required string DatabaseProvider { get; set; }
    public required string DataContext { get; set; }
    public bool Prerelease { get; set; }
    public bool Overwrite { get; set; }
}
