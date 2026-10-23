// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.Build.Locator;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers.Syncfusion;
using Xunit;

namespace Microsoft.DotNet.Scaffold.Tests.AspNet.Helpers.Syncfusion;

public class BlazorProjectClassifierTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "sfbt-classify-" + Guid.NewGuid().ToString("N"));

    public BlazorProjectClassifierTests()
    {
        if (!MSBuildLocator.IsRegistered)
        {
            MSBuildLocator.RegisterDefaults();
        }

        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }

        GC.SuppressFinalize(this);
    }

    private string Project(string xml, params (string Name, string Content)[] files)
    {
        string path = Path.Combine(_dir, "App.csproj");
        File.WriteAllText(path, xml);
        foreach (var (name, content) in files)
        {
            string full = Path.Combine(_dir, name);
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            File.WriteAllText(full, content);
        }

        return path;
    }

    private static string Sdk(string sdk, string extra = "") =>
        $"<Project Sdk=\"{sdk}\"><PropertyGroup><TargetFramework>net10.0</TargetFramework>{extra}</PropertyGroup></Project>";

    [Fact]
    public void BlazorWebApp_WithRazorComponents_IsWebApp()
    {
        var info = BlazorProjectClassifier.Classify(Project(Sdk("Microsoft.NET.Sdk.Web"), ("Components/Home.razor", "<h1/>")));

        Assert.Equal(BlazorAppKind.WebApp, info.Kind);
        Assert.Null(info.Error);
    }

    [Fact]
    public void PlainWebApi_UsingTheWebSdk_IsNotBlazor()
    {
        // Same SDK as a Blazor Web App, but no Razor components: the SDK name alone proves nothing.
        var info = BlazorProjectClassifier.Classify(Project(Sdk("Microsoft.NET.Sdk.Web"), ("Program.cs", "var b = 1;")));

        Assert.Equal(BlazorAppKind.NotBlazor, info.Kind);
        Assert.Null(info.Error);
    }

    [Fact]
    public void BlazorWebApp_WithComponentsOnlyInAReferencedLibrary_IsWebApp()
    {
        var info = BlazorProjectClassifier.Classify(Project(
            Sdk("Microsoft.NET.Sdk.Web"),
            ("Program.cs", "var app = builder.Build();\napp.MapRazorComponents<Lib.App>();\n")));

        Assert.Equal(BlazorAppKind.WebApp, info.Kind);
    }

    [Fact]
    public void StandaloneWasm_IsStandaloneWasm()
    {
        var info = BlazorProjectClassifier.Classify(Project(Sdk("Microsoft.NET.Sdk.BlazorWebAssembly")));

        Assert.Equal(BlazorAppKind.StandaloneWasm, info.Kind);
    }

    [Fact]
    public void SdkNameInsideAComment_DoesNotClassifyAProject()
    {
        string xml =
            "<Project Sdk=\"Microsoft.NET.Sdk\"><!-- Microsoft.NET.Sdk.Web Microsoft.NET.Sdk.BlazorWebAssembly -->" +
            "<PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>";

        Assert.Equal(BlazorAppKind.NotBlazor, BlazorProjectClassifier.Classify(Project(xml)).Kind);
    }

    [Fact]
    public void UseMauiFalse_IsNotHybrid()
    {
        var info = BlazorProjectClassifier.Classify(
            Project(Sdk("Microsoft.NET.Sdk.Web", "<UseMaui>false</UseMaui>"), ("Components/Home.razor", "<h1/>")));

        Assert.Equal(BlazorAppKind.WebApp, info.Kind);
    }

    [Fact]
    public void UseMauiTrue_IsHybrid()
    {
        var info = BlazorProjectClassifier.Classify(
            Project(Sdk("Microsoft.NET.Sdk.Web", "<UseMaui>true</UseMaui>"), ("Components/Home.razor", "<h1/>")));

        Assert.Equal(BlazorAppKind.Hybrid, info.Kind);
    }

    [Fact]
    public void ImportedSdkConfiguration_IsRespected()
    {
        // The Blazor WebAssembly SDK is selected by an imported file, not the .csproj text.
        File.WriteAllText(Path.Combine(_dir, "Directory.Build.props"), "<Project><PropertyGroup><UseMaui>true</UseMaui></PropertyGroup></Project>");
        var info = BlazorProjectClassifier.Classify(Project(Sdk("Microsoft.NET.Sdk.Web"), ("Components/Home.razor", "<h1/>")));

        Assert.Equal(BlazorAppKind.Hybrid, info.Kind);
    }

    [Fact]
    public void ClassLibrary_IsNotBlazor()
    {
        Assert.Equal(BlazorAppKind.NotBlazor, BlazorProjectClassifier.Classify(Project(Sdk("Microsoft.NET.Sdk"))).Kind);
    }

    [Fact]
    public void UnreadableOrMissingProject_FailsClosedWithADiagnostic()
    {
        var missing = BlazorProjectClassifier.Classify(Path.Combine(_dir, "Nope.csproj"));
        Assert.Equal(BlazorAppKind.NotBlazor, missing.Kind);
        Assert.NotNull(missing.Error);

        var broken = BlazorProjectClassifier.Classify(Project("<Project Sdk=\"Microsoft.NET.Sdk\"><Import Project=\"missing.props\" /></Project>"));
        Assert.Equal(BlazorAppKind.NotBlazor, broken.Kind);
        Assert.NotNull(broken.Error);
    }
}

public class BlazorHostingInspectorTests
{
    private const string ServerOnly =
        "var builder = WebApplication.CreateBuilder(args);\n" +
        "builder.Services.AddRazorComponents().AddInteractiveServerComponents();\n" +
        "var app = builder.Build();\n" +
        "app.MapRazorComponents<App>().AddInteractiveServerRenderMode();\n";

    [Fact]
    public void ServerHosting_IsDetectedFromServicesAndEndpoint()
    {
        var facts = BlazorHostingInspector.Inspect(ServerOnly, appRazorText: null);

        Assert.Equal(BlazorHostingModes.Server, facts.Supported);
        Assert.Equal("None", facts.GlobalRenderMode);
    }

    [Fact]
    public void HostingCallsInUncalledHelpersAndLambdas_AreNotCounted()
    {
        string text = ServerOnly +
            "void Unused(WebApplicationBuilder b, WebApplication a)\n{\n" +
            "    b.Services.AddRazorComponents().AddInteractiveWebAssemblyComponents();\n" +
            "    a.MapRazorComponents<App>().AddInteractiveWebAssemblyRenderMode();\n}\n" +
            "Action lambda = () => app.MapRazorComponents<App>().AddInteractiveWebAssemblyRenderMode();\n";

        Assert.Equal(BlazorHostingModes.Server, BlazorHostingInspector.Inspect(text, null).Supported);
    }

    [Fact]
    public void ServerAndWasm_BothMustBeRegisteredForAuto()
    {
        string both = ServerOnly.Replace("AddInteractiveServerComponents()", "AddInteractiveServerComponents().AddInteractiveWebAssemblyComponents()")
            .Replace("AddInteractiveServerRenderMode()", "AddInteractiveServerRenderMode().AddInteractiveWebAssemblyRenderMode()");

        Assert.Equal(
            BlazorHostingModes.Server | BlazorHostingModes.WebAssembly,
            BlazorHostingInspector.Inspect(both, null).Supported);
    }

    [Fact]
    public void ServicesWithoutEndpoint_AreNotSupported()
    {
        string servicesOnly =
            "var builder = WebApplication.CreateBuilder(args);\n" +
            "builder.Services.AddRazorComponents().AddInteractiveServerComponents();\n" +
            "var app = builder.Build();\napp.MapRazorComponents<App>();\n";

        var facts = BlazorHostingInspector.Inspect(servicesOnly, null);

        Assert.Equal(BlazorHostingModes.Server, facts.Registered);
        Assert.Equal(BlazorHostingModes.None, facts.Supported);
    }

    [Fact]
    public void StaticSsr_HasNoSupportedHosting()
    {
        string ssr =
            "var builder = WebApplication.CreateBuilder(args);\nbuilder.Services.AddRazorComponents();\n" +
            "var app = builder.Build();\napp.MapRazorComponents<App>();\n";

        Assert.Equal(BlazorHostingModes.None, BlazorHostingInspector.Inspect(ssr, null).Supported);
    }

    [Theory]
    [InlineData("<Routes @rendermode=\"InteractiveServer\" />", "InteractiveServer")]
    [InlineData("<Routes @rendermode=\"InteractiveAuto\" />", "InteractiveAuto")]
    [InlineData("<Routes @rendermode=\"InteractiveWebAssembly\" />", "InteractiveWebAssembly")]
    [InlineData("<HeadOutlet @rendermode=\"@RenderMode.InteractiveServer\" />", "InteractiveServer")]
    [InlineData("<Routes />", "None")]
    public void GlobalRenderMode_IsReadFromAppRazor(string appRazor, string expected)
    {
        Assert.Equal(expected, BlazorHostingInspector.Inspect(null, appRazor).GlobalRenderMode);
    }

    [Fact]
    public void GlobalRenderModeNone_DoesNotImplyNoInteractivity()
    {
        var facts = BlazorHostingInspector.Inspect(ServerOnly, "<Routes />");

        Assert.Equal("None", facts.GlobalRenderMode);
        Assert.NotEqual(BlazorHostingModes.None, facts.Supported);
    }

    [Fact]
    public void EmptyInputs_AreUnknown()
    {
        var facts = BlazorHostingInspector.Inspect(null, null);

        Assert.Equal(BlazorHostingModes.None, facts.Supported);
        Assert.Equal("None", facts.GlobalRenderMode);
    }
}
