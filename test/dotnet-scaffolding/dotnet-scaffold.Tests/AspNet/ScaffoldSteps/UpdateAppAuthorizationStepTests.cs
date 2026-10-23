// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.ScaffoldSteps;

public class UpdateAppAuthorizationStepTests
{
    [Fact]
    public void ConfigureLocalRedirectUris_UsesConfiguredCallbackPath()
    {
        string projectPath = Path.Combine("test", "project", "TestProject.csproj");
        string launchSettingsPath = Path.Combine("test", "project", "Properties", "launchSettings.json");
        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(fs => fs.FileExists(launchSettingsPath)).Returns(true);
        fileSystem.Setup(fs => fs.ReadAllText(launchSettingsPath)).Returns(
            """
            {
              "profiles": {
                "https": {
                  "applicationUrl": "https://localhost:7001"
                }
              }
            }
            """);
        var step = new UpdateAppAuthorizationStep(
            NullLogger<UpdateAppAuthorizationStep>.Instance,
            fileSystem.Object,
            Mock.Of<ITelemetryService>())
        {
            ProjectPath = projectPath,
            ClientId = "client-id",
            CallbackPath = "/custom-signin"
        };

        step.ConfigureLocalRedirectUris(projectPath);

        Assert.Contains("https://localhost:7001/custom-signin", step.WebRedirectUris!);
        Assert.DoesNotContain("https://localhost:7001/signin-oidc", step.WebRedirectUris!);
    }
}
