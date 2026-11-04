// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.ScaffoldSteps;

public class ValidateAzureCliIdentityStepTests
{
    [Theory]
    [InlineData(0, "https://login.microsoftonline.com/11111111-2222-3333-4444-555555555555/v2.0", true)]
    [InlineData(0, "https://login.microsoftonline.com/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee/v2.0", false)]
    [InlineData(1, null, false)]
    [InlineData(0, "https://untrusted.example/11111111-2222-3333-4444-555555555555/v2.0", false)]
    public void ValidateIdentity_ResolvesTenantDomainUsingTrustedIssuer(int exitCode, string? issuer, bool expected)
    {
        var resolved = false;
        var result = ValidateAzureCliIdentityStep.ValidateIdentity("contoso.onmicrosoft.com", "user@example.com",
            NullLogger.Instance,
            () => (0, """{"tenantId":"11111111-2222-3333-4444-555555555555","user":{"name":"user@example.com","type":"user"}}""", null),
            domain =>
            {
                Assert.Equal("contoso.onmicrosoft.com", domain);
                resolved = true;
                return (exitCode, issuer, null);
            });

        Assert.Equal(expected, result);
        Assert.True(resolved);
    }

    [Theory]
    [InlineData("""{"tenantId":"TENANT-A","user":{"name":"USER@EXAMPLE.COM","type":"USER"}}""", true)]
    [InlineData("""{"tenantId":"tenant-b","user":{"name":"user@example.com","type":"user"}}""", false)]
    [InlineData("""{"tenantId":"tenant-a","user":{"name":"other@example.com","type":"user"}}""", false)]
    [InlineData("""{"tenantId":"tenant-a","user":{"name":"user@example.com","type":"servicePrincipal"}}""", false)]
    [InlineData("{}", false)]
    [InlineData("invalid JSON", false)]
    public async Task ExecuteAsync_RequiresSelectedTenantAndUser(string output, bool expected)
    {
        var step = CreateStep(output);
        var context = new ScaffolderContext(Mock.Of<IScaffolder>());

        Assert.Equal(expected, await step.ExecuteAsync(context));
        Assert.Empty(context.Properties);
    }

    [Fact]
    public async Task ExecuteAsync_CliFailureReportsSelectedIdentityLoginGuidance()
    {
        var logger = new Mock<ILogger<ValidateAzureCliIdentityStep>>();
        var step = new TestStep(logger.Object)
        {
            TenantId = "tenant-a",
            Username = "user@example.com",
            Output = """{"tenantId":"tenant-a","user":{"name":"user@example.com","type":"user"}}""",
            ExitCode = 1
        };

        Assert.False(await step.ExecuteAsync(new ScaffolderContext(Mock.Of<IScaffolder>())));
        var error = Assert.Single(logger.Invocations, invocation =>
            invocation.Method.Name == nameof(ILogger.Log) &&
            invocation.Arguments[0] is LogLevel.Error);
        Assert.Contains("az login --tenant tenant-a --allow-no-subscriptions", error.Arguments[2].ToString());
        Assert.Contains("user@example.com", error.Arguments[2].ToString());
    }

    private static TestStep CreateStep(string output) => new(NullLogger<ValidateAzureCliIdentityStep>.Instance)
    {
        TenantId = "tenant-a",
        Username = "user@example.com",
        Output = output
    };

    private sealed class TestStep(ILogger<ValidateAzureCliIdentityStep> logger) : ValidateAzureCliIdentityStep(logger)
    {
        public required string Output { get; init; }
        public int ExitCode { get; set; }
        protected override (int ExitCode, string? Output, string? Error) ReadAccount()
        {
            return (ExitCode, Output, ExitCode == 0 ? null : "Please run az login.");
        }
    }
}
