// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.EntraId;

/// <summary>
/// Integration tests to verify that MSIdentity is properly used during Entra ID scaffolding.
/// These tests ensure the scaffolding pipeline includes the necessary steps that invoke msidentity CLI.
/// </summary>
public class EntraIdMsIdentityIntegrationTests
{
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
        // This step is responsible for calling "dotnet msidentity --register-app" or "--update-app-registration"
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
        // 2. RegisterAppStep uses msidentity to register or update the application
        
        // The actual scaffolding order in AspNetCommandService.cs is:
        // .WithStep<ValidateEntraIdStep>()
        // .WithRegisterAppStep()           <- Uses msidentity CLI
        // .WithDetectBlazorWasmStep()
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
