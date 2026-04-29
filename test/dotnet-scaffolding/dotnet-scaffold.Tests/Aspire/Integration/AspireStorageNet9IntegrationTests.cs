// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.Tools.Scaffold.Tests.Aspire.Integration;

public class AspireStorageNet9IntegrationTests : AspireStorageIntegrationTestsBase
{
    protected override string TargetFramework => "net9.0";
    protected override string TestClassName => nameof(AspireStorageNet9IntegrationTests);
}
