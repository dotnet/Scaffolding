// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.ScaffoldSteps;

public class UpdateAppAuthorizationStepTests
{
    [Fact]
    public async Task ExecuteAsync_RechecksIdentityBeforeEachUpdate()
    {
        var step = CreateStep();
        var context = new ScaffolderContext(Mock.Of<IScaffolder>());
        Assert.True(await step.ExecuteAsync(context));
        Assert.Equal(1, step.Updates);

        step.AccountOutput = """{"tenantId":"tenant-b","user":{"name":"user@example.com","type":"user"}}""";
        Assert.False(await step.ExecuteAsync(context));
        Assert.Equal(1, step.Updates);
        Assert.Equal(2, step.AccountReads);
    }

    [Theory]
    [InlineData(0, null, true)]
    [InlineData(0, "CLI warning", true)]
    [InlineData(1, "Update failed", false)]
    public async Task ExecuteAsync_UsesExitCodeAndPreservesAuthorizationArguments(int exitCode, string? error, bool expected)
    {
        var step = CreateStep();
        step.ExitCode = exitCode;
        step.Error = error;

        Assert.Equal(expected, await step.ExecuteAsync(new ScaffolderContext(Mock.Of<IScaffolder>())));
        Assert.Equal("ad app update --id app-id --enable-id-token-issuance true --web-redirect-uris \"https://localhost:5001/signin-oidc\"",
            step.Command);
    }

    private static TestStep CreateStep() => new()
    {
        ProjectPath = "Test.csproj",
        ClientId = "app-id",
        TenantId = "tenant-a",
        Username = "user@example.com",
        AutoConfigureLocalUrls = false,
        WebRedirectUris = ["https://localhost:5001/signin-oidc"]
    };

    private sealed class TestStep(IFileSystem? fileSystem = null) : UpdateAppAuthorizationStep(
        NullLogger<UpdateAppAuthorizationStep>.Instance, fileSystem ?? Mock.Of<IFileSystem>(), Mock.Of<ITelemetryService>())
    {
        public string AccountOutput { get; set; } =
            """{"tenantId":"tenant-a","user":{"name":"user@example.com","type":"user"}}""";
        public int AccountReads { get; private set; }
        public int Updates { get; private set; }
        public int ExitCode { get; set; }
        public string? Error { get; set; }
        public string? Command { get; private set; }

        protected override (int ExitCode, string? Output, string? Error) ReadAccount()
        {
            AccountReads++;
            return (0, AccountOutput, null);
        }

        protected override (int ExitCode, string? Error) RunUpdate(string command)
        {
            Assert.True(AccountReads > Updates);
            Command = command;
            Updates++;
            return AzCliCommand is not null ? base.RunUpdate(command) : (ExitCode, Error);
        }
    }

    private readonly Mock<IFileSystem> _fileSystem = new();
    private readonly TestStep _step;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteAsync_DoesNotUpdateExistingApplicationWhenIdentityChanges(bool duringRead)
    {
        var commands = new List<string>();
        const string mismatchedAccount = """{"tenantId":"tenant-b","user":{"name":"user@example.com","type":"user"}}""";
        _step.AutoConfigureLocalUrls = false;
        _step.UseExistingApplication = true;
        if (!duringRead)
        {
            _step.AccountOutput = mismatchedAccount;
        }
        _step.AzCliCommand = command =>
        {
            commands.Add(command);
            _step.AccountOutput = mismatchedAccount;
            return new(0, "[]", null);
        };

        Assert.False(await _step.ExecuteAsync(CreateContext()));

        Assert.Equal(duringRead ? 2 : 1, _step.AccountReads);
        Assert.Equal(duringRead ? 1 : 0, commands.Count);
        if (duringRead)
        {
            Assert.StartsWith("ad app show", commands[0]);
        }
        Assert.Equal(0, _step.Updates);
    }

    public UpdateAppAuthorizationStepTests()
    {
        _step = new TestStep(_fileSystem.Object)
        {
            ClientId = "client-id",
            ProjectPath = Path.Combine("test", "TestProject.csproj"),
            TenantId = "tenant-a",
            Username = "user@example.com"
        };
    }

    [Fact]
    public async Task ExecuteAsync_PreservesExistingWebRedirectUris()
    {
        // Arrange
        var commands = new List<string>();
        _step.AutoConfigureLocalUrls = false;
        _step.UseExistingApplication = true;
        _step.WebRedirectUris = ["https://localhost:7188/signin-oidc", "http://localhost:5012/signin-oidc"];
        _step.AzCliCommand = command =>
        {
            commands.Add(command);
            return command.StartsWith("ad app show", StringComparison.Ordinal)
                ? new(0, """["https://production.contoso.com/signin-oidc","https://localhost:7188/signin-oidc"]""", null)
                : new(0, null, null);
        };

        // Act
        bool result = await _step.ExecuteAsync(CreateContext());

        // Assert
        Assert.True(result);
        Assert.Collection(
            commands,
            command => Assert.Equal(
                "ad app show --id client-id --query web.redirectUris --output json",
                command),
            command =>
            {
                Assert.Contains("\"https://production.contoso.com/signin-oidc\"", command);
                Assert.Contains("\"https://localhost:7188/signin-oidc\"", command);
                Assert.Contains("\"http://localhost:5012/signin-oidc\"", command);
                Assert.Equal(
                    1,
                    CountOccurrences(
                        command,
                        "https://localhost:7188/signin-oidc",
                        StringComparison.Ordinal));
            });
    }

    [Fact]
    public async Task ExecuteAsync_PreservesExistingWebRedirectUrisThatDifferOnlyByPathCase()
    {
        // Arrange
        string? updateCommand = null;
        _step.AutoConfigureLocalUrls = false;
        _step.UseExistingApplication = true;
        _step.AzCliCommand = command =>
        {
            if (command.StartsWith("ad app show", StringComparison.Ordinal))
            {
                return new(
                    0,
                    """["https://example.com/App/signin-oidc","https://example.com/app/signin-oidc"]""",
                    null);
            }

            updateCommand = command;
            return new(0, null, null);
        };

        // Act
        bool result = await _step.ExecuteAsync(CreateContext());

        // Assert
        Assert.True(result);
        Assert.NotNull(updateCommand);
        Assert.Contains("\"https://example.com/App/signin-oidc\"", updateCommand);
        Assert.Contains("\"https://example.com/app/signin-oidc\"", updateCommand);
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotUpdateWhenExistingWebRedirectUrisCannotBeRead()
    {
        // Arrange
        var commands = new List<string>();
        _step.AutoConfigureLocalUrls = false;
        _step.UseExistingApplication = true;
        _step.WebRedirectUris = ["https://localhost:7188/signin-oidc"];
        _step.AzCliCommand = command =>
        {
            commands.Add(command);
            return new(1, null, "Unable to read the app registration.");
        };

        // Act
        bool result = await _step.ExecuteAsync(CreateContext());

        // Assert
        Assert.False(result);
        Assert.Single(commands);
        Assert.StartsWith("ad app show", commands[0]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t\r\n")]
    [InlineData("null")]
    [InlineData("[")]
    [InlineData("{}")]
    [InlineData("\"https://production.contoso.com/signin-oidc\"")]
    public async Task ExecuteAsync_DoesNotUpdateWhenExistingWebRedirectUriOutputIsInvalid(string? output)
    {
        // Arrange
        var commands = new List<string>();
        _step.AutoConfigureLocalUrls = false;
        _step.UseExistingApplication = true;
        _step.WebRedirectUris = ["https://localhost:7188/signin-oidc"];
        _step.AzCliCommand = command =>
        {
            commands.Add(command);
            return new(0, output, null);
        };

        // Act
        bool result = await _step.ExecuteAsync(CreateContext());

        // Assert
        Assert.False(result);
        Assert.Collection(
            commands,
            command => Assert.Equal(
                "ad app show --id client-id --query web.redirectUris --output json",
                command));
    }

    [Fact]
    public async Task ExecuteAsync_AddsOnlyCallbackUrisFromLaunchSettings()
    {
        // Arrange
        string launchSettingsPath = Path.Combine("test", "Properties", "launchSettings.json");
        _fileSystem.Setup(fileSystem => fileSystem.FileExists(launchSettingsPath)).Returns(true);
        _fileSystem.Setup(fileSystem => fileSystem.ReadAllText(launchSettingsPath)).Returns(
            """
            {
              "profiles": {
                "http": {
                  "applicationUrl": "http://localhost:5012;https://localhost:7188/"
                }
              }
            }
            """);

        string? updateCommand = null;
        _step.AzCliCommand = command =>
        {
            updateCommand = command;
            return new(0, null, null);
        };

        // Act
        bool result = await _step.ExecuteAsync(CreateContext());

        // Assert
        Assert.True(result);
        Assert.NotNull(updateCommand);
        Assert.Contains("\"http://localhost:5012/signin-oidc\"", updateCommand);
        Assert.Contains("\"https://localhost:7188/signin-oidc\"", updateCommand);
        Assert.DoesNotContain("\"http://localhost:5012\"", updateCommand);
        Assert.DoesNotContain("\"https://localhost:7188/\"", updateCommand);
    }

    private static ScaffolderContext CreateContext()
    {
        var scaffolder = new Mock<IScaffolder>();
        scaffolder.SetupGet(value => value.Name).Returns("test");
        scaffolder.SetupGet(value => value.DisplayName).Returns("Test");
        return new ScaffolderContext(scaffolder.Object);
    }

    private static int CountOccurrences(string value, string searchValue, StringComparison comparison)
    {
        int count = 0;
        int index = 0;
        while ((index = value.IndexOf(searchValue, index, comparison)) >= 0)
        {
            count++;
            index += searchValue.Length;
        }

        return count;
    }
}
