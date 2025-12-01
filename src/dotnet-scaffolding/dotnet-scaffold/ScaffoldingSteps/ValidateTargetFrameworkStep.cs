// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.Core.Model;
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
        "net8.0", "net9.0", "net10.0"
    ];

    private static readonly HashSet<string> UnsupportedLegacyFrameworks =
    [
        "net7.0",
        "net6.0",
        "net5.0",
        "netcoreapp3.1",
        "netcoreapp3.0",
        "netcoreapp2.2",
        "netcoreapp2.1",
        "netcoreapp2.0",
        "netcoreapp1.1",
        "netcoreapp1.0",
        "netstandard2.1",
        "netstandard2.0",
        "netstandard1.6",
        "netstandard1.5",
        "netstandard1.4",
        "netstandard1.3",
        "netstandard1.2",
        "netstandard1.1",
        "netstandard1.0",
        "net48",
        "net472",
        "net471",
        "net47",
        "net462",
        "net461",
        "net46",
        "net452",
        "net451",
        "net45",
        "net403",
        "net40",
        "net35",
        "net20",
        "net11"
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

        var normalizedValue = value.Trim().ToLowerInvariant();

        // Check if it's a legacy framework that is no longer supported
        if (UnsupportedLegacyFrameworks.Contains(normalizedValue))
        {
            logger.LogError($"Invalid {TargetFrameworkConstants.TargetFrameworkCliOption} option: '{value}'. .NET 8.0 is the lowest supported version. Please use net8.0, net9.0, or net10.0.");
            return false;
        }

        // Check if it's a valid supported framework
        if (!ValidTargetFrameworks.Contains(normalizedValue))
        {
            logger.LogError($"Invalid {TargetFrameworkConstants.TargetFrameworkCliOption} option: '{value}'. Must be a valid .NET SDK version (e.g., net8.0, net9.0, net10.0).");
            return false;
        }

        return true;
    }
}
