// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.Builder;

namespace Microsoft.DotNet.Scaffolding.Core.Hosting;

public static class Host
{
    public static IScaffoldRunnerBuilder CreateScaffoldBuilder() => new ScaffoldRunnerBuilder();
}
