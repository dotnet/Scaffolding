// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Scaffolding.Core.Builder;

internal class ScaffoldRunner(ILogger<ScaffoldRunner> logger) : IScaffoldRunner
{
    private readonly ILogger<ScaffoldRunner> _logger = logger;

    public IEnumerable<IScaffolder>? Scaffolders { get; set; }
    internal RootCommand? RootCommand { get; set; }

    public async Task RunAsync(string[] args)
    {
        if (RootCommand is null)
        {
            throw new InvalidOperationException("RootCommand is not set.");
        }

        await RootCommand.InvokeAsync(args);
    }
}
