// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.Identity;

[Trait("Suite", "ScaffoldIntegration")]
[Trait("Family", "blazor-identity")]
public class IdentityValidationIntegrationTests
{
    private const string ProgramContent = """
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddRazorComponents().AddInteractiveWebAssemblyComponents();
        builder.Build().Run();
        """;

    [Theory]
    [InlineData("blazor-identity", 0, false, "No referenced project using the Microsoft.NET.Sdk.BlazorWebAssembly SDK was found.")]
    [InlineData("blazor-identity", 2, false, "Multiple referenced projects use the Microsoft.NET.Sdk.BlazorWebAssembly SDK")]
    [InlineData("identity", 0, true, "Unable to determine a supported target framework")]
    public async Task Scaffold_Identity_ProjectDiscoveryFailureDoesNotMutateProject(
        string scaffolder, int clientCount, bool missingImport, string expectedDiagnostic)
    {
        using var project = new BlazorTestProject("net11.0");
        var references = string.Empty;
        for (var index = 0; index < clientCount; index++)
        {
            var clientDirectory = Path.Combine(project.DirectoryPath, $"Client{index}");
            Directory.CreateDirectory(clientDirectory);
            File.WriteAllText(Path.Combine(clientDirectory, $"Client{index}.csproj"), """
                <Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">
                  <PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup>
                </Project>
                """);
            references += $"""<ProjectReference Include="..\Client{index}\Client{index}.csproj" />""";
        }

        var import = missingImport ? """<Import Project="MissingClientReferences.props" />""" : string.Empty;
        var projectContent = File.ReadAllText(project.ProjectPath).Replace("</Project>", $"""
            <ItemGroup>
              {references}
              <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly.Server" Version="11.0.*-*" />
            </ItemGroup>
            {import}
            </Project>
            """);
        File.WriteAllText(project.ProjectPath, projectContent);
        File.WriteAllText(Path.Combine(project.ProjectDirectory, "Program.cs"), ProgramContent);

        var (_, output, error) = await ScaffoldCliHelper.RunScaffoldAsync(
            "net11.0",
            scaffolder,
            "--project", project.ProjectPath,
            "--dataContext", "TestDbContext",
            "--dbProvider", "sqlite-efcore",
            "--prerelease");

        Assert.Contains(expectedDiagnostic, output + error);
        AssertProjectUnchanged(project, projectContent);
    }

    [Theory]
    [InlineData("net11.0", true, "Unable to restore", "Test restore failure")]
    [InlineData("net11.0", false, "Unable to resolve Blazor registration", "AddInteractiveWebAssemblyComponents")]
    [InlineData("net8.0", false, "Unable to resolve Blazor registration", "AddInteractiveWebAssemblyComponents")]
    public async Task Scaffold_BlazorIdentity_AnalysisFailureDoesNotMutateProject(
        string targetFramework, bool failRestore, string expectedDiagnostic, string expectedDetail)
    {
        using var project = new BlazorTestProject(targetFramework);
        var projectContent = File.ReadAllText(project.ProjectPath);
        if (failRestore)
        {
            projectContent = projectContent.Replace("</Project>", """
                <Target Name="FailRestore" BeforeTargets="Restore">
                  <Error Text="Test restore failure" />
                </Target>
                </Project>
                """);
        }
        File.WriteAllText(project.ProjectPath, projectContent);
        File.WriteAllText(Path.Combine(project.ProjectDirectory, "Program.cs"), ProgramContent);

        var (_, output, error) = await ScaffoldCliHelper.RunScaffoldAsync(
            targetFramework,
            "blazor-identity",
            "--project", project.ProjectPath,
            "--dataContext", "TestDbContext",
            "--dbProvider", "sqlite-efcore",
            "--prerelease");

        Assert.Contains(expectedDiagnostic, output + error);
        Assert.Contains(expectedDetail, output + error);
        AssertProjectUnchanged(project, projectContent);
    }

    private static void AssertProjectUnchanged(BlazorTestProject project, string projectContent)
    {
        Assert.Equal(projectContent, File.ReadAllText(project.ProjectPath));
        Assert.Equal(ProgramContent, File.ReadAllText(Path.Combine(project.ProjectDirectory, "Program.cs")));
        Assert.False(Directory.Exists(Path.Combine(project.ProjectDirectory, "Data")));
        Assert.False(Directory.Exists(Path.Combine(project.ProjectDirectory, "Areas", "Identity")));
        Assert.False(Directory.Exists(Path.Combine(project.ProjectDirectory, "Components", "Account")));
    }
}
