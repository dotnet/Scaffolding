// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.IO;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.ScaffoldSteps;

public class UpdateAppSettingsStepTests
{
    private const string DefaultInstance = "https://login.microsoftonline.com/";
    private const string DefaultCallbackPath = "/signin-oidc";
    private const string SovereignInstance = "https://login.microsoftonline.us/";
    private const string CustomCallbackPath = "/custom-signin";

    private readonly Mock<IFileSystem> _mockFileSystem = new();
    private readonly ScaffolderContext _context;
    private readonly string _projectPath = Path.Combine("test", "project", "TestProject.csproj");
    private readonly string _appSettingsPath = Path.Combine("test", "project", "appsettings.json");
    private string? _writtenContent;

    public UpdateAppSettingsStepTests()
    {
        Mock<IScaffolder> mockScaffolder = new Mock<IScaffolder>();
        mockScaffolder.Setup(s => s.DisplayName).Returns("TestScaffolder");
        mockScaffolder.Setup(s => s.Name).Returns("test-scaffolder");
        _context = new ScaffolderContext(mockScaffolder.Object);

        _mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(true);
        _mockFileSystem.Setup(fs => fs.EnumerateFiles(
            It.IsAny<string>(),
            "appsettings.json",
            SearchOption.AllDirectories))
            .Returns(new[] { _appSettingsPath });
        _mockFileSystem.Setup(fs => fs.FileExists(_appSettingsPath)).Returns(true);
        _mockFileSystem.Setup(fs => fs.WriteAllText(_appSettingsPath, It.IsAny<string>()))
            .Callback<string, string>((path, content) => _writtenContent = content);
    }

    [Fact]
    public async Task ExecuteAsync_PreservesExistingInstanceAndCallbackPath_WhenNotSupplied()
    {
        JsonNode azureAd = await RunStepAsync(new JsonObject
        {
            ["ClientId"] = "old-client-id",
            ["TenantId"] = "old-tenant-id",
            ["Instance"] = SovereignInstance,
            ["CallbackPath"] = CustomCallbackPath
        });

        Assert.Equal(SovereignInstance, azureAd["Instance"]?.ToString());
        Assert.Equal(CustomCallbackPath, azureAd["CallbackPath"]?.ToString());
        Assert.Equal("new-client-id", azureAd["ClientId"]?.ToString());
        Assert.Equal("new-tenant-id", azureAd["TenantId"]?.ToString());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteAsync_AddsDefaults_WhenInstanceAndCallbackPathAreMissingOrNull(bool presentAsNull)
    {
        JsonObject existingAzureAd = new JsonObject { ["ClientId"] = "old-client-id" };
        if (presentAsNull)
        {
            existingAzureAd["Instance"] = null;
            existingAzureAd["CallbackPath"] = null;
        }

        JsonNode azureAd = await RunStepAsync(existingAzureAd);

        Assert.Equal(DefaultInstance, azureAd["Instance"]?.ToString());
        Assert.Equal(DefaultCallbackPath, azureAd["CallbackPath"]?.ToString());
    }

    [Fact]
    public async Task ExecuteAsync_ReplacesExistingValues_WhenDifferentValuesSupplied()
    {
        JsonNode azureAd = await RunStepAsync(
            new JsonObject
            {
                ["Instance"] = DefaultInstance,
                ["CallbackPath"] = DefaultCallbackPath
            },
            instance: SovereignInstance,
            callbackPath: CustomCallbackPath);

        Assert.Equal(SovereignInstance, azureAd["Instance"]?.ToString());
        Assert.Equal(CustomCallbackPath, azureAd["CallbackPath"]?.ToString());
    }

    [Fact]
    public async Task ExecuteAsync_KeepsExistingValues_WhenSameValuesSupplied()
    {
        JsonNode azureAd = await RunStepAsync(
            new JsonObject
            {
                ["Instance"] = SovereignInstance,
                ["CallbackPath"] = CustomCallbackPath
            },
            instance: SovereignInstance,
            callbackPath: CustomCallbackPath);

        Assert.Equal(SovereignInstance, azureAd["Instance"]?.ToString());
        Assert.Equal(CustomCallbackPath, azureAd["CallbackPath"]?.ToString());
    }

    private async Task<JsonNode> RunStepAsync(JsonObject existingAzureAd, string? instance = null, string? callbackPath = null)
    {
        JsonObject existingContent = new JsonObject { ["AzureAd"] = existingAzureAd };
        _mockFileSystem.Setup(fs => fs.ReadAllText(_appSettingsPath)).Returns(existingContent.ToJsonString());

        UpdateAppSettingsStep step = new UpdateAppSettingsStep(
            NullLogger<UpdateAppSettingsStep>.Instance,
            _mockFileSystem.Object,
            new Mock<ITelemetryService>().Object)
        {
            ProjectPath = _projectPath,
            Username = "user",
            ClientId = "new-client-id",
            TenantId = "new-tenant-id",
            Instance = instance,
            CallbackPath = callbackPath
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.True(result);
        Assert.NotNull(_writtenContent);
        JsonNode? azureAd = JsonNode.Parse(_writtenContent)?["AzureAd"];
        Assert.NotNull(azureAd);
        return azureAd;
    }
}
