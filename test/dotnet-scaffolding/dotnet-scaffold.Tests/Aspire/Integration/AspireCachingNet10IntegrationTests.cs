// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.Tools.Scaffold.Tests.Aspire.Integration;

[Skip("Aspire tests on separate branch")]
public class AspireCachingNet10IntegrationTests : AspireCachingIntegrationTestsBase
{
    protected override string TargetFramework => "net10.0";
    protected override string TestClassName => nameof(AspireCachingNet10IntegrationTests);
}
