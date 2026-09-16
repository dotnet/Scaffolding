// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.ScaffoldSteps;

public class UpdateAppSettingsStepTests
{
    private readonly Mock<IFileSystem> _mockFileSystem;
    private readonly ScaffolderContext _context;

    public UpdateAppSettingsStepTests()
    {
        _mockFileSystem = new Mock<IFileSystem>();

        Mock<IScaffolder> scaffolder = new Mock<IScaffolder>();
        scaffolder.Setup(s => s.DisplayName).Returns("TestScaffolder");
        scaffolder.Setup(s => s.Name).Returns("test-scaffolder");
        _context = new ScaffolderContext(scaffolder.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WritesAppSettingsWithTrailingNewLine()
    {
        string projectPath = Path.Combine("test", "project", "TestProject.csproj");
        string baseProjectPath = Path.GetDirectoryName(projectPath)!;
        string generatedAppSettingsPath = Path.Combine(projectPath, "appsettings.json");

        _mockFileSystem.Setup(fs => fs.DirectoryExists(baseProjectPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.EnumerateFiles(baseProjectPath, "appsettings.json", SearchOption.AllDirectories))
            .Returns(new List<string>());

        string? appSettingsContent = null;
        _mockFileSystem.Setup(fs => fs.WriteAllText(generatedAppSettingsPath, It.IsAny<string>()))
            .Callback<string, string>((_, content) => appSettingsContent = content);

        _mockFileSystem.Setup(fs => fs.FileExists(Path.Combine(projectPath, "appsettings.Development.json"))).Returns(false);

        Mock<ITelemetryService> telemetryService = new Mock<ITelemetryService>();
        UpdateAppSettingsStep step = new UpdateAppSettingsStep(
            NullLogger<UpdateAppSettingsStep>.Instance,
            _mockFileSystem.Object,
            telemetryService.Object)
        {
            ProjectPath = projectPath,
            Username = "testuser",
            ClientId = "client-id",
            TenantId = "tenant-id"
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.True(result);
        Assert.NotNull(appSettingsContent);
        Assert.EndsWith(Environment.NewLine, appSettingsContent);

        using JsonDocument document = JsonDocument.Parse(appSettingsContent);
        Assert.True(document.RootElement.TryGetProperty("AzureAd", out JsonElement azureAd));
        Assert.Equal("client-id", azureAd.GetProperty("ClientId").GetString());

        _mockFileSystem.Verify(fs => fs.WriteAllText(generatedAppSettingsPath, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WritesDevelopmentAppSettingsWithTrailingNewLine()
    {
        string projectPath = Path.Combine("test", "project", "TestProject.csproj");
        string baseProjectPath = Path.GetDirectoryName(projectPath)!;
        string appSettingsPath = Path.Combine(baseProjectPath, "appsettings.json");
        string developmentSettingsPath = Path.Combine(projectPath, "appsettings.Development.json");

        _mockFileSystem.Setup(fs => fs.DirectoryExists(baseProjectPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.EnumerateFiles(baseProjectPath, "appsettings.json", SearchOption.AllDirectories))
            .Returns(new List<string> { appSettingsPath });
        _mockFileSystem.Setup(fs => fs.FileExists(appSettingsPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllText(appSettingsPath)).Returns("{}");

        _mockFileSystem.Setup(fs => fs.FileExists(developmentSettingsPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllText(developmentSettingsPath)).Returns("{}");

        string? developmentContent = null;
        _mockFileSystem.Setup(fs => fs.WriteAllText(developmentSettingsPath, It.IsAny<string>()))
            .Callback<string, string>((_, content) => developmentContent = content);

        Mock<ITelemetryService> telemetryService = new Mock<ITelemetryService>();
        UpdateAppSettingsStep step = new UpdateAppSettingsStep(
            NullLogger<UpdateAppSettingsStep>.Instance,
            _mockFileSystem.Object,
            telemetryService.Object)
        {
            ProjectPath = projectPath,
            Username = "testuser",
            ClientId = "client-id"
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.True(result);
        Assert.NotNull(developmentContent);
        Assert.EndsWith(Environment.NewLine, developmentContent);

        using JsonDocument document = JsonDocument.Parse(developmentContent);
        Assert.True(document.RootElement.TryGetProperty("AzureAd", out _));

        _mockFileSystem.Verify(fs => fs.WriteAllText(developmentSettingsPath, It.IsAny<string>()), Times.Once);
    }
}
