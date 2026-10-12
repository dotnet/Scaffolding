// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;

/// <summary>
/// Logs what the app still needs from the developer once the Ignite UI for Blazor setup has completed, such as the
/// interactive render mode for the pages that use the components (the scaffolder does not generate pages).
/// </summary>
internal class IgniteUIBlazorGuidanceStep : ScaffoldStep
{
    /// <summary>
    /// Gets or sets the guidance messages to log.
    /// </summary>
    public IList<string> Messages { get; set; } = [];

    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="IgniteUIBlazorGuidanceStep"/> class.
    /// </summary>
    public IgniteUIBlazorGuidanceStep(ILogger<IgniteUIBlazorGuidanceStep> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public override Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
    {
        foreach (var message in Messages)
        {
            _logger.LogInformation(message);
        }

        return Task.FromResult(true);
    }
}
