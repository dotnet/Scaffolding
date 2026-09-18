// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.ScaffoldSteps;

public class AddIdentityMigrationStepTests
{
    [Fact]
    public void GetEfDesignPackageVersion_ReturnsVersion()
    {
        const string assetsContent = """
{
  "libraries": {
    "Microsoft.EntityFrameworkCore.Design/11.0.0-rc.1.26425.128": {
      "type": "package"
    }
  }
}
""";

        var result = AddIdentityMigrationStep.GetEfDesignPackageVersion(assetsContent);

        Assert.Equal("11.0.0-rc.1.26425.128", result);
    }

    [Fact]
    public void GetEfDesignPackageVersion_ReturnsNullWhenPackageIsMissing()
    {
        const string assetsContent = """{ "libraries": {} }""";

        var result = AddIdentityMigrationStep.GetEfDesignPackageVersion(assetsContent);

        Assert.Null(result);
    }
}
