// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.ScaffoldSteps;

public class UpdateAppAuthorizationStepTests
{
    [Fact]
    public async Task ExecuteAsync_RechecksIdentityBeforeEachUpdate()
    {
        var step = CreateStep();
        var context = new ScaffolderContext(Mock.Of<IScaffolder>());
        Assert.True(await step.ExecuteAsync(context));
        Assert.Equal(1, step.Updates);

        step.AccountOutput = """{"tenantId":"tenant-b","user":{"name":"user@example.com","type":"user"}}""";
        Assert.False(await step.ExecuteAsync(context));
        Assert.Equal(1, step.Updates);
        Assert.Equal(2, step.AccountReads);
    }

    [Theory]
    [InlineData(0, null, true)]
    [InlineData(0, "CLI warning", true)]
    [InlineData(1, "Update failed", false)]
    public async Task ExecuteAsync_UsesExitCodeAndPreservesAuthorizationArguments(int exitCode, string? error, bool expected)
    {
        var step = CreateStep();
        step.ExitCode = exitCode;
        step.Error = error;

        Assert.Equal(expected, await step.ExecuteAsync(new ScaffolderContext(Mock.Of<IScaffolder>())));
        Assert.Equal("ad app update --id app-id --enable-id-token-issuance true --web-redirect-uris \"https://localhost:5001/signin-oidc\"",
            step.Command);
    }

    private static TestStep CreateStep() => new()
    {
        ProjectPath = "Test.csproj",
        ClientId = "app-id",
        TenantId = "tenant-a",
        Username = "user@example.com",
        AutoConfigureLocalUrls = false,
        WebRedirectUris = ["https://localhost:5001/signin-oidc"]
    };

    private sealed class TestStep() : UpdateAppAuthorizationStep(
        NullLogger<UpdateAppAuthorizationStep>.Instance, Mock.Of<IFileSystem>(), Mock.Of<ITelemetryService>())
    {
        public string AccountOutput { get; set; } =
            """{"tenantId":"tenant-a","user":{"name":"user@example.com","type":"user"}}""";
        public int AccountReads { get; private set; }
        public int Updates { get; private set; }
        public int ExitCode { get; set; }
        public string? Error { get; set; }
        public string? Command { get; private set; }

        protected override (int ExitCode, string? Output, string? Error) ReadAccount()
        {
            AccountReads++;
            return (0, AccountOutput, null);
        }

        protected override (int ExitCode, string? Error) RunUpdate(string command)
        {
            Assert.True(AccountReads > Updates);
            Command = command;
            Updates++;
            return (ExitCode, Error);
        }
    }
}
