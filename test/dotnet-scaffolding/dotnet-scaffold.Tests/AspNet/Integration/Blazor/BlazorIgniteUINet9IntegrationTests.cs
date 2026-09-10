// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.Blazor;

public class BlazorIgniteUINet9IntegrationTests : BlazorIgniteUIIntegrationTestsBase
{
    protected override string TargetFramework => "net9.0";
    protected override string TestClassName => nameof(BlazorIgniteUINet9IntegrationTests);

    [Fact]
    public async Task Scaffold_BlazorIgniteUI_Net9_CliInvocation()
    {
        // Arrange — Blazor Web App restricted to nuget.org so preview feeds do not interfere
        SetupBlazorWebAppProject();
        File.WriteAllText(Path.Combine(_testProjectDir, "NuGet.config"), ScaffoldCliHelper.StableNuGetConfig);

        // Act + Assert — dotnet scaffold aspnet blazor-igniteui --package All
        await ScaffoldAllPackagesAndAssertAsync();
    }
}
