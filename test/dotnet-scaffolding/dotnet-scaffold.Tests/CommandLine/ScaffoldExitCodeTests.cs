// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.CommandLine;

public class ScaffoldExitCodeTests
{
    [Fact]
    public async Task EntraValidationFailure_ReturnsNonzeroProcessExitCode()
    {
        string missingProject = Path.Combine(Path.GetTempPath(), $"scaffold-missing-{Guid.NewGuid():N}.csproj");

        var (exitCode, output, error) = await ScaffoldCliHelper.RunScaffoldAsync(
            "net11.0", "entra-id",
            "--project", missingProject,
            "--username", "user@example.com",
            "--tenantId", "test-tenant",
            "--use-existing-application", "false");

        Assert.True(exitCode == 1, $"Expected a failed scaffold process. Exit code: {exitCode}\n{output}\n{error}");
        Assert.Contains("ValidateEntraIdStep", output + error);
        Assert.Contains("project or external resources may have been partially modified", output + error);
    }

    [Theory]
    [InlineData("--help", 0)]
    [InlineData("--unknown-option", 1)]
    public async Task HelpAndParseErrors_ReturnExpectedProcessExitCode(string argument, int expectedExitCode)
    {
        var (exitCode, output, error) = await ScaffoldCliHelper.RunScaffoldAsync("net11.0", "entra-id", argument);

        Assert.True(exitCode == expectedExitCode, $"Unexpected process exit code: {exitCode}\n{output}\n{error}");
        Assert.Contains(argument == "--help" ? "entra-id" : argument, output + error);
    }
}
