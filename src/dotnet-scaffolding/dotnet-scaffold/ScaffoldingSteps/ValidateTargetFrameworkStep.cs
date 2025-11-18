// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Tools.Scaffold.ScaffoldingSteps;

public class ValidateTargetFrameworkStep : ScaffoldStep
{
    private readonly ILogger _logger;

    /// <summary>
    /// Gets or sets the target .NET framework for the project or component.
    /// </summary>
    /// <remarks>Specify the framework using a valid framework moniker, such as "net6.0" or
    /// "netstandard2.1". This property is typically used to determine compatibility and runtime behavior.</remarks>
    public string? TargetFramework { get; set; }

    private static readonly HashSet<string> ValidTargetFrameworks =
    [
        "net8.0", "net9.0", "net10.0", "net7.0", "net6.0", "net5.0", "netcoreapp3.1", "netstandard2.0"
    ];

    public ValidateTargetFrameworkStep(ILogger<ValidateTargetFrameworkStep> logger)
    {
        _logger = logger;
    }

    public override Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(TargetFramework) && !ValidateTargetFrameworkOption(TargetFramework, _logger))
        {
            return Task.FromResult(false);
        }
        return Task.FromResult(true);
    }

    private static bool ValidateTargetFrameworkOption(string? value, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            // Option is optional, so do not error if not specified
            return true;
        }
        if (!ValidTargetFrameworks.Contains(value.Trim().ToLowerInvariant()))
        {
            logger.LogError($"Invalid --target-framework option: '{value}'. Must be a valid .NET SDK version (e.g., net8.0, net9.0, net10.0, net7.0, net6.0, net5.0, netcoreapp3.1, netstandard2.0).");
            return false;
        }
        return true;
    }
}
