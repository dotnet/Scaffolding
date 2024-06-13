// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;

namespace Microsoft.DotNet.Scaffolding.Steps;

internal abstract class ScaffoldStep
{
    public abstract Task<bool> ExecuteAsync();
    public bool ContinueOnError { get; set; }
}


// Scaffolding Component (dotnet-scaffold-aspire, dotnet-scaffold-aspnet)
// Scaffolding Step (add reference, code mod, drop file, templated file)
// Scaffolder (collection of steps)
// Command (routes from CLI input or command line args to a Scaffolder)
