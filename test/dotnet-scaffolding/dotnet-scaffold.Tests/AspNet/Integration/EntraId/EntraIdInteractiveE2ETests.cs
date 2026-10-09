// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.EntraId;

public class EntraIdInteractiveE2ETests : EntraIdIntegrationTestsBase
{
    private const string RunExternalE2EEnvVar = "DOTNET_SCAFFOLD_RUN_ENTRA_E2E";
    private const string UsernameEnvVar = "DOTNET_SCAFFOLD_ENTRA_USERNAME";
    private const string TenantIdEnvVar = "DOTNET_SCAFFOLD_ENTRA_TENANT_ID";
    private const string ApplicationIdEnvVar = "DOTNET_SCAFFOLD_ENTRA_APPLICATION_ID";

    protected override string TargetFramework => "net10.0";
    protected override string TestClassName => nameof(EntraIdInteractiveE2ETests);

    [SkippableFact]
    public async Task Scaffold_Interactive_Server_Builds()
    {
        var entraSettings = GetRequiredExternalEntraSettings();

        // Arrange — minimal ASP.NET Web project
        File.WriteAllText(_testProjectPath, ProjectContent);
        File.WriteAllText(Path.Combine(_testProjectDir, "Program.cs"), ScaffoldCliHelper.GetMinimalProgramCs());

        var (preExitCode, preOutput, preError) = await RunBuildAsync(_testProjectDir);
        Assert.True(preExitCode == 0, $"Project should build before scaffolding. Error: {preError}");

        // Act — invoke CLI: dotnet scaffold aspnet entra-id using a real existing application.
        var (cliExitCode, cliOutput, cliError) = await ScaffoldCliHelper.RunScaffoldAsync(
            TargetFramework,
            "entra-id",
            "--project", _testProjectPath,
            "--username", entraSettings.Username,
            "--tenantId", entraSettings.TenantId,
            "--use-existing-application",
            "--applicationId", entraSettings.ApplicationId);

        Assert.True(cliExitCode == 0, $"CLI scaffold should succeed.\nOutput: {cliOutput}\nError: {cliError}");

        // Assert — project builds after scaffolding
        var (postExitCode, postOutput, postError) = await RunBuildAsync(_testProjectDir);
        Assert.True(postExitCode == 0, $"Project should build after scaffolding. Error: {postError}");
    }

    [SkippableTheory]
    [InlineData("WebAssembly")]
    [InlineData("Auto")]
    public async Task Scaffold_Interactive_HostedClient_RewritesRoutesAndBuilds(string interactivity)
    {
        var entraSettings = GetRequiredExternalEntraSettings();

        var created = await ScaffoldCliHelper.RunDotNetAsync(
            _testDirectory, "new", "blazor", "-n", "TestProject", "-o", _testDirectory,
            "-f", TargetFramework, "-int", interactivity, "-ai", "--no-restore");
        Assert.True(created.ExitCode == 0, $"dotnet new failed: {created.Output}\n{created.Error}");
        string clientDirectory = Path.Combine(_testDirectory, "TestProject.Client");
        string clientProjectPath = Path.Combine(clientDirectory, "TestProject.Client.csproj");
        string routesPath = Path.Combine(clientDirectory, "Routes.razor");
        Assert.Contains("Microsoft.NET.Sdk.BlazorWebAssembly", File.ReadAllText(clientProjectPath));
        Assert.Contains("<RouteView ", File.ReadAllText(routesPath));
        Assert.False(File.Exists(Path.Combine(clientDirectory, "RedirectToLogin.razor")));

        var (preExitCode, preOutput, preError) = await RunBuildAsync(_testProjectDir);
        Assert.True(preExitCode == 0, $"Project should build before scaffolding. Error: {preError}");

        // Act — invoke CLI with a real existing application.
        var (cliExitCode, cliOutput, cliError) = await ScaffoldCliHelper.RunScaffoldAsync(
            TargetFramework,
            "entra-id",
            "--project", _testProjectPath,
            "--username", entraSettings.Username,
            "--tenantId", entraSettings.TenantId,
            "--use-existing-application",
            "--applicationId", entraSettings.ApplicationId);

        Assert.True(cliExitCode == 0, $"CLI scaffold should succeed.\nOutput: {cliOutput}\nError: {cliError}");

        string routes = File.ReadAllText(routesPath);
        Assert.Contains("<AuthorizeRouteView", routes);
        Assert.Contains("<NotAuthorized>", routes);
        Assert.Contains("<RedirectToLogin />", routes);
        Assert.DoesNotContain("<RouteView ", routes);
        Assert.Contains("authentication/login?returnUrl=",
            File.ReadAllText(Path.Combine(clientDirectory, "RedirectToLogin.razor")));
        Assert.Contains("AddAuthenticationStateDeserialization",
            File.ReadAllText(Path.Combine(clientDirectory, "Program.cs")));
        Assert.Contains("AddAuthenticationStateSerialization",
            File.ReadAllText(Path.Combine(_testProjectDir, "Program.cs")));
        var (postExitCode, postOutput, postError) = await RunBuildAsync(_testProjectDir);
        Assert.True(postExitCode == 0, $"Project should build after scaffolding. Error: {postError}");
    }

    [Fact]
    public async Task UpdateAppAuthorizationStep_Fails_When_AzureCliMissing()
    {
        // Arrange — create step pointing to a (fake) project and provide a client id
        var fileSystem = new Mock<Microsoft.DotNet.Scaffolding.Internal.Services.IFileSystem>();
        fileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(false);
        var telemetry = new TestTelemetryService();
        var logger = Mock.Of<ILogger<UpdateAppAuthorizationStep>>();

        var step = new UpdateAppAuthorizationStep(logger, fileSystem.Object, telemetry)
        {
            ProjectPath = _testProjectPath,
            ClientId = "00000000-0000-0000-0000-000000000000",
            AutoConfigureLocalUrls = false
        };

        // Temporarily clear PATH so AzCliRunner cannot find the az executable
        var originalPath = Environment.GetEnvironmentVariable("PATH");
        try
        {
            Environment.SetEnvironmentVariable("PATH", string.Empty);
            var result = await step.ExecuteAsync(_context, default);
            Assert.False(result, "UpdateAppAuthorizationStep should fail when Azure CLI is missing.");
        }
        finally
        {
            Environment.SetEnvironmentVariable("PATH", originalPath);
        }
    }

    private static (string Username, string TenantId, string ApplicationId) GetRequiredExternalEntraSettings()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(RunExternalE2EEnvVar), "1", StringComparison.Ordinal))
        {
            throw new Xunit.SkipException(
                $"Set {RunExternalE2EEnvVar}=1 and provide {UsernameEnvVar}, {TenantIdEnvVar}, and {ApplicationIdEnvVar} to run external Entra ID E2E tests.");
        }

        var username = Environment.GetEnvironmentVariable(UsernameEnvVar);
        var tenantId = Environment.GetEnvironmentVariable(TenantIdEnvVar);
        var applicationId = Environment.GetEnvironmentVariable(ApplicationIdEnvVar);

        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(tenantId) ||
            string.IsNullOrWhiteSpace(applicationId))
        {
            throw new Xunit.SkipException(
                $"External Entra ID E2E tests require {UsernameEnvVar}, {TenantIdEnvVar}, and {ApplicationIdEnvVar}.");
        }

        return (username, tenantId, applicationId);
    }
}
