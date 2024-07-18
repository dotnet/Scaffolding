// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.Builder;

namespace Microsoft.DotNet.Scaffolding.Core.Hosting;

public static class Host
{
    /// <summary>
    /// The starting point for creating a scaffolder. Returns a builder for a scaffold runner that allows configuration of any number of scaffolders, their options and steps.
    /// </summary>
    public static IScaffoldRunnerBuilder CreateScaffoldBuilder() => new ScaffoldRunnerBuilder();
}
