// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.Blazor;

public class BlazorIgniteUINet11IntegrationTests : BlazorIgniteUIIntegrationTestsBase
{
    protected override string TargetFramework => "net11.0";
    protected override string TestClassName => nameof(BlazorIgniteUINet11IntegrationTests);

    [Fact]
    public async Task Scaffold_BlazorIgniteUI_Net11_CliInvocation()
    {
        // Arrange — Blazor Web App; allow warnings so preview-SDK warnings do not fail the build, and use
        // the preview feeds so the preview-only framework packages resolve during restore/build.
        // Use the shared preview feed configuration for framework and Ignite UI packages;
        // no explicit nuget.org source is needed.
        SetupBlazorWebAppProject("    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>");
        File.WriteAllText(Path.Combine(_testProjectDir, "NuGet.config"), ScaffoldCliHelper.PreviewNuGetConfig);

        // Act + Assert — dotnet scaffold aspnet blazor-igniteui --project TestProject.csproj
        // The Ignite UI packages ship net10.0 assets which net11.0 projects consume; no prerelease needed.
        await ScaffoldAllPackagesAndAssertAsync();
    }
}
