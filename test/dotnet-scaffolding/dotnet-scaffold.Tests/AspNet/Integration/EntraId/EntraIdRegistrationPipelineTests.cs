// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.DotNet.MSIdentity;
using Microsoft.DotNet.MSIdentity.AuthenticationParameters;
using Microsoft.DotNet.MSIdentity.Tool;
using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.Hosting;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Models;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using IdentityProgram = Microsoft.DotNet.MSIdentity.Tool.Program;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.EntraId;

public class EntraIdRegistrationPipelineTests
{
    private const string BaseClientId = "11111111-1111-1111-1111-111111111111";
    private const string DevelopmentClientId = "22222222-2222-2222-2222-222222222222";

    [Fact]
    public async Task LegacyRegisterCommand_DoesNotOptIntoRegistrationOnly()
    {
        bool called = false;
        int result = await IdentityProgram.Run(["--register-app", "--client-id", BaseClientId], (command, options) =>
        {
            called = true;
            Assert.False(options.RegistrationOnly);
            Assert.Equal(BaseClientId, options.ClientId);
            Assert.Equal(Commands.REGISTER_APPLICATIION_COMMAND, command);
            return Mock.Of<IMsAADTool>(tool => tool.Run() == Task.FromResult<ApplicationParameters?>(null));
        });

        Assert.True(called);
        Assert.Equal(0, result);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RegistrationAndSettings_LeaveBaseConfigurationUntouched(bool reuseExisting)
    {
        string directory = Path.Combine(Directory.GetCurrentDirectory(), $"EntraPipeline-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            string project = Path.Combine(directory, "Development.csproj");
            string baseSettings = Path.Combine(directory, "appsettings.json");
            string baseContent = $$"""{"AzureAd":{"ClientId":"{{BaseClientId}}","TenantId":"production"},"Secret":"preserve"}""";
            File.WriteAllText(project, """<Project Sdk="Microsoft.NET.Sdk.Web"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>""");
            File.WriteAllText(baseSettings, baseContent);
            var fileSystem = new FileSystem();
            var context = new ScaffolderContext(Mock.Of<IScaffolder>());
            context.Properties[nameof(EntraIdSettings)] = new EntraIdSettings
            {
                Project = project,
                Username = "developer@contoso.com",
                TenantId = "development-tenant",
                UseExistingApplication = reuseExisting,
                Application = reuseExisting ? $"Development {DevelopmentClientId}" : null
            };
            context.Properties[nameof(EntraIdModel)] = new EntraIdModel
            {
                Username = "developer@contoso.com",
                TenantId = "development-tenant"
            };
            var registration = new PipelineRegisterStep(fileSystem, project);
            var settings = new UpdateAppSettingsStep(
                NullLogger<UpdateAppSettingsStep>.Instance, fileSystem, Mock.Of<ITelemetryService>())
            {
                ProjectPath = string.Empty
            };
            var builder = new Mock<IScaffoldBuilder>();
            Action<ScaffoldStepConfigurator<RegisterAppStep>>? configureRegistration = null;
            Action<ScaffoldStepConfigurator<UpdateAppSettingsStep>>? configureSettings = null;
            builder.Setup(b => b.WithStep<RegisterAppStep>(It.IsAny<Action<ScaffoldStepConfigurator<RegisterAppStep>>>()))
                .Callback<Action<ScaffoldStepConfigurator<RegisterAppStep>>, Action<ScaffoldStepConfigurator<RegisterAppStep>>>((action, _) => configureRegistration = action)
                .Returns(builder.Object);
            builder.Setup(b => b.WithStep<UpdateAppSettingsStep>(It.IsAny<Action<ScaffoldStepConfigurator<UpdateAppSettingsStep>>>()))
                .Callback<Action<ScaffoldStepConfigurator<UpdateAppSettingsStep>>, Action<ScaffoldStepConfigurator<UpdateAppSettingsStep>>>((action, _) => configureSettings = action)
                .Returns(builder.Object);
            builder.Object.WithRegisterAppStep().WithUpdateAppSettingsStep();

            // Deny all managed file reads/writes while the real parser, provisioning pipeline and settings step run.
            using (File.Open(baseSettings, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                configureRegistration!(new() { Step = registration, Context = context });
                Assert.Equal(reuseExisting, registration.SkipStep);
                if (!registration.SkipStep)
                {
                    Assert.True(await registration.ExecuteAsync(context));
                }
                configureSettings!(new() { Step = settings, Context = context });
                Assert.True(await settings.ExecuteAsync(context));
            }

            Assert.Equal(reuseExisting ? 0 : 1, registration.ProvisioningCalls);
            Assert.Equal(DevelopmentClientId, context.Properties["ClientId"]);
            Assert.Equal(baseContent, File.ReadAllText(baseSettings));
            JsonNode development = JsonNode.Parse(File.ReadAllText(Path.Combine(directory, "appsettings.Development.json")))!;
            Assert.Equal(DevelopmentClientId, development["AzureAd"]!["ClientId"]!.GetValue<string>());
            Assert.Equal("development-tenant", development["AzureAd"]!["TenantId"]!.GetValue<string>());
            Assert.Equal(3, Directory.GetFiles(directory).Length);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task RegistrationOnly_ExplicitClientIdDoesNotSelectBaseId()
    {
        string directory = Path.Combine(Directory.GetCurrentDirectory(), $"EntraReuse-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            string project = Path.Combine(directory, "Development.csproj");
            File.WriteAllText(project, "<Project />");
            var step = new PipelineRegisterStep(new FileSystem(), project) { ClientId = DevelopmentClientId };
            var context = new ScaffolderContext(Mock.Of<IScaffolder>());
            Assert.True(await step.ExecuteAsync(context));
            Assert.Equal(DevelopmentClientId, step.RequestedClientId);
            Assert.Equal(DevelopmentClientId, context.Properties["ClientId"]);
            Assert.Single(Directory.GetFiles(directory));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private sealed class PipelineRegisterStep : RegisterAppStep
    {
        public int ProvisioningCalls { get; private set; }
        public string? RequestedClientId { get; private set; }

        [SetsRequiredMembers]
        public PipelineRegisterStep(IFileSystem fileSystem, string project)
            : base(NullLogger<AddClientSecretStep>.Instance, fileSystem, Mock.Of<ITelemetryService>())
        {
            ProjectPath = project;
            Username = "developer@contoso.com";
            TenantId = "development-tenant";
        }

        protected override bool EnsureMsIdentityIsInstalled() => true;

        protected override int ExecuteMsIdentity(string[] args, out string stdOut, out string stdErr)
        {
            TextWriter originalOutput = Console.Out;
            using var output = new StringWriter();
            try
            {
                int exitCode = Task.Run(() => IdentityProgram.Run(args, (command, options) =>
                {
                    Assert.Equal(Commands.REGISTER_APPLICATIION_COMMAND, command);
                    Assert.True(options.RegistrationOnly);
                    Assert.Equal(ProjectPath, options.ProjectFilePath);
                    Assert.Equal("webapp", options.ProjectType);
                    Assert.Equal("Development", options.AppDisplayName);
                    var tool = new FakeProvisioner(command, options, parameters =>
                    {
                        ProvisioningCalls++;
                        RequestedClientId = parameters.ClientId;
                        Assert.NotEqual(BaseClientId, parameters.ClientId);
                        Assert.Equal(ClientId, parameters.ClientId);
                        Assert.Null(parameters.EffectiveClientId);
                        Assert.Equal(TenantId, parameters.TenantId);
                        Assert.True(parameters.IsWebApp);
                        Assert.Equal(Path.GetDirectoryName(ProjectPath), parameters.ProjectPath);
                        return new ApplicationParameters { ClientId = DevelopmentClientId };
                    });
                    Console.SetOut(output);
                    return tool;
                })).GetAwaiter().GetResult();
                stdOut = output.ToString();
                stdErr = string.Empty;
                return exitCode;
            }
            finally
            {
                Console.SetOut(originalOutput);
            }
        }
    }

    private sealed class FakeProvisioner : AppProvisioningTool
    {
        private readonly Func<ApplicationParameters, ApplicationParameters> _provision;

        public FakeProvisioner(string command, ProvisioningToolOptions options, Func<ApplicationParameters, ApplicationParameters> provision)
            : base(command, options) => _provision = provision;

        protected override Task<ApplicationParameters?> ProvisionRegistrationOnly(ApplicationParameters parameters) =>
            Task.FromResult<ApplicationParameters?>(_provision(parameters));
    }
}
