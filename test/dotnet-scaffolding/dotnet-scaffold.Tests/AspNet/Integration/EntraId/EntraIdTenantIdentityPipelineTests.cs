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
    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(1, 0, false)]
    [InlineData(0, 1, false)]
    public async Task EntraPipeline_ExistingApplicationPersistsSettingsAfterSuccessfulAuthorizationWithWarnings(
        int readExitCode, int updateExitCode, bool expectedSuccess)
    {
        var files = new Mock<IFileSystem>();
        var projectPath = Path.Combine("project", "Test.csproj");
        var settingsPath = Path.Combine("project", "appsettings.json");
        files.Setup(f => f.FileExists(projectPath)).Returns(true);
        files.Setup(f => f.DirectoryExists("project")).Returns(true);
        files.Setup(f => f.EnumerateFiles("project", "appsettings.json", SearchOption.AllDirectories))
            .Returns(new[] { settingsPath });
        files.Setup(f => f.FileExists(settingsPath)).Returns(true);
        files.Setup(f => f.ReadAllText(settingsPath)).Returns("""{"Keep":"original"}""");
        string? writtenSettings = null;
        files.Setup(f => f.WriteAllText(settingsPath, It.IsAny<string>()))
            .Callback((string _, string content) => writtenSettings = content);
        var telemetry = Mock.Of<ITelemetryService>();
        var authorization = new MatchingIdentityAuthorizationStep(files.Object, telemetry)
        {
            ProjectPath = string.Empty,
            ClientId = null,
            AutoConfigureLocalUrls = false,
            WebRedirectUris = ["https://localhost:5001/signin-oidc"]
        };
        var commands = new List<string>();
        authorization.AzCliCommand = command =>
        {
            commands.Add(command);
            return command.StartsWith("ad app show", StringComparison.Ordinal)
                ? new(readExitCode, """["https://example.com/signin-oidc"]""", "Azure CLI read diagnostic")
                : new(updateExitCode, null, "Azure CLI update diagnostic");
        };
        var registration = new Mock<RegisterAppStep>(MockBehavior.Strict,
            NullLogger<AddClientSecretStep>.Instance, files.Object, telemetry);
        var services = new Dictionary<Type, object>
        {
            [typeof(RegisterAppStep)] = registration.Object,
            [typeof(UpdateAppAuthorizationStep)] = authorization,
            [typeof(AddClientSecretStep)] = new AddClientSecretStep(
                NullLogger<AddClientSecretStep>.Instance, files.Object, Mock.Of<IEnvironmentService>())
                { ProjectPath = string.Empty },
            [typeof(UpdateAppSettingsStep)] = new UpdateAppSettingsStep(
                NullLogger<UpdateAppSettingsStep>.Instance, files.Object, telemetry)
                { ProjectPath = string.Empty }
        };
        var provider = new Mock<IServiceProvider>();
        provider.Setup(p => p.GetService(It.IsAny<Type>()))
            .Returns((Type type) => services.TryGetValue(type, out var service) ? service : null);
        var scaffolder = new ScaffoldBuilder("entra-test")
            .WithRegisterAppStep()
            .WithUpdateAppAuthorizationStep()
            .WithAddClientSecretStep()
            .WithUpdateAppSettingsStep()
            .Build(provider.Object);
        var context = new ScaffolderContext(scaffolder);
        context.Properties[nameof(EntraIdSettings)] = new EntraIdSettings
        {
            Project = projectPath,
            Username = "user@example.com",
            TenantId = "tenant-a",
            UseExistingApplication = true,
            Application = "Existing Application app-id"
        };
        context.Properties[nameof(EntraIdModel)] = new EntraIdModel
        {
            Username = "user@example.com",
            TenantId = "tenant-a",
            UseExistingApplication = true
        };

        Assert.Equal(expectedSuccess, await scaffolder.ExecuteAsync(context));

        registration.Verify(s => s.ExecuteAsync(It.IsAny<ScaffolderContext>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.False(context.Properties.ContainsKey("ClientSecret"));
        Assert.Equal(readExitCode == 0 ? 2 : 1, commands.Count);
        if (readExitCode == 0)
        {
            Assert.Contains("\"https://example.com/signin-oidc\"", commands[1]);
            Assert.Contains("\"https://localhost:5001/signin-oidc\"", commands[1]);
        }
        if (expectedSuccess)
        {
            var settings = JsonNode.Parse(Assert.IsType<string>(writtenSettings))!;
            Assert.Equal("app-id", settings["AzureAd"]!["ClientId"]!.GetValue<string>());
            Assert.Equal("tenant-a", settings["AzureAd"]!["TenantId"]!.GetValue<string>());
            Assert.Equal("original", settings["Keep"]!.GetValue<string>());
        }
        else
        {
            Assert.Null(writtenSettings);
            files.Verify(f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
    }

    private sealed class MatchingIdentityAuthorizationStep(IFileSystem files, ITelemetryService telemetry)
        : UpdateAppAuthorizationStep(NullLogger<UpdateAppAuthorizationStep>.Instance, files, telemetry)
    {
        protected override (int ExitCode, string? Output, string? Error) ReadAccount()
            => (0, """{"tenantId":"tenant-a","user":{"name":"user@example.com","type":"user"}}""", null);
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
    public void EntraPipeline_ValidatesIdentityBeforeMutationsAndDoesNotCreateClientSecrets()
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
            typeof(UpdateAppAuthorizationStep), typeof(UpdateAppSettingsStep)
        }, steps);
        Assert.DoesNotContain(typeof(AddClientSecretStep), steps);
        Assert.Contains(typeof(ValidateAzureCliIdentityStep), service.GetScaffoldSteps());
    }

    private static void Capture<TStep>(Mock<IScaffoldBuilder> builder, List<Type> steps) where TStep : ScaffoldStep
    {
        builder.Setup(b => b.WithStep<TStep>(It.IsAny<Action<ScaffoldStepConfigurator<TStep>>>()))
            .Callback(() => steps.Add(typeof(TStep)))
            .Returns(builder.Object);
    }
}
