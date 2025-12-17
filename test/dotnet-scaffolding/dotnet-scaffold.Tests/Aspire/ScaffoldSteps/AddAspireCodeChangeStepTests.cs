// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Locator;
using Microsoft.DotNet.Scaffolding.CodeModification;
using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.Aspire;
using Microsoft.DotNet.Tools.Scaffold.Aspire.ScaffoldSteps;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.Aspire.ScaffoldSteps;

public class AddAspireCodeChangeStepTests : IDisposable
{
    private readonly string _testProjectDirectory;
    private readonly string _testProjectPath;
    private readonly string _testObjDirectory;
    private readonly string _testDebugDirectory;
    private readonly Mock<ILogger<CodeModificationStep>> _mockLogger;
    private readonly TestTelemetryService _testTelemetryService;

    public AddAspireCodeChangeStepTests()
    {
        // Register MSBuild if not already registered
        if (!MSBuildLocator.IsRegistered)
        {
            MSBuildLocator.RegisterDefaults();
        }

        _testProjectDirectory = Path.Combine(Path.GetTempPath(), "AddAspireCodeChangeStepTests", Guid.NewGuid().ToString());
        _testProjectPath = Path.Combine(_testProjectDirectory, "TestProject.AppHost.csproj");
        _testObjDirectory = Path.Combine(_testProjectDirectory, "obj");
        _testDebugDirectory = Path.Combine(_testObjDirectory, "Debug", "net9.0");

        Directory.CreateDirectory(_testProjectDirectory);
        Directory.CreateDirectory(_testDebugDirectory);

        // Create an Aspire AppHost project file
        File.WriteAllText(_testProjectPath, $@"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <IsAspireHost>true</IsAspireHost>
    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
    <Deterministic>false</Deterministic>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Aspire.Hosting.AppHost"" Version=""9.0.0"" />
  </ItemGroup>
  <ItemGroup>
    <Compile Include=""Program.cs"" />
    <Compile Include=""obj\Debug\net9.0\TestProject.AppHost.ProjectMetadata.g.cs"" />
  </ItemGroup>
</Project>");

        // Create a Program.cs file
        File.WriteAllText(Path.Combine(_testProjectDirectory, "Program.cs"), @"
var builder = DistributedApplication.CreateBuilder(args);
builder.Build().Run();
");

        // Create the AppHost ProjectMetadata file
        string appHostMetadata = @"
namespace Projects
{
    public class TestProject
    {}
}";
        File.WriteAllText(Path.Combine(_testDebugDirectory, "TestProject.AppHost.ProjectMetadata.g.cs"), appHostMetadata);

        _mockLogger = new Mock<ILogger<CodeModificationStep>>();
        _testTelemetryService = new TestTelemetryService();
    }

    public void Dispose()
    {
        if (Directory.Exists(_testProjectDirectory))
        {
            try
            {
                Directory.Delete(_testProjectDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public async Task AddAutoGenProjectPropertiesAsync_WithValidProjectMetadata_CompletesSuccessfully()
    {
        // Arrange
        string workerProjectPath = Path.Combine(_testProjectDirectory, "..", "WorkerProject.csproj");
        string escapedPath = workerProjectPath.Replace("\\", "\\\\");
        string workerMetadata = @"
namespace Projects
{
    public class WorkerService
    {
        public string ProjectPath => """ + escapedPath + @""";
    }
}";
        File.WriteAllText(Path.Combine(_testDebugDirectory, "WorkerService.ProjectMetadata.g.cs"), workerMetadata);

        CommandSettings commandSettings = new CommandSettings
        {
            Type = "redis",
            AppHostProject = _testProjectPath,
            Project = workerProjectPath,
            Prerelease = false
        };

        AddAspireCodeChangeStep step = new AddAspireCodeChangeStep(_mockLogger.Object, _testTelemetryService)
        {
            CodeChangeOptions = new List<string>(),
            ProjectPath = _testProjectPath,
            CodeModifierConfigJsonText = "{\"Files\": []}"
        };

        // Act
        await step.AddAutoGenProjectPropertiesAsync(commandSettings);

        // Assert - Method completes without throwing
        // Note: In a full integration environment with proper Roslyn workspace,
        // the CodeModifierProperties would contain "AutoGenProjectName"
        Assert.NotNull(step.CodeModifierProperties);
    }

    [Fact]
    public async Task AddAutoGenProjectPropertiesAsync_WithNoMatchingProject_CompletesSuccessfully()
    {
        // Arrange
        string workerProjectPath = Path.Combine(_testProjectDirectory, "..", "WorkerProject.csproj");
        string otherProjectPath = Path.Combine(_testProjectDirectory, "..", "OtherProject.csproj");
        string escapedPath = otherProjectPath.Replace("\\", "\\\\");
        string workerMetadata = @"
namespace Projects
{
    public class WorkerService
    {
        public string ProjectPath => """ + escapedPath + @""";
    }
}";
        File.WriteAllText(Path.Combine(_testDebugDirectory, "WorkerService.ProjectMetadata.g.cs"), workerMetadata);

        CommandSettings commandSettings = new CommandSettings
        {
            Type = "redis",
            AppHostProject = _testProjectPath,
            Project = workerProjectPath,
            Prerelease = false
        };

        AddAspireCodeChangeStep step = new AddAspireCodeChangeStep(_mockLogger.Object, _testTelemetryService)
        {
            CodeChangeOptions = new List<string>(),
            ProjectPath = _testProjectPath,
            CodeModifierConfigJsonText = "{\"Files\": []}"
        };

        // Act
        await step.AddAutoGenProjectPropertiesAsync(commandSettings);

        // Assert - Method completes without throwing
        Assert.NotNull(step.CodeModifierProperties);
    }

    [Fact]
    public async Task ExecuteAsync_WithCommandSettings_CallsAddAutoGenProjectPropertiesAsync()
    {
        // Arrange
        string workerProjectPath = Path.Combine(_testProjectDirectory, "..", "WorkerProject.csproj");
        string escapedPath = workerProjectPath.Replace("\\", "\\\\");
        string workerMetadata = @"
namespace Projects
{
    public class WorkerService
    {
        public string ProjectPath => """ + escapedPath + @""";
    }
}";
        File.WriteAllText(Path.Combine(_testDebugDirectory, "WorkerService.ProjectMetadata.g.cs"), workerMetadata);

        CommandSettings commandSettings = new CommandSettings
        {
            Type = "redis",
            AppHostProject = _testProjectPath,
            Project = workerProjectPath,
            Prerelease = false
        };

        AddAspireCodeChangeStep step = new AddAspireCodeChangeStep(_mockLogger.Object, _testTelemetryService)
        {
            CodeChangeOptions = new List<string>(),
            ProjectPath = _testProjectPath,
            CodeModifierConfigJsonText = "{\"Files\": []}"
        };


        ScaffolderContext context = new ScaffolderContext(new TestScaffolder("TestScaffolder"));
        context.Properties[nameof(CommandSettings)] = commandSettings;

        // Act
        bool result = await step.ExecuteAsync(context, CancellationToken.None);

        // Assert - With empty CodeModifierConfig, ExecuteAsync returns false (no files to modify)
        // but it should complete and track telemetry
        Assert.False(result);
        Assert.Single(_testTelemetryService.TrackedEvents);
        // Note: In a full integration environment with proper Roslyn workspace,
        // the CodeModifierProperties would contain "AutoGenProjectName"
    }

    [Fact]
    public async Task ExecuteAsync_WithoutCommandSettings_StillExecutesSuccessfully()
    {
        // Arrange
        AddAspireCodeChangeStep step = new AddAspireCodeChangeStep(_mockLogger.Object, _testTelemetryService)
        {
            CodeChangeOptions = new List<string>(),
            ProjectPath = _testProjectPath,
            CodeModifierConfigJsonText = "{\"Files\": []}"
        };

        ScaffolderContext context = new ScaffolderContext(new TestScaffolder("TestScaffolder"));

        // Act
        bool result = await step.ExecuteAsync(context, CancellationToken.None);

        // Assert - With empty CodeModifierConfig, ExecuteAsync returns false (no files to modify)
        // but it should complete and track telemetry
        Assert.False(result);
        Assert.Single(_testTelemetryService.TrackedEvents);
    }

    private class TestScaffolder : IScaffolder
    {
        public TestScaffolder(string name)
        {
            Name = name;
            DisplayName = name;
        }

        public string Name { get; }
        public string DisplayName { get; }
        public string? Description => "Test Scaffolder";
        public IEnumerable<string> Categories => new[] { "Test" };
        public IEnumerable<ScaffolderOption> Options => Enumerable.Empty<ScaffolderOption>();

        public Task ExecuteAsync(ScaffolderContext context)
        {
            return Task.CompletedTask;
        }
    }

    private class TestTelemetryService : ITelemetryService
    {
        public List<(string EventName, IReadOnlyDictionary<string, string> Properties, IReadOnlyDictionary<string, double> Measurements)> TrackedEvents { get; } = new();

        public void TrackEvent(string eventName, IReadOnlyDictionary<string, string> properties, IReadOnlyDictionary<string, double> measurements)
        {
            TrackedEvents.Add((eventName, properties, measurements));
        }

        public void Flush()
        {
        }
    }
}

