// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.Hosting;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Models;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;
using Microsoft.Extensions.Logging;
using Moq;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Extensions;

public class BlazorEntraScaffolderBuilderExtensionsTests
{
    [Fact]
    public void WithUpdateAppAuthorizationStep_ForwardsSelectedIdentityAndClientId()
    {
        var builder = new Mock<IScaffoldBuilder>();
        Action<ScaffoldStepConfigurator<UpdateAppAuthorizationStep>>? configure = null;
        builder.Setup(b => b.WithStep<UpdateAppAuthorizationStep>(It.IsAny<Action<ScaffoldStepConfigurator<UpdateAppAuthorizationStep>>>()))
            .Callback<Action<ScaffoldStepConfigurator<UpdateAppAuthorizationStep>>, Action<ScaffoldStepConfigurator<UpdateAppAuthorizationStep>>>((action, _) => configure = action)
            .Returns(builder.Object);
        var context = new ScaffolderContext(Mock.Of<IScaffolder>());
        context.Properties[nameof(EntraIdModel)] = new EntraIdModel
        {
            Username = "selected@example.com",
            TenantId = "selected-tenant"
        };
        context.Properties[nameof(EntraIdSettings)] = new EntraIdSettings { Project = "Test.csproj" };
        context.Properties["ClientId"] = "selected-app";
        var step = new UpdateAppAuthorizationStep(NullLogger<UpdateAppAuthorizationStep>.Instance,
            Mock.Of<IFileSystem>(), Mock.Of<ITelemetryService>())
        {
            ProjectPath = string.Empty,
            ClientId = null
        };

        builder.Object.WithUpdateAppAuthorizationStep();
        Assert.NotNull(configure);
        configure(new ScaffoldStepConfigurator<UpdateAppAuthorizationStep> { Step = step, Context = context });

        Assert.Equal("selected-tenant", step.TenantId);
        Assert.Equal("selected@example.com", step.Username);
        Assert.Equal("selected-app", step.ClientId);
        Assert.Equal("Test.csproj", step.ProjectPath);
        Assert.False(step.SkipStep);
    }

    [Fact]
    public void WithValidateAzureCliIdentityStep_UsesSelectedModelIdentity()
    {
        var builder = new Mock<IScaffoldBuilder>();
        Action<ScaffoldStepConfigurator<ValidateAzureCliIdentityStep>>? configure = null;
        builder.Setup(b => b.WithStep<ValidateAzureCliIdentityStep>(It.IsAny<Action<ScaffoldStepConfigurator<ValidateAzureCliIdentityStep>>>()))
            .Callback<Action<ScaffoldStepConfigurator<ValidateAzureCliIdentityStep>>, Action<ScaffoldStepConfigurator<ValidateAzureCliIdentityStep>>>((action, _) => configure = action)
            .Returns(builder.Object);
        var context = new ScaffolderContext(Mock.Of<IScaffolder>());
        context.Properties[nameof(EntraIdModel)] = new EntraIdModel
        {
            Username = "selected@example.com",
            TenantId = "selected-tenant"
        };
        var step = new ValidateAzureCliIdentityStep(NullLogger<ValidateAzureCliIdentityStep>.Instance)
        {
            TenantId = string.Empty,
            Username = string.Empty
        };

        builder.Object.WithValidateAzureCliIdentityStep();
        Assert.NotNull(configure);
        configure(new ScaffoldStepConfigurator<ValidateAzureCliIdentityStep> { Step = step, Context = context });

        Assert.Equal("selected-tenant", step.TenantId);
        Assert.Equal("selected@example.com", step.Username);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WithAddClientSecretStep_ForwardsExistingApplicationSetting(bool useExistingApplication)
    {
        var builder = new Mock<IScaffoldBuilder>();
        Action<ScaffoldStepConfigurator<AddClientSecretStep>>? configure = null;
        builder.Setup(b => b.WithStep<AddClientSecretStep>(It.IsAny<Action<ScaffoldStepConfigurator<AddClientSecretStep>>>()))
            .Callback<Action<ScaffoldStepConfigurator<AddClientSecretStep>>, Action<ScaffoldStepConfigurator<AddClientSecretStep>>>((action, _) => configure = action)
            .Returns(builder.Object);
        var context = new ScaffolderContext(Mock.Of<IScaffolder>());
        context.Properties[nameof(EntraIdSettings)] = new EntraIdSettings
        {
            Project = "Test.csproj",
            Username = "test@example.com",
            TenantId = "tenant",
            UseExistingApplication = useExistingApplication
        };
        context.Properties[nameof(EntraIdModel)] = new EntraIdModel
        {
            Username = "test@example.com",
            TenantId = "tenant"
        };
        var step = new AddClientSecretStep(NullLogger<AddClientSecretStep>.Instance, Mock.Of<IFileSystem>(), Mock.Of<IEnvironmentService>())
        {
            ProjectPath = string.Empty
        };

        builder.Object.WithAddClientSecretStep();
        Assert.NotNull(configure);
        configure(new ScaffoldStepConfigurator<AddClientSecretStep> { Step = step, Context = context });

        Assert.Equal(useExistingApplication, step.UseExistingApplication);
    }

    [Fact]
    public void WithAddClientSecretStep_ReturnsBuilder()
    {
        // Arrange
        Mock<IScaffoldBuilder> mockBuilder = new Mock<IScaffoldBuilder>();
        mockBuilder.Setup(b => b.WithStep<AddClientSecretStep>(It.IsAny<Action<ScaffoldStepConfigurator<AddClientSecretStep>>>()))
            .Returns(mockBuilder.Object);

        // Act
        IScaffoldBuilder result = mockBuilder.Object.WithAddClientSecretStep();

        // Assert
        Assert.NotNull(result);
        mockBuilder.Verify(b => b.WithStep<AddClientSecretStep>(It.IsAny<Action<ScaffoldStepConfigurator<AddClientSecretStep>>>()), Times.Once);
    }

    [Fact]
    public void WithRegisterAppStep_ReturnsBuilder()
    {
        // Arrange
        Mock<IScaffoldBuilder> mockBuilder = new Mock<IScaffoldBuilder>();
        mockBuilder.Setup(b => b.WithStep<RegisterAppStep>(It.IsAny<Action<ScaffoldStepConfigurator<RegisterAppStep>>>()))
            .Returns(mockBuilder.Object);

        // Act
        IScaffoldBuilder result = mockBuilder.Object.WithRegisterAppStep();

        // Assert
        Assert.NotNull(result);
        mockBuilder.Verify(b => b.WithStep<RegisterAppStep>(It.IsAny<Action<ScaffoldStepConfigurator<RegisterAppStep>>>()), Times.Once);
    }

    [Fact]
    public void WithUpdateAppSettingsStep_ReturnsBuilder()
    {
        // Arrange
        Mock<IScaffoldBuilder> mockBuilder = new Mock<IScaffoldBuilder>();
        mockBuilder.Setup(b => b.WithStep<UpdateAppSettingsStep>(It.IsAny<Action<ScaffoldStepConfigurator<UpdateAppSettingsStep>>>()))
            .Returns(mockBuilder.Object);

        // Act
        IScaffoldBuilder result = mockBuilder.Object.WithUpdateAppSettingsStep();

        // Assert
        Assert.NotNull(result);
        mockBuilder.Verify(b => b.WithStep<UpdateAppSettingsStep>(It.IsAny<Action<ScaffoldStepConfigurator<UpdateAppSettingsStep>>>()), Times.Once);
    }

    [Fact]
    public void WithUpdateAppAuthorizationStep_ReturnsBuilder()
    {
        // Arrange
        Mock<IScaffoldBuilder> mockBuilder = new Mock<IScaffoldBuilder>();
        mockBuilder.Setup(b => b.WithStep<UpdateAppAuthorizationStep>(It.IsAny<Action<ScaffoldStepConfigurator<UpdateAppAuthorizationStep>>>()))
            .Returns(mockBuilder.Object);

        // Act
        IScaffoldBuilder result = mockBuilder.Object.WithUpdateAppAuthorizationStep();

        // Assert
        Assert.NotNull(result);
        mockBuilder.Verify(b => b.WithStep<UpdateAppAuthorizationStep>(It.IsAny<Action<ScaffoldStepConfigurator<UpdateAppAuthorizationStep>>>()), Times.Once);
    }

    [Fact]
    public void WithUpdateAppAuthorizationStep_ConfiguresExistingApplication()
    {
        // Arrange
        Action<ScaffoldStepConfigurator<UpdateAppAuthorizationStep>>? configure = null;
        Mock<IScaffoldBuilder> mockBuilder = new Mock<IScaffoldBuilder>();
        mockBuilder
            .Setup(b => b.WithStep<UpdateAppAuthorizationStep>(It.IsAny<Action<ScaffoldStepConfigurator<UpdateAppAuthorizationStep>>>()))
            .Callback<Action<ScaffoldStepConfigurator<UpdateAppAuthorizationStep>>?, Action<ScaffoldStepConfigurator<UpdateAppAuthorizationStep>>?>(
                (preExecute, _) => configure = preExecute)
            .Returns(mockBuilder.Object);

        var scaffolder = new Mock<IScaffolder>();
        scaffolder.SetupGet(value => value.Name).Returns("test");
        scaffolder.SetupGet(value => value.DisplayName).Returns("Test");
        var context = new ScaffolderContext(scaffolder.Object);
        context.Properties[nameof(EntraIdModel)] = new EntraIdModel { UseExistingApplication = true };
        context.Properties[nameof(EntraIdSettings)] = new EntraIdSettings { Project = "TestProject.csproj" };
        context.Properties["ClientId"] = "client-id";

        var step = new UpdateAppAuthorizationStep(
            Mock.Of<ILogger<UpdateAppAuthorizationStep>>(),
            Mock.Of<IFileSystem>(),
            Mock.Of<ITelemetryService>())
        {
            ClientId = null,
            ProjectPath = string.Empty
        };

        // Act
        mockBuilder.Object.WithUpdateAppAuthorizationStep();
        Assert.NotNull(configure);
        configure(new ScaffoldStepConfigurator<UpdateAppAuthorizationStep>
        {
            Context = context,
            Step = step
        });

        // Assert
        Assert.True(step.UseExistingApplication);
        Assert.Equal("client-id", step.ClientId);
    }

    [Fact]
    public void WithDetectBlazorWasmStep_ReturnsBuilder()
    {
        // Arrange
        Mock<IScaffoldBuilder> mockBuilder = new Mock<IScaffoldBuilder>();
        mockBuilder.Setup(b => b.WithStep<DetectBlazorWasmStep>(It.IsAny<Action<ScaffoldStepConfigurator<DetectBlazorWasmStep>>>()))
            .Returns(mockBuilder.Object);

        // Act
        IScaffoldBuilder result = mockBuilder.Object.WithDetectBlazorWasmStep();

        // Assert
        Assert.NotNull(result);
        mockBuilder.Verify(b => b.WithStep<DetectBlazorWasmStep>(It.IsAny<Action<ScaffoldStepConfigurator<DetectBlazorWasmStep>>>()), Times.Once);
    }

    [Fact]
    public void WithEntraAddPackagesStep_ReturnsBuilder()
    {
        // Arrange
        Mock<IScaffoldBuilder> mockBuilder = new Mock<IScaffoldBuilder>();
        mockBuilder.Setup(b => b.WithStep<WrappedAddPackagesStep>(It.IsAny<Action<ScaffoldStepConfigurator<WrappedAddPackagesStep>>>()))
            .Returns(mockBuilder.Object);

        // Act
        IScaffoldBuilder result = mockBuilder.Object.WithEntraAddPackagesStep();

        // Assert
        Assert.NotNull(result);
        mockBuilder.Verify(b => b.WithStep<WrappedAddPackagesStep>(It.IsAny<Action<ScaffoldStepConfigurator<WrappedAddPackagesStep>>>()), Times.Once);
    }

    [Fact]
    public void WithEntraBlazorWasmAddPackagesStep_ReturnsBuilder()
    {
        // Arrange
        Mock<IScaffoldBuilder> mockBuilder = new Mock<IScaffoldBuilder>();
        mockBuilder.Setup(b => b.WithStep<WrappedAddPackagesStep>(It.IsAny<Action<ScaffoldStepConfigurator<WrappedAddPackagesStep>>>()))
            .Returns(mockBuilder.Object);

        // Act
        IScaffoldBuilder result = mockBuilder.Object.WithEntraBlazorWasmAddPackagesStep();

        // Assert
        Assert.NotNull(result);
        mockBuilder.Verify(b => b.WithStep<WrappedAddPackagesStep>(It.IsAny<Action<ScaffoldStepConfigurator<WrappedAddPackagesStep>>>()), Times.Once);
    }

    [Fact]
    public void WithEntraIdCodeChangeStep_ReturnsBuilder()
    {
        // Arrange
        Mock<IScaffoldBuilder> mockBuilder = new Mock<IScaffoldBuilder>();
        mockBuilder.Setup(b => b.WithStep<WrappedCodeModificationStep>(It.IsAny<Action<ScaffoldStepConfigurator<WrappedCodeModificationStep>>>()))
            .Returns(mockBuilder.Object);

        // Act
        IScaffoldBuilder result = mockBuilder.Object.WithEntraIdCodeChangeStep();

        // Assert
        Assert.NotNull(result);
        mockBuilder.Verify(b => b.WithStep<WrappedCodeModificationStep>(It.IsAny<Action<ScaffoldStepConfigurator<WrappedCodeModificationStep>>>()), Times.Once);
    }

    [Fact]
    public void WithEntraIdBlazorWasmCodeChangeStep_ReturnsBuilder()
    {
        // Arrange
        Mock<IScaffoldBuilder> mockBuilder = new Mock<IScaffoldBuilder>();
        mockBuilder.Setup(b => b.WithStep<WrappedCodeModificationStep>(It.IsAny<Action<ScaffoldStepConfigurator<WrappedCodeModificationStep>>>()))
            .Returns(mockBuilder.Object);

        // Act
        IScaffoldBuilder result = mockBuilder.Object.WithEntraIdBlazorWasmCodeChangeStep();

        // Assert
        Assert.NotNull(result);
        mockBuilder.Verify(b => b.WithStep<WrappedCodeModificationStep>(It.IsAny<Action<ScaffoldStepConfigurator<WrappedCodeModificationStep>>>()), Times.Once);
    }

    [Fact]
    public void WithEntraIdTextTemplatingStep_ReturnsBuilder()
    {
        // Arrange
        Mock<IScaffoldBuilder> mockBuilder = new Mock<IScaffoldBuilder>();
        mockBuilder.Setup(b => b.WithStep<WrappedTextTemplatingStep>(It.IsAny<Action<ScaffoldStepConfigurator<WrappedTextTemplatingStep>>>()))
            .Returns(mockBuilder.Object);

        // Act
        IScaffoldBuilder result = mockBuilder.Object.WithEntraIdTextTemplatingStep();

        // Assert
        Assert.NotNull(result);
        mockBuilder.Verify(b => b.WithStep<WrappedTextTemplatingStep>(It.IsAny<Action<ScaffoldStepConfigurator<WrappedTextTemplatingStep>>>()), Times.Once);
    }
}
