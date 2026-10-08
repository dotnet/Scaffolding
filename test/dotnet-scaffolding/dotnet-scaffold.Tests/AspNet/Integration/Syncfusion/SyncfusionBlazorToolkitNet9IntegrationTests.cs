// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.Syncfusion;

public class SyncfusionBlazorToolkitNet9IntegrationTests : SyncfusionBlazorToolkitIntegrationTestsBase
{
    protected override string TargetFramework => "net9.0";
    protected override string TestClassName => nameof(SyncfusionBlazorToolkitNet9IntegrationTests);

    [Fact]
    public async Task Scaffold_SyncfusionBlazorToolkit_Net9_CliInvocation()
    {
        WriteBlazorWebAppScaffold();

        var (cliExitCode, cliOutput, cliError) = await ScaffoldCliHelper.RunScaffoldAsync(
            TargetFramework, "syncfusion-blazor-toolkit", "--project", _testProjectPath);
        Assert.True(cliExitCode == 0,
            $"CLI scaffold should succeed.\nOutput: {cliOutput}\nError: {cliError}");

        string csproj = File.ReadAllText(_testProjectPath);
        Assert.Contains("Syncfusion.Blazor.Toolkit", csproj);
    }
}
