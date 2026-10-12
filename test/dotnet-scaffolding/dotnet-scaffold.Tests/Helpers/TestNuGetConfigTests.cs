// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using NuGet.Configuration;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;

public class TestNuGetConfigTests
{
    [Theory]
    [InlineData("net8.0")]
    [InlineData("net9.0")]
    [InlineData("net10.0")]
    [InlineData("net11.0")]
    public void TestProjectFeedsIgnoreInheritedDisabledSourcesAndMappings(string framework)
    {
        string directory = Path.Combine(Path.GetTempPath(), nameof(TestNuGetConfigTests), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllText(Path.Combine(directory, "NuGet.config"), """
                <configuration>
                  <packageSources>
                    <clear />
                    <add key="unrelated" value="https://example.invalid/v3/index.json" />
                  </packageSources>
                  <disabledPackageSources>
                    <add key="dotnet-public" value="true" />
                    <add key="dotnet11" value="true" />
                  </disabledPackageSources>
                  <packageSourceMapping>
                    <packageSource key="unrelated">
                      <package pattern="*" />
                    </packageSource>
                  </packageSourceMapping>
                </configuration>
                """);

            string project = ScaffoldCliHelper.SetupTestProject(directory, framework);
            var settings = Settings.LoadDefaultSettings(project);
            var sources = new PackageSourceProvider(settings).LoadPackageSources().ToArray();
            Assert.DoesNotContain(sources, source => source.Name == "unrelated");
            Assert.Contains(sources, source => source.Name == "dotnet-public" && source.IsEnabled);
            Assert.False(PackageSourceMapping.GetPackageSourceMapping(settings).IsEnabled);
            if (framework == "net11.0")
            {
                Assert.Contains(sources, source => source.Name == "dotnet11" && source.IsEnabled);
            }
            else
            {
                Assert.Equal("dotnet-public", Assert.Single(sources).Name);
            }
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
