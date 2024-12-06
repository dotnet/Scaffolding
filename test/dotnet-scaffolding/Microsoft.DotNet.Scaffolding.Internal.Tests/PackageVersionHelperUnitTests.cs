// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Internal.Helpers;
using Xunit;

namespace Microsoft.DotNet.Scaffolding.Internal;

public class PackageVersionHelperUnitTests
{
    [Fact]
    public async Task GetPackageVersionFromTfmTest()
    {
        //using "Microsoft.AspNetCore" since its depracated
        var microsoftAspnetCorePackageNet21Version = await PackageVersionHelper.GetPackageVersionFromTfmAsync("Microsoft.AspNetCore", "net2.1");
        var microsoftAspnetCorePackageNet22Version = await PackageVersionHelper.GetPackageVersionFromTfmAsync("Microsoft.AspNetCore", "net2.2");
        Assert.Equal("2.2.0", microsoftAspnetCorePackageNet22Version);
        Assert.Equal("2.1.7", microsoftAspnetCorePackageNet21Version);
    }
}
