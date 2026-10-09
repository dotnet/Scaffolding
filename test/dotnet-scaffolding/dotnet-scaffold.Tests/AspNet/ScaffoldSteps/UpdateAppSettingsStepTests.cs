// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.ScaffoldSteps;

public class UpdateAppSettingsStepTests
{
    private readonly Mock<IFileSystem> _mockFileSystem;
    private readonly Mock<IScaffolder> _mockScaffolder;
    private readonly ScaffolderContext _context;
    private readonly string _projectDirectory;
    private readonly string _projectPath;
    private readonly string _devSettingsPath;
    private readonly string _appSettingsPath;

    public UpdateAppSettingsStepTests()
    {
        _mockFileSystem = new Mock<IFileSystem>();
        _mockScaffolder = new Mock<IScaffolder>();
        _projectDirectory = Path.Combine("test", "project");
        _projectPath = Path.Combine(_projectDirectory, "TestProject.csproj");
        _devSettingsPath = Path.Combine(_projectDirectory, "appsettings.Development.json");
        _appSettingsPath = Path.Combine(_projectDirectory, "appsettings.json");

        _mockScaffolder.Setup(s => s.DisplayName).Returns("TestScaffolder");
        _mockScaffolder.Setup(s => s.Name).Returns("test-scaffolder");
        _context = new ScaffolderContext(_mockScaffolder.Object);
    }

    [Fact]
    public async Task ExecuteAsync_UpdatesDevelopmentSettingsOnly_PreservingUnrelatedSettings()
    {
        _mockFileSystem.Setup(fs => fs.DirectoryExists(_projectDirectory)).Returns(true);
        _mockFileSystem.Setup(fs => fs.FileExists(_devSettingsPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllText(_devSettingsPath)).Returns(
            """
            {
              "Logging": {
                "LogLevel": {
                  "Default": "Information"
                }
              },
              "AzureAd": {
                "Instance": "https://old-instance/",
                "TenantId": "old-tenant",
                "Domain": "old-domain",
                "ClientId": "old-client",
                "CallbackPath": "/old-callback",
                "SignedOutCallbackPath": "/signout-callback-oidc"
              }
            }
            """);

        string? writtenPath = null;
        string? writtenContent = null;
        _mockFileSystem.Setup(fs => fs.WriteAllText(It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string>((path, content) =>
            {
                writtenPath = path;
                writtenContent = content;
            });

        var step = new UpdateAppSettingsStep(
            NullLogger<UpdateAppSettingsStep>.Instance,
            _mockFileSystem.Object,
            Mock.Of<ITelemetryService>())
        {
            ProjectPath = _projectPath,
            Username = "devuser",
            TenantId = "new-tenant-id",
            ClientId = "new-client-id",
            ClientSecret = "do-not-write-this",
            Overwrite = true
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.True(result);
        Assert.Equal(_devSettingsPath, writtenPath);
        Assert.NotNull(writtenContent);

        JsonNode? json = JsonNode.Parse(writtenContent);
        Assert.NotNull(json);
        Assert.Equal("Information", json["Logging"]?["LogLevel"]?["Default"]?.ToString());
        Assert.Equal("https://login.microsoftonline.com/", json["AzureAd"]?["Instance"]?.ToString());
        Assert.Equal("new-tenant-id", json["AzureAd"]?["TenantId"]?.ToString());
        Assert.Equal("devuser.onmicrosoft.com", json["AzureAd"]?["Domain"]?.ToString());
        Assert.Equal("new-client-id", json["AzureAd"]?["ClientId"]?.ToString());
        Assert.Equal("/signin-oidc", json["AzureAd"]?["CallbackPath"]?.ToString());
        Assert.Equal("/signout-callback-oidc", json["AzureAd"]?["SignedOutCallbackPath"]?.ToString());
        Assert.Null(json["AzureAd"]?["ClientSecret"]);

        _mockFileSystem.Verify(
            fs => fs.EnumerateFiles(It.IsAny<string>(), "appsettings.json", SearchOption.AllDirectories),
            Times.Never);
        _mockFileSystem.Verify(fs => fs.WriteAllText(_appSettingsPath, It.IsAny<string>()), Times.Never);
        _mockFileSystem.Verify(fs => fs.WriteAllText(_devSettingsPath, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenAzureAdConflictsAndOverwriteIsDisabled()
    {
        _mockFileSystem.Setup(fs => fs.DirectoryExists(_projectDirectory)).Returns(true);
        _mockFileSystem.Setup(fs => fs.FileExists(_devSettingsPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllText(_devSettingsPath)).Returns(
            """
            {
              "AzureAd": {
                "Instance": "https://login.microsoftonline.com/",
                "TenantId": "existing-tenant-id",
                "Domain": "existing-domain",
                "ClientId": "existing-client-id",
                "CallbackPath": "/signin-oidc"
              }
            }
            """);

        var step = new UpdateAppSettingsStep(
            NullLogger<UpdateAppSettingsStep>.Instance,
            _mockFileSystem.Object,
            Mock.Of<ITelemetryService>())
        {
            ProjectPath = _projectPath,
            Username = "devuser",
            TenantId = "new-tenant-id",
            ClientId = "new-client-id",
            Overwrite = false
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.False(result);
        _mockFileSystem.Verify(fs => fs.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_OverwritesConflictingAzureAd_WhenOverwriteIsEnabled()
    {
        _mockFileSystem.Setup(fs => fs.DirectoryExists(_projectDirectory)).Returns(true);
        _mockFileSystem.Setup(fs => fs.FileExists(_devSettingsPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllText(_devSettingsPath)).Returns(
            """
            {
              "AzureAd": {
                "Instance": "https://login.microsoftonline.com/",
                "TenantId": "existing-tenant-id",
                "Domain": "existing-domain",
                "ClientId": "existing-client-id",
                "CallbackPath": "/signin-oidc"
              }
            }
            """);

        string? writtenContent = null;
        _mockFileSystem.Setup(fs => fs.WriteAllText(_devSettingsPath, It.IsAny<string>()))
            .Callback<string, string>((_, content) => writtenContent = content);

        var step = new UpdateAppSettingsStep(
            NullLogger<UpdateAppSettingsStep>.Instance,
            _mockFileSystem.Object,
            Mock.Of<ITelemetryService>())
        {
            ProjectPath = _projectPath,
            Username = "devuser",
            TenantId = "new-tenant-id",
            ClientId = "new-client-id",
            Overwrite = true
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.True(result);
        Assert.NotNull(writtenContent);
        JsonNode? json = JsonNode.Parse(writtenContent);
        Assert.NotNull(json);
        Assert.Equal("new-tenant-id", json["AzureAd"]?["TenantId"]?.ToString());
        Assert.Equal("new-client-id", json["AzureAd"]?["ClientId"]?.ToString());
        Assert.Equal("devuser.onmicrosoft.com", json["AzureAd"]?["Domain"]?.ToString());
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task ExecuteAsync_SucceedsWithoutWrite_WhenManagedAzureAdAlreadyMatches(bool overwrite, bool bareProjectFilename)
    {
        string projectPath = bareProjectFilename ? "TestProject.csproj" : _projectPath;
        string projectDirectory = bareProjectFilename ? Directory.GetCurrentDirectory() : _projectDirectory;
        string devSettingsPath = Path.Combine(projectDirectory, "appsettings.Development.json");
        _mockFileSystem.Setup(fs => fs.DirectoryExists(projectDirectory)).Returns(true);
        _mockFileSystem.Setup(fs => fs.FileExists(devSettingsPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllText(devSettingsPath)).Returns(
            """
            {
              "AzureAd": {
                "Instance": "https://login.microsoftonline.com/",
                "TenantId": "tenant-id",
                "Domain": "devuser.onmicrosoft.com",
                "ClientId": "client-id",
                "CallbackPath": "/signin-oidc",
                "SignedOutCallbackPath": "/signout-callback-oidc"
              }
            }
            """);

        var logger = new TestLogger();
        var step = new UpdateAppSettingsStep(
            logger,
            _mockFileSystem.Object,
            Mock.Of<ITelemetryService>())
        {
            ProjectPath = projectPath,
            Username = "devuser",
            TenantId = "tenant-id",
            ClientId = "client-id",
            Overwrite = overwrite
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.True(result);
        _mockFileSystem.Verify(fs => fs.ReadAllText(devSettingsPath), Times.Once);
        _mockFileSystem.Verify(fs => fs.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        Assert.Contains(
            "The generated AzureAd app registration and settings are intended for development environments.",
            logger.InformationMessages);
    }

    [Theory]
    [InlineData("azuread", false, false)]
    [InlineData("AZUREAD", true, false)]
    [InlineData("azuread", false, true)]
    [InlineData("AZUREAD", true, true)]
    [InlineData("AzureAd", false, false)]
    [InlineData("AzureAd", true, false)]
    [InlineData("AzureAd", false, true)]
    [InlineData("AzureAd", true, true)]
    public async Task ExecuteAsync_ResolvesAzureAdSettingsCaseInsensitively(
        string sectionName, bool overwrite, bool settingsMatch)
    {
        string existingTenantId = settingsMatch ? "tenant-id" : overwrite ? "old-tenant-id" : "";
        string settings =
            $$"""
            {
              "{{sectionName}}": {
                "instance": "https://login.microsoftonline.com/",
                "tenantid": "{{existingTenantId}}",
                "domain": "devuser.onmicrosoft.com",
                "clientid": "client-id",
                "callbackpath": "/signin-oidc",
                "SignedOutCallbackPath": "/custom-signout"
              },
              "Logging": { "LogLevel": { "Default": "Information" } }
            }
            """;
        using var originalConfiguration = LoadConfiguration(settings);
        Assert.Equal("client-id", originalConfiguration["AzureAd:ClientId"]);
        _mockFileSystem.Setup(fs => fs.DirectoryExists(_projectDirectory)).Returns(true);
        _mockFileSystem.Setup(fs => fs.FileExists(_devSettingsPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllText(_devSettingsPath)).Returns(settings);

        string? writtenContent = null;
        _mockFileSystem.Setup(fs => fs.WriteAllText(_devSettingsPath, It.IsAny<string>()))
            .Callback<string, string>((_, content) => writtenContent = content);
        var step = new UpdateAppSettingsStep(
            NullLogger<UpdateAppSettingsStep>.Instance, _mockFileSystem.Object, Mock.Of<ITelemetryService>())
        {
            ProjectPath = _projectPath,
            Username = "devuser",
            TenantId = "tenant-id",
            ClientId = "client-id",
            Overwrite = overwrite
        };

        Assert.True(await step.ExecuteAsync(_context, CancellationToken.None));
        if (settingsMatch)
        {
            Assert.Null(writtenContent);
            _mockFileSystem.Verify(fs => fs.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
        else
        {
            Assert.NotNull(writtenContent);
            using var configuration = LoadConfiguration(writtenContent);
            Assert.Equal("https://login.microsoftonline.com/", configuration["AzureAd:Instance"]);
            Assert.Equal("tenant-id", configuration["AzureAd:TenantId"]);
            Assert.Equal("devuser.onmicrosoft.com", configuration["AzureAd:Domain"]);
            Assert.Equal("client-id", configuration["AzureAd:ClientId"]);
            Assert.Equal("/signin-oidc", configuration["AzureAd:CallbackPath"]);
            Assert.Equal("/custom-signout", configuration["AzureAd:SignedOutCallbackPath"]);
            Assert.Equal("Information", configuration["Logging:LogLevel:Default"]);

            JsonObject json = Assert.IsType<JsonObject>(JsonNode.Parse(writtenContent));
            Assert.True(json.ContainsKey(sectionName));
            JsonObject azureAd = Assert.IsType<JsonObject>(json[sectionName]);
            Assert.True(azureAd.ContainsKey("tenantid"));
            Assert.False(azureAd.ContainsKey("TenantId"));
            _mockFileSystem.Verify(fs => fs.WriteAllText(_devSettingsPath, It.IsAny<string>()), Times.Once);
        }
    }

    [Theory]
    [InlineData("""{ /* development settings */ "Logging": { "LogLevel": { "Default": "Information" } }, "AzureAd": { "ClientId": "client-id" } }""")]
    [InlineData("""{ "Logging": { "LogLevel": { "Default": "Information", }, }, "AzureAd": { "ClientId": "client-id", }, }""")]
    public async Task ExecuteAsync_AcceptsAspNetJsonConfigurationSyntax(string settings)
    {
        using var originalConfiguration = LoadConfiguration(settings);
        Assert.Equal("client-id", originalConfiguration["AzureAd:ClientId"]);
        _mockFileSystem.Setup(fs => fs.DirectoryExists(_projectDirectory)).Returns(true);
        _mockFileSystem.Setup(fs => fs.FileExists(_devSettingsPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllText(_devSettingsPath)).Returns(settings);
        string? writtenContent = null;
        _mockFileSystem.Setup(fs => fs.WriteAllText(_devSettingsPath, It.IsAny<string>()))
            .Callback<string, string>((_, content) => writtenContent = content);
        var step = new UpdateAppSettingsStep(
            NullLogger<UpdateAppSettingsStep>.Instance, _mockFileSystem.Object, Mock.Of<ITelemetryService>())
        {
            ProjectPath = _projectPath,
            Username = "devuser",
            TenantId = "tenant-id",
            ClientId = "client-id"
        };

        Assert.True(await step.ExecuteAsync(_context, CancellationToken.None));
        Assert.NotNull(writtenContent);
        using var configuration = LoadConfiguration(writtenContent);
        Assert.Equal("tenant-id", configuration["AzureAd:TenantId"]);
        Assert.Equal("client-id", configuration["AzureAd:ClientId"]);
        Assert.Equal("Information", configuration["Logging:LogLevel:Default"]);
        _mockFileSystem.Verify(fs => fs.WriteAllText(_devSettingsPath, It.IsAny<string>()), Times.Once);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteAsync_MatchingSettingsWithCommentsAndTrailingCommas_DoesNotWrite(bool overwrite)
    {
        const string settings =
            """
            {
              /* development identity */
              "AzureAd": {
                "Instance": "https://login.microsoftonline.com/",
                "TenantId": "tenant-id",
                "Domain": "devuser.onmicrosoft.com",
                "ClientId": "client-id",
                "CallbackPath": "/signin-oidc",
                "SignedOutCallbackPath": "/custom-signout",
              },
            }
            """;
        using var configuration = LoadConfiguration(settings);
        Assert.Equal("client-id", configuration["AzureAd:ClientId"]);
        _mockFileSystem.Setup(fs => fs.DirectoryExists(_projectDirectory)).Returns(true);
        _mockFileSystem.Setup(fs => fs.FileExists(_devSettingsPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllText(_devSettingsPath)).Returns(settings);
        var step = new UpdateAppSettingsStep(
            NullLogger<UpdateAppSettingsStep>.Instance, _mockFileSystem.Object, Mock.Of<ITelemetryService>())
        {
            ProjectPath = _projectPath,
            Username = "devuser",
            TenantId = "tenant-id",
            ClientId = "client-id",
            Overwrite = overwrite
        };

        Assert.True(await step.ExecuteAsync(_context, CancellationToken.None));
        _mockFileSystem.Verify(fs => fs.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    private static ConfigurationManager LoadConfiguration(string json)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var configuration = new ConfigurationManager();
        configuration.AddJsonStream(stream);
        return configuration;
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("client-id", false)]
    [InlineData("old-client-id", true)]
    public async Task ExecuteAsync_BareProjectFilename_WritesDevelopmentSettingsInCurrentDirectory(
        string? existingClientId, bool overwrite)
    {
        string projectDirectory = Directory.GetCurrentDirectory();
        string devSettingsPath = Path.Combine(projectDirectory, "appsettings.Development.json");
        _mockFileSystem.Setup(fs => fs.DirectoryExists(projectDirectory)).Returns(true);
        _mockFileSystem.Setup(fs => fs.FileExists(devSettingsPath)).Returns(existingClientId is not null);
        if (existingClientId is not null)
        {
            _mockFileSystem.Setup(fs => fs.ReadAllText(devSettingsPath)).Returns(
                $$"""
                {
                  "Logging": { "LogLevel": { "Default": "Information" } },
                  "AzureAd": {
                    "ClientId": "{{existingClientId}}",
                    "SignedOutCallbackPath": "/custom-signout"
                  }
                }
                """);
        }

        string? writtenContent = null;
        _mockFileSystem.Setup(fs => fs.WriteAllText(devSettingsPath, It.IsAny<string>()))
            .Callback<string, string>((_, content) => writtenContent = content);
        var step = new UpdateAppSettingsStep(
            NullLogger<UpdateAppSettingsStep>.Instance,
            _mockFileSystem.Object,
            Mock.Of<ITelemetryService>())
        {
            ProjectPath = "TestProject.csproj",
            Username = "devuser",
            TenantId = "tenant-id",
            ClientId = "client-id",
            Overwrite = overwrite
        };

        Assert.True(await step.ExecuteAsync(_context, CancellationToken.None));
        Assert.NotNull(writtenContent);
        JsonNode? json = JsonNode.Parse(writtenContent);
        Assert.NotNull(json);
        Assert.Equal("https://login.microsoftonline.com/", json["AzureAd"]?["Instance"]?.ToString());
        Assert.Equal("tenant-id", json["AzureAd"]?["TenantId"]?.ToString());
        Assert.Equal("devuser.onmicrosoft.com", json["AzureAd"]?["Domain"]?.ToString());
        Assert.Equal("client-id", json["AzureAd"]?["ClientId"]?.ToString());
        Assert.Equal("/signin-oidc", json["AzureAd"]?["CallbackPath"]?.ToString());
        if (existingClientId is not null)
        {
            Assert.Equal("Information", json["Logging"]?["LogLevel"]?["Default"]?.ToString());
            Assert.Equal("/custom-signout", json["AzureAd"]?["SignedOutCallbackPath"]?.ToString());
        }

        _mockFileSystem.Verify(fs => fs.WriteAllText(devSettingsPath, It.IsAny<string>()), Times.Once);
        _mockFileSystem.Verify(fs => fs.ReadAllText(Path.Combine(projectDirectory, "appsettings.json")), Times.Never);
        _mockFileSystem.Verify(fs => fs.WriteAllText(Path.Combine(projectDirectory, "appsettings.json"), It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteAsync_PreservesExtraAzureAdProperties_WhenAddingMissingManagedSettings(bool overwrite)
    {
        _mockFileSystem.Setup(fs => fs.DirectoryExists(_projectDirectory)).Returns(true);
        _mockFileSystem.Setup(fs => fs.FileExists(_devSettingsPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllText(_devSettingsPath)).Returns(
            """
            {
              "AzureAd": {
                "ClientId": "client-id",
                "SignedOutCallbackPath": "/signout-callback-oidc",
                "ExtraOptions": { "Enabled": true }
              }
            }
            """);

        string? writtenContent = null;
        _mockFileSystem.Setup(fs => fs.WriteAllText(_devSettingsPath, It.IsAny<string>()))
            .Callback<string, string>((_, content) => writtenContent = content);

        var step = new UpdateAppSettingsStep(
            NullLogger<UpdateAppSettingsStep>.Instance,
            _mockFileSystem.Object,
            Mock.Of<ITelemetryService>())
        {
            ProjectPath = _projectPath,
            Username = "devuser",
            TenantId = "tenant-id",
            ClientId = "client-id",
            Overwrite = overwrite
        };

        Assert.True(await step.ExecuteAsync(_context, CancellationToken.None));
        Assert.NotNull(writtenContent);
        JsonNode? json = JsonNode.Parse(writtenContent);
        Assert.NotNull(json);
        Assert.Equal("tenant-id", json["AzureAd"]?["TenantId"]?.ToString());
        Assert.Equal("/signout-callback-oidc", json["AzureAd"]?["SignedOutCallbackPath"]?.ToString());
        Assert.True(json["AzureAd"]?["ExtraOptions"]?["Enabled"]?.GetValue<bool>());
        _mockFileSystem.Verify(fs => fs.WriteAllText(_devSettingsPath, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenAzureAdSectionIsNotObjectAndOverwriteIsDisabled()
    {
        _mockFileSystem.Setup(fs => fs.DirectoryExists(_projectDirectory)).Returns(true);
        _mockFileSystem.Setup(fs => fs.FileExists(_devSettingsPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllText(_devSettingsPath)).Returns(
            """
            {
              "AzureAd": "invalid-shape"
            }
            """);

        var step = new UpdateAppSettingsStep(
            NullLogger<UpdateAppSettingsStep>.Instance,
            _mockFileSystem.Object,
            Mock.Of<ITelemetryService>())
        {
            ProjectPath = _projectPath,
            Username = "devuser",
            TenantId = "tenant-id",
            ClientId = "client-id",
            Overwrite = false
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.False(result);
        _mockFileSystem.Verify(fs => fs.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ReplacesAzureAdSection_WhenItIsNotObjectAndOverwriteIsEnabled()
    {
        _mockFileSystem.Setup(fs => fs.DirectoryExists(_projectDirectory)).Returns(true);
        _mockFileSystem.Setup(fs => fs.FileExists(_devSettingsPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllText(_devSettingsPath)).Returns(
            """
            {
              "AzureAd": "invalid-shape"
            }
            """);

        string? writtenContent = null;
        _mockFileSystem.Setup(fs => fs.WriteAllText(_devSettingsPath, It.IsAny<string>()))
            .Callback<string, string>((_, content) => writtenContent = content);

        var step = new UpdateAppSettingsStep(
            NullLogger<UpdateAppSettingsStep>.Instance,
            _mockFileSystem.Object,
            Mock.Of<ITelemetryService>())
        {
            ProjectPath = _projectPath,
            Username = "devuser",
            TenantId = "tenant-id",
            ClientId = "client-id",
            Overwrite = true
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.True(result);
        Assert.NotNull(writtenContent);
        JsonNode? json = JsonNode.Parse(writtenContent);
        Assert.NotNull(json);
        Assert.Equal("tenant-id", json["AzureAd"]?["TenantId"]?.ToString());
        Assert.Equal("client-id", json["AzureAd"]?["ClientId"]?.ToString());
    }

    [Fact]
    public async Task ExecuteAsync_CreatesDevelopmentSettingsFile_WhenMissing()
    {
        _mockFileSystem.Setup(fs => fs.DirectoryExists(_projectDirectory)).Returns(true);
        _mockFileSystem.Setup(fs => fs.FileExists(_devSettingsPath)).Returns(false);

        string? writtenContent = null;
        _mockFileSystem.Setup(fs => fs.WriteAllText(_devSettingsPath, It.IsAny<string>()))
            .Callback<string, string>((_, content) => writtenContent = content);

        var step = new UpdateAppSettingsStep(
            NullLogger<UpdateAppSettingsStep>.Instance,
            _mockFileSystem.Object,
            Mock.Of<ITelemetryService>())
        {
            ProjectPath = _projectPath,
            Username = "devuser",
            TenantId = "tenant-id",
            ClientId = "client-id"
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.True(result);
        Assert.NotNull(writtenContent);
        JsonNode? json = JsonNode.Parse(writtenContent);
        Assert.NotNull(json);
        Assert.Equal("tenant-id", json["AzureAd"]?["TenantId"]?.ToString());
        Assert.Equal("client-id", json["AzureAd"]?["ClientId"]?.ToString());
        _mockFileSystem.Verify(fs => fs.WriteAllText(_devSettingsPath, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenDevelopmentSettingsIsInvalidJson()
    {
        _mockFileSystem.Setup(fs => fs.DirectoryExists(_projectDirectory)).Returns(true);
        _mockFileSystem.Setup(fs => fs.FileExists(_devSettingsPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllText(_devSettingsPath)).Returns("{ invalid json");

        var step = new UpdateAppSettingsStep(
            NullLogger<UpdateAppSettingsStep>.Instance,
            _mockFileSystem.Object,
            Mock.Of<ITelemetryService>())
        {
            ProjectPath = _projectPath,
            Username = "devuser",
            TenantId = "tenant-id",
            ClientId = "client-id"
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.False(result);
        _mockFileSystem.Verify(fs => fs.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenClientIdIsMissing()
    {
        _mockFileSystem.Setup(fs => fs.DirectoryExists(_projectDirectory)).Returns(true);

        var step = new UpdateAppSettingsStep(
            NullLogger<UpdateAppSettingsStep>.Instance,
            _mockFileSystem.Object,
            Mock.Of<ITelemetryService>())
        {
            ProjectPath = _projectPath,
            Username = "devuser",
            TenantId = "tenant-id",
            ClientId = null
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.False(result);
        _mockFileSystem.Verify(fs => fs.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenTenantIdIsMissing()
    {
        _mockFileSystem.Setup(fs => fs.DirectoryExists(_projectDirectory)).Returns(true);

        var step = new UpdateAppSettingsStep(
            NullLogger<UpdateAppSettingsStep>.Instance,
            _mockFileSystem.Object,
            Mock.Of<ITelemetryService>())
        {
            ProjectPath = _projectPath,
            Username = "devuser",
            TenantId = null,
            ClientId = "client-id"
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.False(result);
        _mockFileSystem.Verify(fs => fs.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenDomainAndUsernameAreMissing()
    {
        _mockFileSystem.Setup(fs => fs.DirectoryExists(_projectDirectory)).Returns(true);
        _mockFileSystem.Setup(fs => fs.FileExists(_devSettingsPath)).Returns(false);

        var step = new UpdateAppSettingsStep(
            NullLogger<UpdateAppSettingsStep>.Instance,
            _mockFileSystem.Object,
            Mock.Of<ITelemetryService>())
        {
            ProjectPath = _projectPath,
            Username = null,
            Domain = null,
            TenantId = "tenant-id",
            ClientId = "client-id"
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.False(result);
        _mockFileSystem.Verify(fs => fs.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    private sealed class TestLogger : ILogger<UpdateAppSettingsStep>
    {
        public List<string> InformationMessages { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Information)
            {
                InformationMessages.Add(formatter(state, exception));
            }
        }
    }
}
