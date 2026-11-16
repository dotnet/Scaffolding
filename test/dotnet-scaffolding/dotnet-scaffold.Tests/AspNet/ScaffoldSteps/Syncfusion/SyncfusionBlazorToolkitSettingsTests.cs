// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers.Syncfusion;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Syncfusion;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Syncfusion.Settings;
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
        Assert.False(settings.ServicesAlreadyRegistered);
        Assert.False(settings.PackageApplied);
        Assert.False(settings.ServicesApplied);
        Assert.False(settings.ImportsApplied);
        Assert.Null(settings.ClientProjectPath);
        Assert.Equal(BlazorAppKind.NotBlazor, settings.AppKind);
        Assert.Equal(BlazorHostingModes.None, settings.Hosting.Supported);
        Assert.Equal("None", settings.Hosting.GlobalRenderMode);
        Assert.False(settings.Server.AnyChange);
        Assert.False(settings.Client.AnyChange);
    }

    [Fact]
    public void CanSetAllProperties()
    {
        var settings = new SyncfusionBlazorToolkitSettings
        {
            Project = "C:/absolute/Test.csproj",
            ImportsFile = Path.Combine("Components", "_Imports.razor"),
            ImportsFileSkipped = false,
            ImportsApplied = true,
            ServicesAlreadyRegistered = true,
            PackageApplied = true,
            ServicesApplied = true,
            ClientProjectPath = "C:/absolute/Test.Client.csproj",
            AppKind = BlazorAppKind.WebApp,
            Hosting = new BlazorHostingFacts(BlazorHostingModes.Server, BlazorHostingModes.Server, "InteractiveServer"),
            Layout = SyncfusionBlazorToolkitLayout.AutoServer,
        };

        Assert.Equal(Path.Combine("Components", "_Imports.razor"), settings.ImportsFile);
        Assert.True(settings.ImportsApplied);
        Assert.True(settings.PackageApplied);
        Assert.True(settings.ServicesApplied);
        Assert.True(settings.ServicesAlreadyRegistered);
        Assert.Equal(BlazorAppKind.WebApp, settings.AppKind);
        Assert.Equal("InteractiveServer", settings.Hosting.GlobalRenderMode);
        Assert.Equal(SyncfusionBlazorToolkitLayout.AutoServer, settings.Layout);
    }

    [Theory]
    [InlineData(null, "2.0.0", true)]
    [InlineData("1.0.2", "2.0.0", true)]
    [InlineData("2.0.0", "2.0.0", false)]
    [InlineData("2.0.0", null, true)]
    public void PackageChanged_ReflectsBeforeAndAfter(string? before, string? after, bool changed)
    {
        var changes = new ProjectChanges { PackageVersionBefore = before, PackageVersionAfter = after };

        Assert.Equal(changed, changes.PackageChanged);
        Assert.Equal(changed, changes.AnyChange);
    }

    [Fact]
    public void AnyChange_IsTrueForEachKindOfChange()
    {
        Assert.True(new ProjectChanges { ServicesInserted = true }.AnyChange);
        Assert.True(new ProjectChanges { ImportsInserted = true }.AnyChange);
        Assert.False(new ProjectChanges().AnyChange);
    }
}
