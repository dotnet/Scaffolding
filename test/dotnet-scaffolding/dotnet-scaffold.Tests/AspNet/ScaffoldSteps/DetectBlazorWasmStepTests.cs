// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.ScaffoldSteps;

public class DetectBlazorWasmStepTests
{
    private readonly ScaffolderContext _context;

    public DetectBlazorWasmStepTests()
    {
        var scaffolder = new Mock<IScaffolder>();
        scaffolder.Setup(s => s.DisplayName).Returns("TestScaffolder");
        scaffolder.Setup(s => s.Name).Returns("test-scaffolder");
        _context = new ScaffolderContext(scaffolder.Object);
    }

    [Fact]
    public async Task ExecuteAsync_DetectsStandaloneWebAssemblyProject()
    {
        string projectPath = Path.GetFullPath("StandaloneClient.csproj");
        string projectArgument = Path.GetRelativePath(Environment.CurrentDirectory, projectPath);
        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(fs => fs.FileExists(projectPath)).Returns(true);
        fileSystem.Setup(fs => fs.ReadAllText(projectPath)).Returns(
            """
            <Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">
              <PropertyGroup>
                <TargetFramework>net11.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);
        var step = CreateStep(fileSystem.Object, projectArgument);

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.True(result);
        Assert.Equal(true, _context.Properties["IsBlazorWasmProject"]);
        Assert.Equal(projectPath, _context.Properties["BlazorWasmClientProjectPath"]);
    }

    [Fact]
    public async Task ExecuteAsync_DetectsReferencedWebAssemblyClientProject()
    {
        string serverProjectPath = Path.GetFullPath(Path.Combine("AutoApp", "AutoApp.csproj"));
        string clientProjectPath = Path.GetFullPath(
            Path.Combine("AutoApp", "AutoApp.Client", "AutoApp.Client.csproj"));
        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(fs => fs.FileExists(serverProjectPath)).Returns(true);
        fileSystem.Setup(fs => fs.ReadAllText(serverProjectPath)).Returns(
            """
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <ItemGroup>
                <ProjectReference Include="AutoApp.Client\AutoApp.Client.csproj" />
              </ItemGroup>
            </Project>
            """);
        fileSystem.Setup(fs => fs.FileExists(clientProjectPath)).Returns(true);
        fileSystem.Setup(fs => fs.ReadAllText(clientProjectPath)).Returns(
            """
            <Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">
              <PropertyGroup>
                <TargetFramework>net11.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);
        var step = CreateStep(fileSystem.Object, serverProjectPath);

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.True(result);
        Assert.Equal(true, _context.Properties["IsBlazorWasmProject"]);
        Assert.Equal(clientProjectPath, _context.Properties["BlazorWasmClientProjectPath"]);
    }

    private static DetectBlazorWasmStep CreateStep(IFileSystem fileSystem, string projectPath)
    {
        return new DetectBlazorWasmStep(
            Mock.Of<ILogger<DetectBlazorWasmStep>>(),
            fileSystem,
            Mock.Of<ITelemetryService>())
        {
            ProjectPath = projectPath
        };
    }
}
