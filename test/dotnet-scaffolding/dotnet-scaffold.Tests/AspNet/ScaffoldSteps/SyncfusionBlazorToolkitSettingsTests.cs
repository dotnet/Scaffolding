// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.ScaffoldSteps;

public class SyncfusionBlazorToolkitSettingsTests
{
    [Fact]
    public void Defaults_AreSafe()
    {
        var settings = new SyncfusionBlazorToolkitSettings { Project = "Test.csproj" };
        Assert.Null(settings.ThemeFile);
        Assert.False(settings.ThemeFileSkipped);
        Assert.Null(settings.ImportsFile);
        Assert.False(settings.ImportsFileSkipped);
        Assert.False(settings.PackageAlreadyReferenced);
        Assert.False(settings.ServicesAlreadyRegistered);
        Assert.False(settings.Prerelease);
    }

    [Fact]
    public void CanSetAllProperties()
    {
        var settings = new SyncfusionBlazorToolkitSettings
        {
            Project = "Test.csproj",
            Prerelease = true,
            ThemeFile = "Components/App.razor",
            ThemeFileSkipped = false,
            ImportsFile = "Components/_Imports.razor",
            ImportsFileSkipped = false,
            PackageAlreadyReferenced = true,
            ServicesAlreadyRegistered = true,
        };

        Assert.Equal("Components/App.razor", settings.ThemeFile);
        Assert.Equal("Components/_Imports.razor", settings.ImportsFile);
        Assert.True(settings.PackageAlreadyReferenced);
        Assert.True(settings.ServicesAlreadyRegistered);
        Assert.True(settings.Prerelease);
    }
}
