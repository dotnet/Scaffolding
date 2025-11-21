// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.CommandLine.Parsing;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Scaffolding.Core.Builder;

/// <summary>
/// Executes scaffolders using provided arguments and manages the root command.
/// </summary>
internal class ScaffoldRunner(ILogger<ScaffoldRunner> logger) : IScaffoldRunner
{
    // Logger instance for runner
    private readonly ILogger<ScaffoldRunner> _logger = logger;

    /// <inheritdoc/>
    public IReadOnlyDictionary<ScaffolderCatagory, IEnumerable<IScaffolder>>? Scaffolders { get; set; }

    /// <summary>
    /// Options for the "dotnet-scaffold" tool
    /// </summary>
    public IEnumerable<ScaffolderOption>? Options { get; set; }

    /// <summary>
    /// Gets or sets the root command for the CLI.
    /// </summary>
    internal RootCommand? RootCommand { get; set; }

    /// <inheritdoc/>
    public async Task RunAsync(string[] args)
    {
        if (RootCommand is null)
        {
            throw new InvalidOperationException("RootCommand is not set.");
        }

        // Parse and invoke the root command with the provided arguments
        ParseResult parseResult = RootCommand.Parse(args);
        await parseResult.InvokeAsync();
    }

    /// <summary>
    /// Adds an action to the RootCommand doing the action passed in the handle parameter.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public void AddHandler(Func<ParseResult, CancellationToken, Task> handle)
    {
        if (RootCommand is null)
        {
            throw new InvalidOperationException("RootCommand is not set.");
        }

        RootCommand.SetAction(handle);
    }
}
