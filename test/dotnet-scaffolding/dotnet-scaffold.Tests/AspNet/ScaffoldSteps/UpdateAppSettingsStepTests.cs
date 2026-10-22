// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.ScaffoldSteps;

public class UpdateAppSettingsStepTests
{
    private const string TenantId = "new-tenant-id";
    private const string ClientId = "new-client-id";
    private const string Domain = "contoso.com";
    private const string Username = "developer@contoso.com";

    private readonly Mock<IFileSystem> _fileSystem = new();
    private readonly Dictionary<string, string> _files = new();
    private readonly ScaffolderContext _context;
    private readonly string _projectDirectory = Path.Combine("test", "project");
    private readonly string _projectPath;
    private readonly string _developmentSettingsPath;
    private readonly string _appSettingsPath;
    private string? _writtenContent;

    public UpdateAppSettingsStepTests()
    {
        _projectPath = Path.Combine(_projectDirectory, "TestProject.csproj");
        _developmentSettingsPath = Path.Combine(_projectDirectory, "appsettings.Development.json");
        _appSettingsPath = Path.Combine(_projectDirectory, "appsettings.json");

        var scaffolder = new Mock<IScaffolder>();
        scaffolder.Setup(s => s.DisplayName).Returns("TestScaffolder");
        scaffolder.Setup(s => s.Name).Returns("test-scaffolder");
        _context = new ScaffolderContext(scaffolder.Object);

        _fileSystem.Setup(fs => fs.DirectoryExists(_projectDirectory)).Returns(true);
        _fileSystem.Setup(fs => fs.FileExists(It.IsAny<string>()))
            .Returns<string>(path => _files.ContainsKey(path));
        _fileSystem.Setup(fs => fs.ReadAllText(It.IsAny<string>()))
            .Returns<string>(path => _files[path]);
        _fileSystem.Setup(fs => fs.WriteAllText(It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string>((path, content) => _files[path] = content);
        _fileSystem.Setup(fs => fs.MoveFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
            .Callback<string, string, bool>((source, destination, _) =>
            {
                _writtenContent = _files[source];
                _files[destination] = _writtenContent;
                _files.Remove(source);
            });
        _fileSystem.Setup(fs => fs.DeleteFile(It.IsAny<string>()))
            .Callback<string>(path => _files.Remove(path));
    }

    [Fact]
    public async Task ExecuteAsync_CreatesDevelopmentSettingsWithoutReadingOrWritingBaseSettings()
    {
        _fileSystem.Setup(fs => fs.FileExists(_developmentSettingsPath)).Returns(false);
        _fileSystem.Setup(fs => fs.FileExists(_appSettingsPath)).Returns(true);

        (bool result, _) = await ExecuteAsync();

        Assert.True(result);
        JsonNode writtenSettings = ParseWrittenSettings();
        Assert.Equal(UpdateAppSettingsStep.DefaultInstance, writtenSettings["AzureAd"]?["Instance"]?.ToString());
        Assert.Equal(TenantId, writtenSettings["AzureAd"]?["TenantId"]?.ToString());
        Assert.Equal(Domain, writtenSettings["AzureAd"]?["Domain"]?.ToString());
        Assert.Equal(ClientId, writtenSettings["AzureAd"]?["ClientId"]?.ToString());
        Assert.Equal(UpdateAppSettingsStep.DefaultCallbackPath, writtenSettings["AzureAd"]?["CallbackPath"]?.ToString());
        Assert.Null(writtenSettings["AzureAd"]?["ClientSecret"]);

        _fileSystem.Verify(
            fs => fs.EnumerateFiles(
                It.IsAny<string>(),
                "appsettings.json",
                SearchOption.AllDirectories),
            Times.Never);
        _fileSystem.Verify(fs => fs.ReadAllText(_appSettingsPath), Times.Never);
        _fileSystem.Verify(fs => fs.WriteAllText(_appSettingsPath, It.IsAny<string>()), Times.Never);
        _fileSystem.Verify(fs => fs.WriteAllText(_developmentSettingsPath, It.IsAny<string>()), Times.Never);
        _fileSystem.Verify(fs => fs.MoveFile(
            It.Is<string>(path => Path.GetDirectoryName(path) == _projectDirectory && path != _developmentSettingsPath),
            _developmentSettingsPath,
            true), Times.Once);
        Assert.Single(_files);
    }

    [Theory]
    [InlineData("Instance")]
    [InlineData("TenantId")]
    [InlineData("Domain")]
    [InlineData("ClientId")]
    [InlineData("CallbackPath")]
    public async Task ExecuteAsync_PopulatesMissingManagedValue(string missingKey)
    {
        JsonObject azureAd = CreateExpectedAzureAd();
        azureAd.Remove(missingKey);
        SetExistingSettings(new JsonObject
        {
            ["Logging"] = new JsonObject { ["Level"] = "Debug" },
            ["AzureAd"] = azureAd
        });

        (bool result, _) = await ExecuteAsync();

        Assert.True(result);
        JsonNode writtenSettings = ParseWrittenSettings();
        Assert.Equal(CreateExpectedAzureAd()[missingKey]?.ToString(), writtenSettings["AzureAd"]?[missingKey]?.ToString());
        Assert.Equal("Debug", writtenSettings["Logging"]?["Level"]?.ToString());
    }

    [Fact]
    public async Task ExecuteAsync_LeavesMatchingManagedValuesUnchanged()
    {
        SetExistingSettings(new JsonObject
        {
            ["AzureAd"] = CreateExpectedAzureAd()
        });

        (bool result, _) = await ExecuteAsync();

        Assert.True(result);
        Assert.Null(_writtenContent);
        _fileSystem.Verify(fs => fs.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData("Instance", "https://login.microsoftonline.us/")]
    [InlineData("TenantId", "existing-tenant-id")]
    [InlineData("Domain", "fabrikam.com")]
    [InlineData("ClientId", "existing-client-id")]
    [InlineData("CallbackPath", "/custom-signin")]
    public async Task ExecuteAsync_ReportsConflictAndDoesNotWrite_WhenOverwriteIsDisabled(
        string conflictingKey,
        string conflictingValue)
    {
        JsonObject azureAd = CreateExpectedAzureAd();
        azureAd[conflictingKey] = conflictingValue;
        SetExistingSettings(new JsonObject { ["AzureAd"] = azureAd });

        (bool result, TestLogger logger) = await ExecuteAsync();

        Assert.False(result);
        Assert.Contains(logger.ErrorMessages, message => message.Contains($"'{conflictingKey}'", StringComparison.Ordinal));
        _fileSystem.Verify(fs => fs.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_OverwriteUpdatesOnlyManagedValuesAndPreservesOtherSettings()
    {
        SetExistingSettings(new JsonObject
        {
            ["Logging"] = new JsonObject
            {
                ["LogLevel"] = new JsonObject { ["Default"] = "Information" }
            },
            ["AzureAd"] = new JsonObject
            {
                ["Instance"] = "https://login.microsoftonline.us/",
                ["TenantId"] = "existing-tenant-id",
                ["Domain"] = "fabrikam.com",
                ["ClientId"] = "existing-client-id",
                ["CallbackPath"] = "/custom-signin",
                ["SignedOutCallbackPath"] = "/custom-signout"
            }
        });

        (bool result, _) = await ExecuteAsync(overwrite: true);

        Assert.True(result);
        JsonNode writtenSettings = ParseWrittenSettings();
        Assert.Equal("Information", writtenSettings["Logging"]?["LogLevel"]?["Default"]?.ToString());
        Assert.Equal("/custom-signout", writtenSettings["AzureAd"]?["SignedOutCallbackPath"]?.ToString());
        AssertManagedValuesMatch(writtenSettings["AzureAd"]!);
    }

    [Fact]
    public async Task ExecuteAsync_FailsWithoutWrite_WhenDevelopmentSettingsIsMalformed()
    {
        _fileSystem.Setup(fs => fs.FileExists(_developmentSettingsPath)).Returns(true);
        _fileSystem.Setup(fs => fs.ReadAllText(_developmentSettingsPath)).Returns("{ malformed json");

        (bool result, _) = await ExecuteAsync(overwrite: true);

        Assert.False(result);
        Assert.Null(_writtenContent);
        _fileSystem.Verify(fs => fs.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteAsync_PreservesOriginalSettingsWhenTemporaryWriteFails(bool fileExists)
    {
        const string originalContent = """{"Logging":{"LogLevel":{"Default":"Debug"}}}""";
        if (fileExists)
        {
            SetExistingSettingsJson(originalContent);
        }
        string? temporaryPath = null;
        _fileSystem.Setup(fs => fs.WriteAllText(It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string>((path, content) =>
            {
                temporaryPath = path;
                _files[path] = content[..(content.Length / 2)];
                throw new IOException("write failed after partial output");
            });

        (bool result, TestLogger logger) = await ExecuteAsync();

        Assert.False(result);
        Assert.Contains(logger.ErrorMessages, message => message.Contains("write failed", StringComparison.Ordinal));
        Assert.NotNull(temporaryPath);
        Assert.Equal(fileExists, _files.ContainsKey(_developmentSettingsPath));
        if (fileExists)
        {
            Assert.Equal(originalContent, _files[_developmentSettingsPath]);
        }
        Assert.False(_files.ContainsKey(temporaryPath));
        _fileSystem.Verify(fs => fs.DeleteFile(temporaryPath), Times.Once);
        _fileSystem.Verify(fs => fs.MoveFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteAsync_PreservesOriginalSettingsWhenReplacementFails(bool fileExists)
    {
        const string originalContent = """{"Logging":{"LogLevel":{"Default":"Debug"}}}""";
        if (fileExists)
        {
            SetExistingSettingsJson(originalContent);
        }
        string? temporaryPath = null;
        _fileSystem.Setup(fs => fs.MoveFile(It.IsAny<string>(), _developmentSettingsPath, true))
            .Callback<string, string, bool>((source, _, _) =>
            {
                temporaryPath = source;
                Assert.NotNull(JsonNode.Parse(_files[source])?["AzureAd"]);
                throw new IOException("replacement failed");
            });

        (bool result, TestLogger logger) = await ExecuteAsync();

        Assert.False(result);
        Assert.Contains(logger.ErrorMessages, message => message.Contains("replacement failed", StringComparison.Ordinal));
        Assert.NotNull(temporaryPath);
        Assert.Equal(fileExists, _files.ContainsKey(_developmentSettingsPath));
        if (fileExists)
        {
            Assert.Equal(originalContent, _files[_developmentSettingsPath]);
        }
        Assert.False(_files.ContainsKey(temporaryPath));
        _fileSystem.Verify(fs => fs.DeleteFile(temporaryPath), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ReportsCleanupFailureWithoutHidingWriteFailure()
    {
        _fileSystem.Setup(fs => fs.WriteAllText(It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string>((path, _) =>
            {
                _files[path] = "{";
                throw new IOException("write failed");
            });
        _fileSystem.Setup(fs => fs.DeleteFile(It.IsAny<string>()))
            .Throws(new IOException("cleanup failed"));

        (bool result, TestLogger logger) = await ExecuteAsync();

        Assert.False(result);
        Assert.Contains(logger.ErrorMessages, message => message.Contains("write failed", StringComparison.Ordinal));
        Assert.Contains(logger.WarningMessages, message => message.Contains("cleanup failed", StringComparison.Ordinal));
        Assert.False(_files.ContainsKey(_developmentSettingsPath));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteAsync_SavesCompleteSettingsUsingRealFileSystem(bool fileExists)
    {
        string projectDirectory = Path.Combine(Path.GetTempPath(), $"UpdateAppSettingsStepTests-{Guid.NewGuid():N}");
        string settingsPath = Path.Combine(projectDirectory, "appsettings.Development.json");
        Directory.CreateDirectory(projectDirectory);
        try
        {
            if (fileExists)
            {
                File.WriteAllText(settingsPath, """{"Logging":{"LogLevel":{"Default":"Debug"}}}""");
            }
            var step = new UpdateAppSettingsStep(new TestLogger(), new FileSystem(), Mock.Of<ITelemetryService>())
            {
                ProjectPath = Path.Combine(projectDirectory, "TestProject.csproj"),
                Username = Username,
                TenantId = TenantId,
                ClientId = ClientId
            };

            bool result = await step.ExecuteAsync(_context);

            Assert.True(result);
            JsonNode settings = JsonNode.Parse(File.ReadAllText(settingsPath))!;
            AssertManagedValuesMatch(settings["AzureAd"]!);
            if (fileExists)
            {
                Assert.Equal("Debug", settings["Logging"]?["LogLevel"]?["Default"]?.ToString());
            }
            Assert.Equal(settingsPath, Assert.Single(Directory.GetFiles(projectDirectory)));
        }
        finally
        {
            Directory.Delete(projectDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteAsync_UsesCustomCallbackPathForConfigurationAndAuthorization()
    {
        const string callbackPath = "/custom-signin";
        _fileSystem.Setup(fs => fs.FileExists(_developmentSettingsPath)).Returns(false);

        (bool result, _) = await ExecuteAsync(callbackPath: callbackPath);

        Assert.True(result);
        Assert.Equal(callbackPath, ParseWrittenSettings()["AzureAd"]?["CallbackPath"]?.ToString());
        Assert.Equal(callbackPath, _context.Properties["CallbackPath"]);
    }

    [Fact]
    public async Task ExecuteAsync_ResolvesManagedKeysCaseInsensitively()
    {
        _fileSystem.Setup(fs => fs.FileExists(_developmentSettingsPath)).Returns(true);
        _fileSystem.Setup(fs => fs.ReadAllText(_developmentSettingsPath)).Returns(
            """
            {
              "azuread": {
                "instance": "https://login.microsoftonline.com/",
                "tenantid": "new-tenant-id",
                "domain": "contoso.com",
                "clientid": "new-client-id",
                "callbackpath": "/signin-oidc"
              }
            }
            """);

        (bool result, _) = await ExecuteAsync();

        Assert.True(result);
        Assert.Null(_writtenContent);
    }

    [Theory]
    [InlineData("null", false)]
    [InlineData("null", true)]
    [InlineData("/* development settings */ null", false)]
    [InlineData("[]", true)]
    [InlineData("\"text\"", false)]
    [InlineData("42", true)]
    public async Task ExecuteAsync_FailsWithoutWrite_WhenDevelopmentSettingsRootIsNotObject(string settings, bool overwrite)
    {
        SetExistingSettingsJson(settings);

        (bool result, TestLogger logger) = await ExecuteAsync(overwrite: overwrite);

        Assert.False(result);
        Assert.Contains(logger.ErrorMessages, message => message.Contains("root JSON value must be an object", StringComparison.Ordinal));
        _fileSystem.Verify(fs => fs.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_PreservesSectionsThatDifferOnlyByCase()
    {
        SetExistingSettingsJson("""{"Logging":{"A":"1"},"logging":{"B":"2"}}""");

        (bool result, _) = await ExecuteAsync();

        Assert.True(result);
        JsonNode writtenSettings = ParseWrittenSettings();
        Assert.Equal("1", writtenSettings["Logging"]?["A"]?.ToString());
        Assert.Equal("2", writtenSettings["logging"]?["B"]?.ToString());
        AssertManagedValuesMatch(writtenSettings["AzureAd"]!);
    }

    [Fact]
    public async Task ExecuteAsync_UpdatesCaseVariantAzureAdKeysInPlace()
    {
        SetExistingSettingsJson(
            """
            {
              "azuread": {
                "clientid": "existing-client-id",
                "signedOutCallbackPath": "/custom-signout"
              }
            }
            """);

        (bool result, _) = await ExecuteAsync(overwrite: true);

        Assert.True(result);
        JsonObject writtenSettings = ParseWrittenSettings().AsObject();
        Assert.False(writtenSettings.ContainsKey("AzureAd"));
        JsonObject azureAd = Assert.IsType<JsonObject>(writtenSettings["azuread"]);
        Assert.Equal(ClientId, azureAd["clientid"]?.ToString());
        Assert.False(azureAd.ContainsKey("ClientId"));
        Assert.Equal(TenantId, azureAd["TenantId"]?.ToString());
        Assert.Equal("/custom-signout", azureAd["signedOutCallbackPath"]?.ToString());
    }

    [Theory]
    [InlineData("""{"AzureAd":{"ClientId":"new-client-id"},"azuread":{"TenantId":"new-tenant-id"}}""", "'AzureAd' and 'azuread'", false)]
    [InlineData("""{"AzureAd":{"ClientId":"new-client-id"},"azuread":{"TenantId":"new-tenant-id"}}""", "'AzureAd' and 'azuread'", true)]
    [InlineData("""{"AzureAd":{"ClientId":"new-client-id","clientId":"other-client-id"}}""", "'ClientId' and 'clientId'", false)]
    [InlineData("""{"AzureAd":{"ClientId":"new-client-id","clientId":"other-client-id"}}""", "'ClientId' and 'clientId'", true)]
    public async Task ExecuteAsync_ReportsAmbiguousCaseVariantsWithoutWrite(string settings, string expectedNames, bool overwrite)
    {
        SetExistingSettingsJson(settings);

        (bool result, TestLogger logger) = await ExecuteAsync(overwrite: overwrite);

        Assert.False(result);
        Assert.Contains(logger.ErrorMessages, message => message.Contains(expectedNames, StringComparison.Ordinal));
        _fileSystem.Verify(fs => fs.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData("AzureAd:Instance", false)]
    [InlineData("AzureAd:Instance", true)]
    [InlineData("AzureAd:TenantId", false)]
    [InlineData("AzureAd:TenantId", true)]
    [InlineData("AzureAd:Domain", false)]
    [InlineData("AzureAd:Domain", true)]
    [InlineData("AzureAd:ClientId", false)]
    [InlineData("AzureAd:ClientId", true)]
    [InlineData("AzureAd:CallbackPath", false)]
    [InlineData("AzureAd:CallbackPath", true)]
    [InlineData("azuread:clientid", false)]
    [InlineData("azuread:clientid", true)]
    public async Task ExecuteAsync_RejectsFlattenedManagedKeysWithoutWrite(string key, bool overwrite)
    {
        SetExistingSettings(new JsonObject { [key] = "existing-value" });
        string originalContent = _files[_developmentSettingsPath];

        (bool result, TestLogger logger) = await ExecuteAsync(overwrite: overwrite);

        Assert.False(result);
        Assert.Contains(logger.ErrorMessages, message => message.Contains($"flattened managed key '{key}'", StringComparison.Ordinal));
        Assert.Contains(logger.ErrorMessages, message => message.Contains("Move this value into a single 'AzureAd' object", StringComparison.Ordinal));
        Assert.Equal(originalContent, _files[_developmentSettingsPath]);
        _fileSystem.Verify(fs => fs.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _fileSystem.Verify(fs => fs.MoveFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteAsync_RejectsFlattenedManagedKeyEvenWhenNestedSettingsMatch(bool overwrite)
    {
        SetExistingSettings(new JsonObject
        {
            ["AzureAd:ClientId"] = ClientId,
            ["AzureAd"] = CreateExpectedAzureAd()
        });

        (bool result, TestLogger logger) = await ExecuteAsync(overwrite: overwrite);

        Assert.False(result);
        Assert.Contains(logger.ErrorMessages, message => message.Contains("flattened managed key 'AzureAd:ClientId'", StringComparison.Ordinal));
        _fileSystem.Verify(fs => fs.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData("developer@contoso.com", null, "contoso.com")]
    [InlineData("developer", null, "developer.onmicrosoft.com")]
    [InlineData("developer@contoso.com", "tenant.onmicrosoft.com", "tenant.onmicrosoft.com")]
    public void CreateAzureAdConfiguration_ResolvesDomain(
        string username,
        string? domain,
        string expectedDomain)
    {
        JsonObject configuration = UpdateAppSettingsStep.CreateAzureAdConfiguration(
            username,
            TenantId,
            ClientId,
            domain);

        Assert.Equal(expectedDomain, configuration["Domain"]?.ToString());
    }

    private async Task<(bool Result, TestLogger Logger)> ExecuteAsync(
        bool overwrite = false,
        string? callbackPath = null)
    {
        var logger = new TestLogger();
        var step = new UpdateAppSettingsStep(
            logger,
            _fileSystem.Object,
            Mock.Of<ITelemetryService>())
        {
            ProjectPath = _projectPath,
            Username = Username,
            TenantId = TenantId,
            Domain = Domain,
            ClientId = ClientId,
            CallbackPath = callbackPath,
            Overwrite = overwrite
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);
        return (result, logger);
    }

    private void SetExistingSettings(JsonObject settings)
    {
        SetExistingSettingsJson(settings.ToJsonString());
    }

    private void SetExistingSettingsJson(string settings)
    {
        _files[_developmentSettingsPath] = settings;
    }

    private JsonNode ParseWrittenSettings()
    {
        Assert.NotNull(_writtenContent);
        JsonNode? settings = JsonNode.Parse(_writtenContent);
        Assert.NotNull(settings);
        return settings;
    }

    private static JsonObject CreateExpectedAzureAd()
    {
        return UpdateAppSettingsStep.CreateAzureAdConfiguration(
            Username,
            TenantId,
            ClientId,
            Domain);
    }

    private static void AssertManagedValuesMatch(JsonNode azureAd)
    {
        JsonObject expected = CreateExpectedAzureAd();
        foreach ((string key, JsonNode? value) in expected)
        {
            Assert.Equal(value?.ToString(), azureAd[key]?.ToString());
        }
    }

    private sealed class TestLogger : ILogger<UpdateAppSettingsStep>
    {
        public List<string> ErrorMessages { get; } = [];
        public List<string> WarningMessages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Error)
            {
                ErrorMessages.Add(formatter(state, exception));
            }
            else if (logLevel == LogLevel.Warning)
            {
                WarningMessages.Add(formatter(state, exception));
            }
        }
    }
}
