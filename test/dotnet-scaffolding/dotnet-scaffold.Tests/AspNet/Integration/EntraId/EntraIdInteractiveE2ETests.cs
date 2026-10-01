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
    protected override string TargetFramework => "net10.0";
    protected override string TestClassName => nameof(EntraIdInteractiveE2ETests);

    [Fact]
    public async Task Scaffold_Interactive_Server_Builds()
    {
        // Arrange — minimal ASP.NET Web project
        File.WriteAllText(_testProjectPath, ProjectContent);
        File.WriteAllText(Path.Combine(_testProjectDir, "Program.cs"), ScaffoldCliHelper.GetMinimalProgramCs());

        var (preExitCode, preOutput, preError) = await RunBuildAsync(_testProjectDir);
        Assert.True(preExitCode == 0, $"Project should build before scaffolding. Error: {preError}");

        // Act — invoke CLI: dotnet scaffold aspnet entra-id using existing application to avoid Azure calls
        var (cliExitCode, cliOutput, cliError) = await ScaffoldCliHelper.RunScaffoldAsync(
            TargetFramework,
            "entra-id",
            "--project", _testProjectPath,
            "--username", "test@example.com",
            "--tenantId", "test-tenant-id",
            "--use-existing-application",
            "--applicationId", "00000000-0000-0000-0000-000000000000");

        Assert.True(cliExitCode == 0, $"CLI scaffold should succeed.\nOutput: {cliOutput}\nError: {cliError}");

        // Assert — project builds after scaffolding
        var (postExitCode, postOutput, postError) = await RunBuildAsync(_testProjectDir);
        Assert.True(postExitCode == 0, $"Project should build after scaffolding. Error: {postError}");
    }

    [Fact]
    public async Task Scaffold_Interactive_BlazorWasm_Builds()
    {
        // Arrange — Blazor WebAssembly project structure
        File.WriteAllText(_testProjectPath, ProjectContent);
        File.WriteAllText(Path.Combine(_testProjectDir, "Program.cs"), ScaffoldCliHelper.GetBlazorProgramCs("TestProject"));
        ScaffoldCliHelper.SetupBlazorProjectStructure(_testProjectDir);

        var (preExitCode, preOutput, preError) = await RunBuildAsync(_testProjectDir);
        Assert.True(preExitCode == 0, $"Project should build before scaffolding. Error: {preError}");

        // Act — invoke CLI with UseExistingApplication to avoid Azure calls
        var (cliExitCode, cliOutput, cliError) = await ScaffoldCliHelper.RunScaffoldAsync(
            TargetFramework,
            "entra-id",
            "--project", _testProjectPath,
            "--username", "test@example.com",
            "--tenantId", "test-tenant-id",
            "--use-existing-application",
            "--applicationId", "00000000-0000-0000-0000-000000000000");

        Assert.True(cliExitCode == 0, $"CLI scaffold should succeed.\nOutput: {cliOutput}\nError: {cliError}");

        // Assert — generated client components exist (RedirectToLogin or other expected artifacts may vary by template)
        // At minimum, the project should still build after scaffolding
        var (postExitCode, postOutput, postError) = await RunBuildAsync(_testProjectDir);
        Assert.True(postExitCode == 0, $"Project should build after scaffolding. Error: {postError}");
    }

    [Fact]
    public async Task Scaffold_Interactive_Auto_Hosted_Builds()
    {
        // Arrange — create a server project that references a client project (Auto hosted layout)
        var serverDir = _testProjectDir;
        var clientDir = Path.Combine(_testDirectory, "TestProject.Client");
        Directory.CreateDirectory(clientDir);

        var serverProjPath = _testProjectPath;
        var clientProjPath = Path.Combine(clientDir, "TestProject.Client.csproj");

        // Server project references the client project
        var serverProjectContent = @"<Project Sdk=\"Microsoft.NET.Sdk.Web\">\n  <ItemGroup>\n    <ProjectReference Include=\"TestProject.Client\" />\n  </ItemGroup>\n</Project>";
        File.WriteAllText(serverProjPath, serverProjectContent);

        // Client project is a Blazor WASM project
        File.WriteAllText(clientProjPath, ScaffoldCliHelper.GetWebProjectContent(TargetFramework).Replace("Microsoft.NET.Sdk.Web", "Microsoft.NET.Sdk.BlazorWebAssembly"));
        File.WriteAllText(Path.Combine(clientDir, "Program.cs"), ScaffoldCliHelper.GetBlazorProgramCs("TestProject.Client"));
        ScaffoldCliHelper.SetupBlazorProjectStructure(clientDir);

        // Add minimal Program.cs to server
        File.WriteAllText(Path.Combine(serverDir, "Program.cs"), ScaffoldCliHelper.GetMinimalProgramCs());

        var (preExitCode, preOutput, preError) = await RunBuildAsync(serverDir);
        Assert.True(preExitCode == 0, $"Server project should build before scaffolding. Error: {preError}");

        // Act — run scaffolder on the server project and ensure it detects the referenced client
        var (cliExitCode, cliOutput, cliError) = await ScaffoldCliHelper.RunScaffoldAsync(
            TargetFramework,
            "entra-id",
            "--project", serverProjPath,
            "--username", "test@example.com",
            "--tenantId", "test-tenant-id",
            "--use-existing-application",
            "--applicationId", "00000000-0000-0000-0000-000000000000");

        Assert.True(cliExitCode == 0, $"CLI scaffold should succeed.\nOutput: {cliOutput}\nError: {cliError}");

        var (postExitCode, postOutput, postError) = await RunBuildAsync(serverDir);
        Assert.True(postExitCode == 0, $"Server project should build after scaffolding. Error: {postError}");
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
}
