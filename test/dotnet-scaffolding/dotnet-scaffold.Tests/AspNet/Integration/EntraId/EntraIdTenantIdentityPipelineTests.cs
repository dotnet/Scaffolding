// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.Hosting;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Tools.Scaffold.AspNet;
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
    public async Task EntraPipeline_AuthorizationFailurePersistsSettingsOnlyForExistingApplication()
    {
        foreach (bool existingApplication in new[] { true, false })
        {
            var path = Path.Combine("project", "appsettings.json");
            var files = new Mock<IFileSystem>();
            files.Setup(f => f.DirectoryExists("project")).Returns(true);
            files.Setup(f => f.EnumerateFiles("project", "appsettings.json", SearchOption.AllDirectories)).Returns(new[] { path });
            files.Setup(f => f.FileExists(path)).Returns(true);
            files.Setup(f => f.ReadAllText(path)).Returns("""{"AllowedHosts":"*"}""");
            string? written = null;
            files.Setup(f => f.WriteAllText(path, It.IsAny<string>())).Callback<string, string>((_, json) => written = json);
            var context = new ScaffolderContext(Mock.Of<IScaffolder>());
            context.Properties[nameof(EntraIdModel)] = new EntraIdModel { TenantId = "tenant-a" };
            context.Properties[nameof(EntraIdSettings)] = new EntraIdSettings
            {
                Project = Path.Combine("project", "Test.csproj"), Username = "user@example.com",
                TenantId = "tenant-a", UseExistingApplication = existingApplication
            };
            context.Properties["ClientId"] = "app-id";
            var steps = new List<Func<Task<bool>>>();
            var builder = new Mock<IScaffoldBuilder>();
            builder.SetReturnsDefault(builder.Object);
            builder.Setup(b => b.WithStep<UpdateAppSettingsStep>(It.IsAny<Action<ScaffoldStepConfigurator<UpdateAppSettingsStep>>>()))
                .Callback<Action<ScaffoldStepConfigurator<UpdateAppSettingsStep>>, Action<ScaffoldStepConfigurator<UpdateAppSettingsStep>>>((configure, _) =>
                    steps.Add(async () =>
                    {
                        var step = new UpdateAppSettingsStep(NullLogger<UpdateAppSettingsStep>.Instance, files.Object, Mock.Of<ITelemetryService>()) { ProjectPath = string.Empty };
                        configure(new ScaffoldStepConfigurator<UpdateAppSettingsStep> { Step = step, Context = context });
                        return step.SkipStep || await step.ExecuteAsync(context);
                    })).Returns(builder.Object);
            builder.Setup(b => b.WithStep<UpdateAppAuthorizationStep>(It.IsAny<Action<ScaffoldStepConfigurator<UpdateAppAuthorizationStep>>>()))
                .Callback(() => steps.Add(() => Task.FromResult(false))).Returns(builder.Object);
            var runner = new Mock<IScaffoldRunnerBuilder>();
            runner.Setup(r => r.AddScaffolder(It.IsAny<ScaffolderCatagory>(), It.IsAny<string>())).Returns(builder.Object);
            new AspNetCommandService(runner.Object).AddScaffolderCommands();

            var succeeded = true;
            foreach (var execute in steps)
            {
                succeeded = await execute();
                if (!succeeded)
                {
                    break;
                }
            }

            Assert.False(succeeded);
            if (existingApplication)
            {
                var settings = JsonNode.Parse(Assert.IsType<string>(written))!;
                Assert.Equal("app-id", settings["AzureAd"]!["ClientId"]!.GetValue<string>());
                Assert.Equal("tenant-a", settings["AzureAd"]!["TenantId"]!.GetValue<string>());
                Assert.Equal("*", settings["AllowedHosts"]!.GetValue<string>());
            }
            else
            {
                Assert.Null(written);
            }
        }
    }

    [Fact]
    public async Task EntraPipeline_TenantMismatchStopsBeforeRegistrationSecretsAuthorizationAndSettings()
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
        var secrets = new Mock<AddClientSecretStep>(NullLogger<AddClientSecretStep>.Instance, files.Object, Mock.Of<IEnvironmentService>());
        var settings = new Mock<UpdateAppSettingsStep>(NullLogger<UpdateAppSettingsStep>.Instance, files.Object, telemetry);
        var services = new Dictionary<Type, object>
        {
            [typeof(ValidateAzureCliIdentityStep)] = identity,
            [typeof(RegisterAppStep)] = registration.Object,
            [typeof(UpdateAppAuthorizationStep)] = authorization.Object,
            [typeof(AddClientSecretStep)] = secrets.Object,
            [typeof(UpdateAppSettingsStep)] = settings.Object
        };
        var provider = new Mock<IServiceProvider>();
        provider.Setup(p => p.GetService(It.IsAny<Type>()))
            .Returns((Type type) => services.TryGetValue(type, out var service) ? service : null);
        var scaffolder = new ScaffoldBuilder("entra-test")
            .WithValidateAzureCliIdentityStep()
            .WithRegisterAppStep()
            .WithUpdateAppAuthorizationStep()
            .WithAddClientSecretStep()
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
        secrets.Verify(s => s.ExecuteAsync(It.IsAny<ScaffolderContext>(), It.IsAny<CancellationToken>()), Times.Never);
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
    public void EntraPipeline_ValidatesIdentityBeforeMutationsAndAuthorizesBeforeSecretAndNewApplicationSettings()
    {
        var builder = new Mock<IScaffoldBuilder>();
        builder.SetReturnsDefault(builder.Object);
        var steps = new List<Type>();
        Capture<ValidateEntraIdStep>(builder, steps);
        Capture<ValidateAzureCliIdentityStep>(builder, steps);
        Capture<DetectBlazorWasmStep>(builder, steps);
        Capture<RegisterAppStep>(builder, steps);
        Capture<UpdateAppAuthorizationStep>(builder, steps);
        Capture<AddClientSecretStep>(builder, steps);
        Capture<UpdateAppSettingsStep>(builder, steps);
        var runner = new Mock<IScaffoldRunnerBuilder>();
        runner.Setup(r => r.AddScaffolder(It.IsAny<ScaffolderCatagory>(), It.IsAny<string>()))
            .Returns(builder.Object);
        var service = new AspNetCommandService(runner.Object);

        service.AddScaffolderCommands();

        Assert.Equal(new[]
        {
            typeof(ValidateEntraIdStep), typeof(ValidateAzureCliIdentityStep), typeof(DetectBlazorWasmStep), typeof(RegisterAppStep),
            typeof(UpdateAppSettingsStep), typeof(UpdateAppAuthorizationStep), typeof(AddClientSecretStep), typeof(UpdateAppSettingsStep)
        }, steps);
        Assert.Contains(typeof(ValidateAzureCliIdentityStep), service.GetScaffoldSteps());
    }

    private static void Capture<TStep>(Mock<IScaffoldBuilder> builder, List<Type> steps) where TStep : ScaffoldStep
    {
        builder.Setup(b => b.WithStep<TStep>(It.IsAny<Action<ScaffoldStepConfigurator<TStep>>>()))
            .Callback(() => steps.Add(typeof(TStep)))
            .Returns(builder.Object);
    }
}
