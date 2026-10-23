// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.ScaffoldSteps;

public class UpdateAppSettingsStepTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task ExecuteAsync_OnWriteFailure_CleansUpOnlyCurrentRunApplication(bool existingApp, bool developmentSettings)
    {
        var fileSystem = CreateFileSystem();
        var failingPath = Path.Combine("project", developmentSettings ? "appsettings.Development.json" : "appsettings.json");
        fileSystem.Setup(fs => fs.WriteAllText(failingPath, It.IsAny<string>()))
            .Throws(new IOException("Write failed."));
        var step = CreateStep(fileSystem);
        var context = CreateContext(existingApp);

        Assert.False(await step.ExecuteAsync(context));

        if (existingApp)
        {
            Assert.Empty(step.CleanupCommands);
            Assert.Equal("app-id", context.Properties["ClientId"]);
        }
        else
        {
            Assert.Equal(new[] { "--unregister-app", "--client-id", "app-id", "--tenant-id", "tenant-a",
                "--username", "user@example.com" }, Assert.Single(step.CleanupCommands));
            Assert.Empty(context.Properties);
        }
    }

    [Fact]
    public async Task ExecuteAsync_InvalidDevelopmentSettings_FailsWithoutOverwritingThem()
    {
        var fileSystem = CreateFileSystem();
        var developmentPath = Path.Combine("project", "appsettings.Development.json");
        fileSystem.Setup(fs => fs.ReadAllText(developmentPath)).Returns("invalid JSON");
        var step = CreateStep(fileSystem);

        Assert.False(await step.ExecuteAsync(CreateContext(false)));

        Assert.Single(step.CleanupCommands);
        fileSystem.Verify(fs => fs.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteAsync_PartialDevelopmentWriteFailure_RestoresOriginalSettings(bool missingPrimary)
    {
        var primary = Path.Combine("project", "appsettings.json");
        var development = Path.Combine("project", "appsettings.Development.json");
        var originalPrimary = """{"Keep":"primary","AzureAd":{"ClientId":"previous-app"}}""";
        var originalDevelopment = """{"Keep":"development"}""";
        var files = new Dictionary<string, string> { [development] = originalDevelopment };
        if (!missingPrimary)
        {
            files[primary] = originalPrimary;
        }
        var fileSystem = CreateFileSystem();
        fileSystem.Setup(fs => fs.FileExists(It.IsAny<string>()))
            .Returns((string path) => files.ContainsKey(path));
        fileSystem.Setup(fs => fs.ReadAllText(It.IsAny<string>()))
            .Returns((string path) => files[path]);
        fileSystem.Setup(fs => fs.DeleteFile(It.IsAny<string>()))
            .Callback((string path) => files.Remove(path));
        var failed = false;
        fileSystem.Setup(fs => fs.WriteAllText(It.IsAny<string>(), It.IsAny<string>()))
            .Callback((string path, string content) =>
            {
                if (path == development && !failed)
                {
                    failed = true;
                    files[path] = "partially written";
                    throw new IOException("Development write failed after truncation.");
                }
                files[path] = content;
            });
        var step = CreateStep(fileSystem);

        Assert.False(await step.ExecuteAsync(CreateContext(false)));

        Assert.True(failed);
        Assert.Equal(originalDevelopment, files[development]);
        if (missingPrimary)
        {
            Assert.False(files.ContainsKey(primary));
        }
        else
        {
            Assert.Equal(originalPrimary, files[primary]);
        }
        Assert.Single(step.CleanupCommands);
    }

    [Fact]
    public async Task ExecuteAsync_SuccessPersistsBothSettingsFilesWithoutCleanup()
    {
        var fileSystem = CreateFileSystem();
        var step = CreateStep(fileSystem);
        var context = CreateContext(false);

        Assert.True(await step.ExecuteAsync(context));

        Assert.Empty(step.CleanupCommands);
        Assert.Equal("app-id", context.Properties["ClientId"]);
        foreach (var name in new[] { "appsettings.json", "appsettings.Development.json" })
        {
            fileSystem.Verify(fs => fs.WriteAllText(Path.Combine("project", name),
                It.Is<string>(json => json.Contains("app-id") && json.Contains("tenant-a"))), Times.Once);
        }
    }

    [Fact]
    public async Task ExecuteAsync_MissingSettings_CreatesFileInProjectDirectory()
    {
        var fileSystem = CreateFileSystem();
        fileSystem.Setup(fs => fs.EnumerateFiles("project", "appsettings.json", SearchOption.AllDirectories))
            .Returns(Array.Empty<string>());
        var step = CreateStep(fileSystem);

        Assert.True(await step.ExecuteAsync(CreateContext(false)));

        fileSystem.Verify(fs => fs.WriteAllText(Path.Combine("project", "appsettings.json"), It.IsAny<string>()), Times.Once);
    }

    private static Mock<IFileSystem> CreateFileSystem()
    {
        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(fs => fs.DirectoryExists("project")).Returns(true);
        fileSystem.Setup(fs => fs.EnumerateFiles("project", "appsettings.json", SearchOption.AllDirectories))
            .Returns(new[] { Path.Combine("project", "appsettings.json") });
        fileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(true);
        fileSystem.Setup(fs => fs.ReadAllText(It.IsAny<string>())).Returns("{}");
        return fileSystem;
    }

    private static TestStep CreateStep(Mock<IFileSystem> fileSystem) => new(fileSystem.Object)
    {
        ProjectPath = Path.Combine("project", "Test.csproj"),
        ClientId = "app-id",
        TenantId = "tenant-a",
        Username = "user@example.com"
    };

    private static ScaffolderContext CreateContext(bool existingApp)
    {
        var context = new ScaffolderContext(Mock.Of<IScaffolder>());
        context.Properties["ClientId"] = "app-id";
        if (!existingApp)
        {
            context.Properties[EntraRegistrationCleanup.CreatedClientIdKey] = "app-id";
            context.Properties["ClientSecret"] = "development-secret";
        }
        return context;
    }

    private sealed class TestStep(IFileSystem fileSystem) : UpdateAppSettingsStep(
        NullLogger<UpdateAppSettingsStep>.Instance, fileSystem, Mock.Of<ITelemetryService>())
    {
        public List<string[]> CleanupCommands { get; } = [];

        protected override void CleanupRegistration(ScaffolderContext context)
            => EntraRegistrationCleanup.TryCleanup(context, TenantId, Username, NullLogger.Instance, args =>
            {
                CleanupCommands.Add(args);
                return (0, null);
            });
    }
}
