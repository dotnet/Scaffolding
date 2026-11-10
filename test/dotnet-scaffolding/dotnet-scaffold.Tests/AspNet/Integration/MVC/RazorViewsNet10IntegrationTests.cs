// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Core.ComponentModel;
using Microsoft.DotNet.Scaffolding.Roslyn.Services;
using Microsoft.DotNet.Tools.Scaffold.Interactive.Flow.Steps;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.MVC;

public class RazorViewsNet10IntegrationTests : RazorViewsIntegrationTestsBase
{
    protected override string TargetFramework => "net10.0";
    protected override string TestClassName => nameof(RazorViewsNet10IntegrationTests);

    [Theory]
    [InlineData("TestProject.Models.Book", "TestProject.Models.Book")]
    [InlineData("BookStore.Models.Book", "BookStore.Models.Book")]
    [InlineData("global::BookStore.Models.Book", "BookStore.Models.Book")]
    [InlineData("BookStore.Models.Catalog.Book", "BookStore.Models.Catalog.Book")]
    public async Task Scaffold_Views_ResolvesQualifiedModelWithDuplicateShortName(string model, string modelFullName)
    {
        var modelsProject = CreateBookModelProject();
        File.AppendAllText(Path.Combine(Path.GetDirectoryName(modelsProject)!, "Book.cs"), """

            public class Catalog
            {
                public class Book
                {
                    public int Id { get; set; }
                    public string Title { get; set; } = string.Empty;
                }
            }
            """);
        File.WriteAllText(Path.Combine(_testProjectDir, "Book.cs"), """
            namespace TestProject.Models;
            public class Book
            {
                public int Id { get; set; }
                public string LocalTitle { get; set; } = string.Empty;
            }
            """);
        await PrepareReferencedBookAppAsync(modelsProject);

        await ScaffoldBookIndexAndVerifyBuildAsync(model, modelFullName);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Scaffold_Views_RejectsAmbiguousShortModelNameWithoutModifyingProject(bool globalNamespace)
    {
        var modelsProject = CreateBookModelProject();
        var localModel = ScaffoldCliHelper.GetModelClassContent("TestProject", "Book");
        if (globalNamespace)
        {
            localModel = "public class Book { public int Id { get; set; } public string Title { get; set; } = string.Empty; }";
        }
        File.WriteAllText(Path.Combine(_testProjectDir, "Book.cs"), localModel);
        await PrepareReferencedBookAppAsync(modelsProject);
        var originalProject = File.ReadAllText(_testProjectPath);
        var originalProgram = File.ReadAllText(Path.Combine(_testProjectDir, "Program.cs"));

        var (exitCode, output, error) = await ScaffoldCliHelper.RunScaffoldAsync(
            TargetFramework, "views", "--project", _testProjectPath, "--model", "Book", "--page", "Index");

        Assert.Equal(1, exitCode);
        Assert.Contains("Ambiguous --model 'Book'", output + error);
        Assert.Contains(globalNamespace ? "global::Book" : "TestProject.Models.Book", output + error);
        Assert.Contains("BookStore.Models.Book", output + error);
        Assert.False(Directory.Exists(Path.Combine(_testProjectDir, "Views")));
        Assert.Equal(originalProject, File.ReadAllText(_testProjectPath));
        Assert.Equal(originalProgram, File.ReadAllText(Path.Combine(_testProjectDir, "Program.cs")));
    }

    [Theory]
    [InlineData(false, "Book")]
    [InlineData(false, "BookStore.Models.Book")]
    [InlineData(true, "Book")]
    [InlineData(true, "BookStore.Models.Book")]
    public async Task Scaffold_Views_RespectsReferencedInternalModelAccessibility(bool friendAssembly, string model)
    {
        var modelsProject = CreateBookModelProject();
        var modelPath = Path.Combine(Path.GetDirectoryName(modelsProject)!, "Book.cs");
        var modelSource = File.ReadAllText(modelPath).Replace("public class Book", "internal class Book");
        if (friendAssembly)
        {
            modelSource = "[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(\"TestProject\")]\n" + modelSource;
        }
        File.WriteAllText(modelPath, modelSource);
        await PrepareReferencedBookAppAsync(modelsProject);
        using var service = new CodeService(NullLogger.Instance, _testProjectPath);
        var options = await ParameterDiscovery.GetClassDisplayNamesAsync(service, new CommandInfo
        {
            Name = "views",
            DisplayName = "Views",
            DisplayCategories = [],
            Parameters = []
        });
        Assert.Equal(friendAssembly, options.Exists(option => option.Value == "global::BookStore.Models.Book"));

        if (friendAssembly)
        {
            await ScaffoldBookIndexAndVerifyBuildAsync(model);
        }
        else
        {
            var originalProject = File.ReadAllText(_testProjectPath);
            var originalProgram = File.ReadAllText(Path.Combine(_testProjectDir, "Program.cs"));

            var (exitCode, output, error) = await ScaffoldCliHelper.RunScaffoldAsync(
                TargetFramework, "views", "--project", _testProjectPath, "--model", model, "--page", "Index");

            Assert.Equal(1, exitCode);
            Assert.Contains($"Invalid --model '{model}'", output + error);
            Assert.False(Directory.Exists(Path.Combine(_testProjectDir, "Views")));
            Assert.Equal(originalProject, File.ReadAllText(_testProjectPath));
            Assert.Equal(originalProgram, File.ReadAllText(Path.Combine(_testProjectDir, "Program.cs")));
        }
    }

    private async Task PrepareReferencedBookAppAsync(string modelsProject)
    {
        File.WriteAllText(_testProjectPath, $"""
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <PropertyGroup>
                <TargetFramework>{TargetFramework}</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
              </PropertyGroup>
              <ItemGroup>
                <ProjectReference Include="{Path.GetRelativePath(_testProjectDir, modelsProject)}" />
              </ItemGroup>
            </Project>
            """);
        File.WriteAllText(Path.Combine(_testProjectDir, "Program.cs"), ScaffoldCliHelper.GetMinimalProgramCs());
        var build = await RunBuildAsync(_testProjectDir);
        Assert.True(build.ExitCode == 0, $"Sample should build before scaffolding.\n{build.Output}\n{build.Error}");
    }

    [Fact]
    public async Task Scaffold_Views_WithFrameworkConditionalTransitiveModelReference()
    {
        var modelsProject = CreateBookModelProject();
        var dataDirectory = Path.Combine(_testDirectory, "BookStore Data");
        Directory.CreateDirectory(dataDirectory);
        var dataProject = Path.Combine(dataDirectory, "BookStore.Data.csproj");
        File.WriteAllText(dataProject, $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFrameworks>net8.0;{TargetFramework}</TargetFrameworks>
              </PropertyGroup>
              <ItemGroup Condition="'$(TargetFramework)' == '{TargetFramework}'">
                <ProjectReference Include="{Path.GetRelativePath(dataDirectory, modelsProject)}" />
              </ItemGroup>
            </Project>
            """);
        File.WriteAllText(Path.Combine(dataDirectory, "Store.cs"), "public class Store { }");
        File.WriteAllText(_testProjectPath, $"""
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <PropertyGroup>
                <TargetFramework>{TargetFramework}</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
              </PropertyGroup>
              <ItemGroup>
                <ProjectReference Include="{Path.GetRelativePath(_testProjectDir, dataProject)}" />
              </ItemGroup>
            </Project>
            """);
        File.WriteAllText(Path.Combine(_testProjectDir, "Program.cs"), ScaffoldCliHelper.GetMinimalProgramCs());
        var build = await RunBuildAsync(_testProjectDir);
        Assert.True(build.ExitCode == 0, $"Sample should build before scaffolding.\n{build.Output}\n{build.Error}");

        await ScaffoldBookIndexAndVerifyBuildAsync();
    }

    [Fact]
    public async Task Scaffold_Views_RejectsModelInBuildOnlyReferenceWithoutModifyingProject()
    {
        var modelsProject = CreateBookModelProject();
        File.WriteAllText(_testProjectPath, $"""
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <PropertyGroup>
                <TargetFramework>{TargetFramework}</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
              </PropertyGroup>
              <ItemGroup>
                <ProjectReference Include="{Path.GetRelativePath(_testProjectDir, modelsProject)}" ReferenceOutputAssembly="false" />
              </ItemGroup>
            </Project>
            """);
        File.WriteAllText(Path.Combine(_testProjectDir, "Program.cs"), ScaffoldCliHelper.GetMinimalProgramCs());
        var build = await RunBuildAsync(_testProjectDir);
        Assert.True(build.ExitCode == 0, $"Sample should build before scaffolding.\n{build.Output}\n{build.Error}");

        var (exitCode, output, error) = await ScaffoldCliHelper.RunScaffoldAsync(
            TargetFramework, "views", "--project", _testProjectPath, "--model", "Book", "--page", "Index");

        Assert.Equal(1, exitCode);
        Assert.Contains("Invalid --model 'Book'", output + error);
        Assert.False(Directory.Exists(Path.Combine(_testProjectDir, "Views")));
        build = await RunBuildAsync(_testProjectDir);
        Assert.True(build.ExitCode == 0, $"Rejected scaffolding must leave a buildable project.\n{build.Output}\n{build.Error}");
    }

    [Fact]
    public async Task Scaffold_Views_Net10_CliInvocation()
    {
        File.WriteAllText(_testProjectPath, ProjectContent);
        File.WriteAllText(Path.Combine(_testProjectDir, "Program.cs"), ScaffoldCliHelper.GetMinimalProgramCs());
        var modelsDir = Path.Combine(_testProjectDir, "Models");
        Directory.CreateDirectory(modelsDir);
        File.WriteAllText(Path.Combine(modelsDir, "TestModel.cs"), ScaffoldCliHelper.GetModelClassContent("TestProject", "TestModel"));

        var (preExitCode, preOutput, preError) = await RunBuildAsync(_testProjectDir);
        Assert.True(preExitCode == 0,
            $"Project should build before scaffolding.\nExit code: {preExitCode}\nOutput: {preOutput}\nError: {preError}");

        var (cliExitCode, cliOutput, cliError) = await ScaffoldCliHelper.RunScaffoldAsync(
            TargetFramework,
            "views",
            "--project", _testProjectPath,
            "--model", "TestModel",
            "--page", "CRUD");
        Assert.True(cliExitCode == 0, $"CLI scaffold should succeed.\nOutput: {cliOutput}\nError: {cliError}");

        // Assert — expected files were created
        var viewsDir = Path.Combine(_testProjectDir, "Views", "TestModel");
        Assert.True(Directory.Exists(viewsDir), "Views/TestModel directory should be created.");
        foreach (var view in new[] { "Create.cshtml", "Delete.cshtml", "Details.cshtml", "Edit.cshtml", "Index.cshtml" })
        {
            Assert.True(File.Exists(Path.Combine(viewsDir, view)), $"View '{view}' should be created.");
        }
        Assert.True(File.Exists(Path.Combine(_testProjectDir, "Views", "Shared", "_ValidationScriptsPartial.cshtml")),
            "_ValidationScriptsPartial.cshtml should be created.");

        // Assert — no NuGet errors and project builds after scaffolding
        Assert.False(cliOutput.Contains("error: NU"),
            $"Scaffolding should not produce NuGet errors for {TargetFramework}.\nOutput: {cliOutput}");
        var (postExitCode, postOutput, postError) = await RunBuildAsync(_testProjectDir);
        Assert.True(postExitCode == 0,
            $"Project should build after scaffolding.\nExit code: {postExitCode}\nOutput: {postOutput}\nError: {postError}");
    }
}
