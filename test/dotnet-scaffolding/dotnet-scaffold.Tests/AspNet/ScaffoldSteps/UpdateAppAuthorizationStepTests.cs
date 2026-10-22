// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.ScaffoldSteps;

/// <summary>
/// Unit tests for the app registration settings applied by UpdateAppAuthorizationStep during Entra ID scaffolding.
/// </summary>
public class UpdateAppAuthorizationStepTests
{
    [Fact]
    public void BuildBaseUpdateCommand_DisablesImplicitGrantIssuance()
    {
        var command = UpdateAppAuthorizationStep.BuildBaseUpdateCommand("11111111-2222-3333-4444-555555555555");

        Assert.Equal(
            "ad app update --id 11111111-2222-3333-4444-555555555555 --enable-id-token-issuance false --enable-access-token-issuance false",
            command);
    }
}
