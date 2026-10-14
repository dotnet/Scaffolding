// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.ScaffoldSteps;

public class UpdateAppAuthorizationStepTests
{
    private readonly Mock<IFileSystem> _fileSystem = new();
    private readonly UpdateAppAuthorizationStep _step;

    public UpdateAppAuthorizationStepTests()
    {
        _step = new UpdateAppAuthorizationStep(
            Mock.Of<ILogger<UpdateAppAuthorizationStep>>(),
            _fileSystem.Object,
            Mock.Of<ITelemetryService>())
        {
            ClientId = "client-id",
            ProjectPath = Path.Combine("test", "TestProject.csproj")
        };
    }

    [Fact]
    public async Task ExecuteAsync_PreservesExistingWebRedirectUris()
    {
        // Arrange
        var commands = new List<string>();
        _step.AutoConfigureLocalUrls = false;
        _step.UseExistingApplication = true;
        _step.WebRedirectUris = ["https://localhost:7188/signin-oidc"];
        _step.AzCliCommand = command =>
        {
            commands.Add(command);
            return command.StartsWith("ad app show", StringComparison.Ordinal)
                ? new(0, """["https://production.contoso.com/signin-oidc","HTTPS://LOCALHOST:7188/signin-oidc"]""", null)
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
                Assert.Contains("\"HTTPS://LOCALHOST:7188/signin-oidc\"", command);
                Assert.Equal(
                    1,
                    CountOccurrences(
                        command,
                        "https://localhost:7188/signin-oidc",
                        StringComparison.OrdinalIgnoreCase));
            });
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
