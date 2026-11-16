// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.IO;
using Microsoft.Build.Locator;
using Microsoft.DotNet.Scaffolding.Roslyn.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Xunit;
using ProjectEvaluation = Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers.IgniteUIBlazorProjectInspector.ProjectEvaluation;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Helpers;

public class IgniteUIBlazorProjectInspectorTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), nameof(IgniteUIBlazorProjectInspectorTests), Guid.NewGuid().ToString());


    public IgniteUIBlazorProjectInspectorTests()
    {
        if (!MSBuildLocator.IsRegistered)
        {
            MSBuildLocator.RegisterDefaults();
        }

        Directory.CreateDirectory(_directory);
    }

    [Fact]
    public void TryEvaluate_RecognizesBlazorWebAppWithoutTemplateLayout()
    {
        // Components live in a custom folder and in a shared folder outside the project directory.
        var project = WriteFile(Path.Combine("App", "App.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
              <ItemGroup><Content Include="../Shared/*.razor" /></ItemGroup>
            </Project>
            """);
        WriteFile(Path.Combine("Shared", "Banner.razor"), "<p>Banner</p>");

        var evaluation = Evaluate(project);

        Assert.True(evaluation.UsesWebSdk);
        Assert.False(evaluation.UsesBlazorWebAssemblySdk);
        Assert.True(evaluation.HasRazorComponents);
        Assert.Null(IgniteUIBlazorProjectInspector.GetUnsupportedProjectError(evaluation, hasWebAssemblyClient: false));
        Assert.False(File.Exists(Path.Combine(_directory, "App", "obj", "project.assets.json")));
    }

    [Fact]
    public void TryEvaluate_RecognizesBlazorWebAssemblyApp()
    {
        var project = WriteFile("Client.csproj", """
            <Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">
              <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
            </Project>
            """);

        var evaluation = Evaluate(project);

        Assert.True(evaluation.UsesBlazorWebAssemblySdk);
        Assert.Null(IgniteUIBlazorProjectInspector.GetUnsupportedProjectError(evaluation, hasWebAssemblyClient: false));
    }

    [Fact]
    public void TryEvaluate_CollectsItemsFromEachTargetFramework()
    {
        // A multi-targeted project gets its default items (.razor files) only in its per-framework evaluations, and
        // PackageReference items can be conditioned on one target framework.
        var project = WriteFile(Path.Combine("Library", "Library.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk.Razor">
              <PropertyGroup><TargetFrameworks>net10.0;net9.0</TargetFrameworks></PropertyGroup>
              <ItemGroup>
                <PackageReference Include="IgniteUI.Blazor" Version="25.1.0" Condition="'$(TargetFramework)' == 'net9.0'" />
              </ItemGroup>
            </Project>
            """);
        WriteFile(Path.Combine("Library", "Components", "Banner.razor"), "<p>Banner</p>");

        var evaluation = Evaluate(project);

        Assert.True(evaluation.HasRazorComponents);
        Assert.Equal("IgniteUI.Blazor", Assert.Single(evaluation.PackageReferences).EvaluatedInclude);
        Assert.Contains("references the commercial IgniteUI.Blazor package", IgniteUIBlazorProjectInspector.GetCommercialPackageConflictError(evaluation));
    }

    [Fact]
    public void TryEvaluate_RecognizesMauiBlazorHybridApp()
    {
        // The properties and items a .NET MAUI Blazor Hybrid app evaluates to, declared directly: evaluating a real
        // MAUI project needs the MAUI workload, and with the workload the MAUI SDK moves .razor files from Content to
        // RazorComponent items.
        var project = WriteFile("HybridApp.csproj", """
            <Project>
              <PropertyGroup>
                <UseMaui>true</UseMaui>
                <UsingMicrosoftNETSdkRazor>true</UsingMicrosoftNETSdkRazor>
              </PropertyGroup>
              <ItemGroup><RazorComponent Include="Components/Routes.razor" /></ItemGroup>
            </Project>
            """);

        var evaluation = Evaluate(project);

        Assert.True(evaluation.UsesMaui);
        Assert.True(evaluation.HasRazorComponents);
        Assert.True(evaluation.IsMauiBlazorHybrid);
        Assert.Null(IgniteUIBlazorProjectInspector.GetUnsupportedProjectError(evaluation, hasWebAssemblyClient: false));
    }

    [Fact]
    public void GetUnsupportedProjectError_RejectsMauiAppWithoutRazorComponents()
    {
        var evaluation = new ProjectEvaluation("MauiApp.csproj", UsesWebSdk: false, UsesRazorSdk: false, UsesBlazorWebAssemblySdk: false, UsesMaui: true, HasRazorComponents: false, PackageReferences: []);

        Assert.False(evaluation.IsMauiBlazorHybrid);
        var error = IgniteUIBlazorProjectInspector.GetUnsupportedProjectError(evaluation, hasWebAssemblyClient: false);
        Assert.Contains("is a .NET MAUI app", error);
        Assert.Contains("not a Blazor Hybrid app", error);
    }

    [Fact]
    public void GetUnsupportedProjectError_RejectsRazorPagesApp()
    {
        var project = WriteFile("Pages.csproj", """
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
            </Project>
            """);
        WriteFile(Path.Combine("Pages", "Index.cshtml"), "@page");

        var evaluation = Evaluate(project);

        Assert.True(evaluation.UsesWebSdk);
        Assert.False(evaluation.HasRazorComponents);
        var error = IgniteUIBlazorProjectInspector.GetUnsupportedProjectError(evaluation, hasWebAssemblyClient: false);
        Assert.Contains("is not a supported Blazor app", error);
        Assert.Contains("includes no Razor components", error);
        Assert.Contains(project, error);
    }

    [Fact]
    public void GetUnsupportedProjectError_AcceptsServerThatOnlyReferencesWebAssemblyClient()
    {
        var evaluation = new ProjectEvaluation("Server.csproj", UsesWebSdk: true, UsesRazorSdk: true, UsesBlazorWebAssemblySdk: false, UsesMaui: false, HasRazorComponents: false, PackageReferences: []);

        Assert.Null(IgniteUIBlazorProjectInspector.GetUnsupportedProjectError(evaluation, hasWebAssemblyClient: true));
    }

    [Theory]
    [InlineData("Microsoft.NET.Sdk.Razor", "Razor class library")]
    [InlineData("Microsoft.NET.Sdk", "neither the ASP.NET Core Web SDK, the Blazor WebAssembly SDK nor .NET MAUI")]
    public void GetUnsupportedProjectError_RejectsNonWebProjects(string sdk, string expectedReason)
    {
        var project = WriteFile("Library.csproj", $"""
            <Project Sdk="{sdk}">
              <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
            </Project>
            """);
        WriteFile("Component.razor", "<p>Component</p>");

        var error = IgniteUIBlazorProjectInspector.GetUnsupportedProjectError(Evaluate(project), hasWebAssemblyClient: false);

        Assert.Contains("is not a supported Blazor app", error);
        Assert.Contains(expectedReason, error);
    }

    [Fact]
    public void GetCommercialPackageConflictError_FindsImportedConditionalReference()
    {
        WriteFile("Directory.Build.props", """
            <Project>
              <ItemGroup>
                <PackageReference Include="igniteui.blazor" Version="25.1.0" Condition="'$(Configuration)' == 'Debug'" />
              </ItemGroup>
            </Project>
            """);
        var project = WriteFile("App.csproj", """
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
            </Project>
            """);

        var error = IgniteUIBlazorProjectInspector.GetCommercialPackageConflictError(Evaluate(project));

        Assert.Contains("references the commercial igniteui.blazor package", error);
        Assert.Contains($"declared in '{Path.Combine(_directory, "Directory.Build.props")}'", error);
        Assert.Contains("remove the igniteui.blazor PackageReference", error);
    }

    [Theory]
    [InlineData("IgniteUI.Blazor", true)]
    [InlineData("IgniteUI.Blazor.Trial", true)]
    [InlineData("IgniteUI.Blazor.Lite", false)]
    [InlineData("IgniteUI.Blazor.GridLite", false)]
    [InlineData("IgniteUI.Blazor.Documents.Excel", false)]
    public void GetCommercialPackageConflictError_OnlyFlagsCommercialPackages(string packageId, bool expectConflict)
    {
        var project = Path.Combine(_directory, "App.csproj");
        var evaluation = new ProjectEvaluation(project, UsesWebSdk: true, UsesRazorSdk: true, UsesBlazorWebAssemblySdk: false, UsesMaui: false, HasRazorComponents: true,
            PackageReferences: [new EvaluatedProjectItem("PackageReference", packageId, project)]);

        var error = IgniteUIBlazorProjectInspector.GetCommercialPackageConflictError(evaluation);

        Assert.Equal(expectConflict, error is not null);
        if (error is not null)
        {
            Assert.DoesNotContain("declared in", error);
        }
    }

    [Fact]
    public void TryEvaluate_ReportsEvaluationFailure()
    {
        var project = WriteFile("Broken.csproj", """<Project><Import Project="Missing.props" /></Project>""");

        Assert.False(IgniteUIBlazorProjectInspector.TryEvaluate(project, out var evaluation, out var error));
        Assert.Null(evaluation);
        Assert.Contains("Unable to evaluate", error);
        Assert.Contains("Missing.props", error);
    }

    private static ProjectEvaluation Evaluate(string projectPath)
    {
        Assert.True(IgniteUIBlazorProjectInspector.TryEvaluate(projectPath, out var evaluation, out var error), error);
        return evaluation!;
    }

    private string WriteFile(string relativePath, string contents)
    {
        var path = Path.Combine(_directory, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, contents);
        return path;
    }

    public void Dispose() => Directory.Delete(_directory, recursive: true);
}
