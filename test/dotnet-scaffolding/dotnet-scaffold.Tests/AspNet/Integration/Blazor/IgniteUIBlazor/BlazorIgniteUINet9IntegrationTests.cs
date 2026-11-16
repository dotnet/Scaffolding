// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.Blazor;

public class BlazorIgniteUINet9IntegrationTests : BlazorIgniteUIIntegrationTestsBase
{
    protected override string TargetFramework => "net9.0";
    protected override string TestClassName => nameof(BlazorIgniteUINet9IntegrationTests);

    [Fact]
    public async Task Scaffold_BlazorIgniteUI_Net9_CliInvocation()
    {
        // Arrange — Blazor Web App (the base fixture restricts restore to the stable dotnet-public feed)
        SetupBlazorWebAppProject();

        // Act + Assert — dotnet scaffold aspnet blazor-igniteui --project TestProject.csproj
        await ScaffoldAllPackagesAndAssertAsync();
    }
}
