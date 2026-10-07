// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Locator;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.ScaffoldSteps;

public class DetectBlazorWasmStepTests : IDisposable
{
    private readonly ScaffolderContext _context;
    private readonly string _testDirectory = Path.Combine(
        Path.GetTempPath(),
        nameof(DetectBlazorWasmStepTests),
        Guid.NewGuid().ToString());

    public DetectBlazorWasmStepTests()
    {
        if (!MSBuildLocator.IsRegistered)
        {
            MSBuildLocator.RegisterDefaults();
        }

        Directory.CreateDirectory(_testDirectory);
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
        string serverDirectory = Path.Combine(_testDirectory, "AutoApp");
        string clientDirectory = Path.Combine(_testDirectory, "AutoApp.Client");
        Directory.CreateDirectory(serverDirectory);
        Directory.CreateDirectory(clientDirectory);
        string serverProjectPath = Path.Combine(serverDirectory, "AutoApp.csproj");
        string clientProjectPath = Path.Combine(clientDirectory, "AutoApp.Client.csproj");
        File.WriteAllText(
            serverProjectPath,
            """
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                <ProjectReference Include="..\AutoApp.Client\AutoApp.Client.csproj" />
              </ItemGroup>
            </Project>
            """);
        File.WriteAllText(
            clientProjectPath,
            """
            <Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);
        var step = CreateStep(FileSystem.Instance, serverProjectPath);

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.True(result);
        Assert.Equal(true, _context.Properties["IsBlazorWasmProject"]);
        Assert.Equal(clientProjectPath, _context.Properties["BlazorWasmClientProjectPath"]);
    }

    [Fact]
    public async Task ExecuteAsync_FailsForAmbiguousReferencedWebAssemblyClients()
    {
        string serverProjectPath = Path.Combine(_testDirectory, "Server.csproj");
        File.WriteAllText(serverProjectPath, """
            <Project>
              <ItemGroup>
                <ProjectReference Include="First.csproj" />
                <ProjectReference Include="Second.csproj" />
              </ItemGroup>
            </Project>
            """);
        const string clientProject = """
            <Project>
              <PropertyGroup>
                <UsingMicrosoftNETSdkBlazorWebAssembly>true</UsingMicrosoftNETSdkBlazorWebAssembly>
              </PropertyGroup>
            </Project>
            """;
        File.WriteAllText(Path.Combine(_testDirectory, "First.csproj"), clientProject);
        File.WriteAllText(Path.Combine(_testDirectory, "Second.csproj"), clientProject);
        var step = CreateStep(FileSystem.Instance, serverProjectPath);

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.False(result);
        Assert.False(_context.Properties.ContainsKey("BlazorWasmClientProjectPath"));
    }

    public void Dispose()
    {
        Directory.Delete(_testDirectory, recursive: true);
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
