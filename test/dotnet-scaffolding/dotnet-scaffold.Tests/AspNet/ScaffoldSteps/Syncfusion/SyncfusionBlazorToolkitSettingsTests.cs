// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;
using Xunit;

namespace Microsoft.DotNet.Scaffold.Tests.AspNet.ScaffoldSteps.Syncfusion;

public class SyncfusionBlazorToolkitSettingsTests
{
    [Fact]
    public void Defaults_AreSafe()
    {
        var settings = new SyncfusionBlazorToolkitSettings { Project = "C:/absolute/Test.csproj" };
        Assert.Null(settings.ImportsFile);
        Assert.False(settings.ImportsFileSkipped);
        Assert.False(settings.PackageAlreadyReferenced);
        Assert.False(settings.ServicesAlreadyRegistered);
        Assert.False(settings.PackageApplied);
        Assert.False(settings.ServicesApplied);
        Assert.False(settings.ImportsApplied);
        Assert.Null(settings.ClientProjectPath);
        Assert.Equal("None", settings.DetectedRenderMode);
        Assert.False(settings.Prerelease);
    }

    [Fact]
    public void CanSetAllProperties()
    {
        var settings = new SyncfusionBlazorToolkitSettings
        {
            Project = "C:/absolute/Test.csproj",
            Prerelease = true,
            ImportsFile = Path.Combine("Components", "_Imports.razor"),
            ImportsFileSkipped = false,
            ImportsApplied = true,
            PackageAlreadyReferenced = true,
            ServicesAlreadyRegistered = true,
            PackageApplied = true,
            ServicesApplied = true,
            ClientProjectPath = "C:/absolute/Test.Client.csproj",
            DetectedRenderMode = "InteractiveServer",
            Layout = SyncfusionBlazorToolkitLayout.AutoServer,
        };

        Assert.Equal(Path.Combine("Components", "_Imports.razor"), settings.ImportsFile);
        Assert.True(settings.ImportsApplied);
        Assert.True(settings.PackageApplied);
        Assert.True(settings.ServicesApplied);
        Assert.True(settings.ServicesAlreadyRegistered);
        Assert.True(settings.Prerelease);
        Assert.Equal("InteractiveServer", settings.DetectedRenderMode);
        Assert.Equal(SyncfusionBlazorToolkitLayout.AutoServer, settings.Layout);
    }
}