// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.DotNet.Scaffolding.Roslyn.Services;
using Xunit;

namespace Microsoft.DotNet.Scaffolding.Roslyn.Tests;

public class MSBuildProjectServiceTests : IDisposable
{
    private readonly string _directory = Path.Combine(AppContext.BaseDirectory, nameof(MSBuildProjectServiceTests), Guid.NewGuid().ToString());
    private readonly string _projectPath;

    public MSBuildProjectServiceTests()
    {
        Directory.CreateDirectory(_directory);
        _projectPath = Path.Combine(_directory, "Server.csproj");
    }

    [Fact]
    public void TryGetProjectReferences_EvaluatesImportedPropertiesAndConditions()
    {
        Directory.CreateDirectory(Path.Combine(_directory, "Imports"));
        File.WriteAllText(Path.Combine(_directory, "Imports", "References.props"), """
            <Project>
              <ItemGroup>
                <ProjectReference Include="$(ClientDirectory)/Client.csproj" Condition="'$(IncludeClient)' == 'true'" />
                <ProjectReference Include="Excluded.csproj" Condition="'$(IncludeClient)' != 'true'" />
              </ItemGroup>
            </Project>
            """);
        File.WriteAllText(_projectPath, """
            <Project>
              <PropertyGroup>
                <ClientDirectory>../RelocatedClient</ClientDirectory>
                <IncludeClient>true</IncludeClient>
              </PropertyGroup>
              <Import Project="Imports/References.props" />
            </Project>
            """);

        var service = new MSBuildProjectService(_projectPath);

        Assert.True(service.TryGetProjectReferences(out var references, out var error), error);
        Assert.Null(error);
        Assert.Equal(Path.GetFullPath(Path.Combine(_directory, "..", "RelocatedClient", "Client.csproj")), Assert.Single(references));
        Assert.True(
            service.TryGetEvaluatedProperties(["ClientDirectory", "IncludeClient"], out var properties, out error),
            error);
        Assert.Equal("../RelocatedClient", properties["ClientDirectory"]);
        Assert.Equal("true", properties["IncludeClient"]);
    }

    [Theory]
    [InlineData("<Project><Import Project=\"Missing.props\" /></Project>", "Missing.props")]
    [InlineData("<Project Sdk=\"Scaffolding.Missing.Sdk\" />", "Scaffolding.Missing.Sdk")]
    public void TryGetProjectReferences_ReportsEvaluationFailure(string projectContent, string expectedDiagnostic)
    {
        File.WriteAllText(_projectPath, projectContent);
        var service = new MSBuildProjectService(_projectPath);

        // A prior permissive evaluation must not hide a required import or SDK failure.
        service.GetProjectCapabilities();

        Assert.False(service.TryGetProjectReferences(out var references, out var error));
        Assert.Empty(references);
        Assert.Contains(expectedDiagnostic, error);
        Assert.False(service.TryGetEvaluatedProperties(["RootNamespace"], out var properties, out error));
        Assert.Empty(properties);
        Assert.Contains(expectedDiagnostic, error);

        File.WriteAllText(_projectPath, "<Project />");
        Assert.True(service.TryGetProjectReferences(out references, out error), error);
        Assert.Empty(references);
        Assert.Null(error);
    }

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
    }
}
