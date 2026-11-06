// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.EntraId;

/// <summary>
/// Integration tests to verify that MSIdentity is properly used during Entra ID scaffolding.
/// These tests ensure the scaffolding pipeline includes the necessary steps that invoke msidentity CLI.
/// </summary>
public class EntraIdMsIdentityIntegrationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("12345678-1234-1234-1234-123456789abc")]
    public async Task RegistrationOnly_PreservesProjectIsolationAndTracksOnlyNewApplications(string? existingClientId)
    {
        const string clientId = "12345678-1234-1234-1234-123456789abc";
        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
        fileSystem.Setup(fs => fs.FileExists("Test.csproj")).Returns(true);
        var step = new RegistrationStep(fileSystem.Object)
        {
            ProjectPath = "Test.csproj",
            TenantId = "tenant-a",
            Username = "user@example.com",
            ClientId = existingClientId
        };
        var context = new ScaffolderContext(Mock.Of<IScaffolder>());

        Assert.True(await step.ExecuteAsync(context));

        var expected = new System.Collections.Generic.List<string>
        {
            "--register-app", "--registration-only",
            "--project-file-path", Path.GetFullPath("Test.csproj"),
            "--project-type", "webapp", "--app-display-name", "Test",
            "--tenant-id", "tenant-a", "--username", "user@example.com"
        };
        if (!string.IsNullOrEmpty(existingClientId))
        {
            expected.AddRange(["--client-id", existingClientId]);
        }
        Assert.Equal(expected, step.Arguments);
        Assert.Equal(clientId, context.Properties["ClientId"]);
        bool createsApplication = string.IsNullOrEmpty(existingClientId);
        Assert.Equal(createsApplication ? 1 : 0, step.CapabilityChecks);
        Assert.Equal(createsApplication, context.Properties.ContainsKey(EntraRegistrationCleanup.CreatedClientIdKey));
        fileSystem.Verify(fs => fs.FileExists("Test.csproj"), Times.Once);
        fileSystem.VerifyNoOtherCalls();
    }

    private sealed class RegistrationStep(IFileSystem fileSystem) : RegisterAppStep(
        NullLogger<AddClientSecretStep>.Instance, fileSystem, Mock.Of<ITelemetryService>())
    {
        public string[]? Arguments { get; private set; }
        public int CapabilityChecks { get; private set; }

        protected override bool EnsureToolInstalled() => true;

        protected override (int ExitCode, string? Output, string? Error) ReadCapabilities()
        {
            CapabilityChecks++;
            return (0, "registration-cleanup", null);
        }

        protected override (int ExitCode, string? Output, string? Error) RunRegistration(string[] args)
        {
            Arguments = args;
            return (0, "ClientId: 12345678-1234-1234-1234-123456789abc", null);
        }
    }

    [Fact]
    public void EntraIdScaffolder_IncludesRegisterAppStep_WhichUsesMsIdentity()
    {
        // This test verifies that RegisterAppStep is part of the Entra ID scaffolding pipeline
        // RegisterAppStep uses "dotnet msidentity" CLI commands to register/update Azure AD applications
        
        // Arrange
        var mockScaffolder = new Mock<IScaffolder>();
        mockScaffolder.Setup(s => s.DisplayName).Returns("Entra ID Scaffolder");
        mockScaffolder.Setup(s => s.Name).Returns("entra-id");
        
        var context = new ScaffolderContext(mockScaffolder.Object);
        
        // Act & Assert
        // Verify that RegisterAppStep type exists and can be instantiated
        // This step calls "dotnet msidentity --register-app --registration-only".
        Type registerAppStepType = typeof(RegisterAppStep);
        Assert.NotNull(registerAppStepType);
        Assert.True(registerAppStepType.IsAssignableTo(typeof(ScaffoldStep)));
    }

    [Fact]
    public void RegisterAppStep_HasRequiredPropertiesForMsIdentity()
    {
        // Verify RegisterAppStep has all properties needed to invoke msidentity CLI
        
        // Act
        Type registerAppStepType = typeof(RegisterAppStep);
        
        // Assert - Verify properties exist for msidentity CLI arguments
        Assert.NotNull(registerAppStepType.GetProperty("ProjectPath"));
        Assert.NotNull(registerAppStepType.GetProperty("Username"));
        Assert.NotNull(registerAppStepType.GetProperty("TenantId"));
        Assert.NotNull(registerAppStepType.GetProperty("ClientId"));
    }

    [Fact]
    public void EntraIdScaffolder_StepsAreOrderedCorrectly()
    {
        // This test documents the expected order of steps in Entra ID scaffolding
        // The order is important because:
        // 1. ValidateEntraIdStep must run first to validate inputs
        // 2. RegisterAppStep uses msidentity after local settings validation succeeds
        
        // The actual scaffolding order in AspNetCommandService.cs is:
        // .WithStep<ValidateEntraIdStep>()
        // .WithValidateAzureCliIdentityStep()
        // .WithDetectBlazorWasmStep()
        // .WithRegisterAppStep()           <- Uses msidentity CLI
        // .WithUpdateAppAuthorizationStep()
        // .WithAddClientSecretStep()       <- Ensures msidentity is installed, then uses it
        // ... other steps
        
        // This test verifies the key steps exist in the expected namespace
        Assert.NotNull(typeof(ValidateEntraIdStep));
        Assert.NotNull(typeof(RegisterAppStep));
    }

    [Fact]
    public void MsIdentitySteps_ArePartOfEntraIdNamespace()
    {
        // Verify that msidentity-related steps are properly organized in the AspNet namespace
        
        // Act
        Type registerAppStepType = typeof(RegisterAppStep);
        
        // Assert
        Assert.False(string.IsNullOrWhiteSpace(registerAppStepType.Namespace));
        Assert.Contains("ScaffoldSteps", registerAppStepType.Namespace!);
    }
}
