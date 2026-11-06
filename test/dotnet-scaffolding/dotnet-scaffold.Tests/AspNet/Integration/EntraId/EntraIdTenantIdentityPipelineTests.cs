// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
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
    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(1, 0, false)]
    [InlineData(0, 1, false)]
    public async Task EntraPipeline_ExistingApplicationPersistsSettingsAfterSuccessfulAuthorizationWithWarnings(
        int readExitCode, int updateExitCode, bool expectedSuccess)
    {
        var files = new Mock<IFileSystem>();
        var projectPath = Path.Combine("project", "Test.csproj");
        var settingsPath = Path.Combine("project", "appsettings.Development.json");
        var settingsFiles = new Dictionary<string, string>
        {
            [settingsPath] = """{"Keep":"original"}"""
        };
        files.Setup(f => f.DirectoryExists("project")).Returns(true);
        files.Setup(f => f.FileExists(It.IsAny<string>()))
            .Returns<string>(path => settingsFiles.ContainsKey(path));
        files.Setup(f => f.ReadAllText(It.IsAny<string>()))
            .Returns<string>(path => settingsFiles[path]);
        files.Setup(f => f.WriteAllTextRestricted(It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string>((path, content) => settingsFiles[path] = content);
        string? writtenSettings = null;
        files.Setup(f => f.ReplaceFilePreservingPermissions(It.IsAny<string>(), settingsPath, true))
            .Callback<string, string, bool>((source, destination, _) =>
            {
                writtenSettings = settingsFiles[source];
                settingsFiles[destination] = writtenSettings;
                settingsFiles.Remove(source);
            });
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
        files.Verify(f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        files.Verify(f => f.ReplaceFilePreservingPermissions(It.IsAny<string>(), settingsPath, true),
            expectedSuccess ? Times.Once() : Times.Never());
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
            files.Verify(f => f.WriteAllTextRestricted(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
    }

    private sealed class MatchingIdentityAuthorizationStep(IFileSystem files, ITelemetryService telemetry)
        : UpdateAppAuthorizationStep(NullLogger<UpdateAppAuthorizationStep>.Instance, files, telemetry)
    {
        protected override (int ExitCode, string? Output, string? Error) ReadAccount()
            => (0, """{"tenantId":"tenant-a","user":{"name":"user@example.com","type":"user"}}""", null);
    }

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
