// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.Hosting;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Tools.Scaffold.AspNet;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Commands;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Models;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.EntraId;

public class EntraIdTenantIdentityPipelineTests
{
    [Fact]
    public async Task EntraPipeline_TenantMismatchStopsBeforeRegistrationAuthorizationAndSettings()
    {
        var logger = new Mock<ILogger<ValidateAzureCliIdentityStep>>();
        var identity = new MismatchedIdentityStep(logger.Object)
        {
            TenantId = string.Empty,
            Username = string.Empty
        };
        var files = new Mock<IFileSystem>(MockBehavior.Strict);
        var telemetry = Mock.Of<ITelemetryService>();
        var registration = new Mock<RegisterAppStep>(NullLogger<AddClientSecretStep>.Instance, files.Object, telemetry);
        var authorization = new Mock<UpdateAppAuthorizationStep>(NullLogger<UpdateAppAuthorizationStep>.Instance, files.Object, telemetry);
        var settings = new Mock<UpdateAppSettingsStep>(NullLogger<UpdateAppSettingsStep>.Instance, files.Object, telemetry);
        var services = new Dictionary<Type, object>
        {
            [typeof(ValidateAzureCliIdentityStep)] = identity,
            [typeof(RegisterAppStep)] = registration.Object,
            [typeof(UpdateAppAuthorizationStep)] = authorization.Object,
            [typeof(UpdateAppSettingsStep)] = settings.Object
        };
        var provider = new Mock<IServiceProvider>();
        provider.Setup(p => p.GetService(It.IsAny<Type>()))
            .Returns((Type type) => services.TryGetValue(type, out var service) ? service : null);
        var scaffolder = new ScaffoldBuilder("entra-test")
            .WithValidateAzureCliIdentityStep()
            .WithRegisterAppStep()
            .WithUpdateAppAuthorizationStep()
            .WithUpdateAppSettingsStep()
            .Build(provider.Object);
        var context = new ScaffolderContext(scaffolder);
        context.Properties[nameof(EntraIdModel)] = new EntraIdModel
        {
            TenantId = "tenant-a",
            Username = "user@example.com"
        };

        Assert.False(await scaffolder.ExecuteAsync(context));

        registration.Verify(s => s.ExecuteAsync(It.IsAny<ScaffolderContext>(), It.IsAny<CancellationToken>()), Times.Never);
        authorization.Verify(s => s.ExecuteAsync(It.IsAny<ScaffolderContext>(), It.IsAny<CancellationToken>()), Times.Never);
        settings.Verify(s => s.ExecuteAsync(It.IsAny<ScaffolderContext>(), It.IsAny<CancellationToken>()), Times.Never);
        files.VerifyNoOtherCalls();
        Assert.DoesNotContain("ClientId", context.Properties.Keys);
        Assert.Contains(logger.Invocations, invocation => invocation.Method.Name == nameof(ILogger.Log) &&
            invocation.Arguments[2].ToString()!.Contains("az login --tenant tenant-a --allow-no-subscriptions"));
    }

    private sealed class MismatchedIdentityStep(ILogger<ValidateAzureCliIdentityStep> logger)
        : ValidateAzureCliIdentityStep(logger)
    {
        protected override (int ExitCode, string? Output, string? Error) ReadAccount()
            => (0, """{"tenantId":"tenant-b","user":{"name":"user@example.com","type":"user"}}""", null);
    }

    [Fact]
    public void EntraPipeline_RegistersSignInOnlyStepSequence()
    {
        var builder = new ScaffoldRunnerBuilder();
        var service = new AspNetCommandService(builder);

        service.AddScaffolderCommands();

        var scaffolder = Assert.Single(builder.Scaffolders[ScaffolderCatagory.AspNet],
            scaffolder => scaffolder.Name == AspnetStrings.EntraId.Name);
        var steps = scaffolder.StepPreparers.Select(step => step.GetStepType()).ToArray();

        Assert.DoesNotContain(typeof(AddClientSecretStep), steps);
        Assert.Equal(new[]
        {
            typeof(ValidateEntraIdStep), typeof(ValidateAzureCliIdentityStep), typeof(DetectBlazorWasmStep), typeof(RegisterAppStep),
            typeof(UpdateAppAuthorizationStep), typeof(UpdateAppSettingsStep),
            typeof(WrappedAddPackagesStep), typeof(WrappedAddPackagesStep),
            typeof(WrappedCodeModificationStep), typeof(WrappedCodeModificationStep), typeof(WrappedTextTemplatingStep)
        }, steps);
        Assert.Contains(typeof(ValidateAzureCliIdentityStep), service.GetScaffoldSteps());
    }
}
