// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Tools.Scaffold.Aspire.ScaffoldSteps;

//TODO: move to Microsoft.DotNet.Scaffolding.Core and register it there.
public class ValidateOptionsStep : ScaffoldStep
{
    public required Func<ScaffolderContext, ILogger, bool> ValidateMethod { get; set; }
    private readonly ILogger _logger;

    public ValidateOptionsStep(ILogger<ValidateOptionsStep> logger)
    {
        _logger = logger;
    }

    public override Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
    {
        if (!ValidateMethod(context, _logger))
        {
            _logger.LogError("Validation failed.");
            return Task.FromResult(false);
        }

        _logger.LogInformation("Validation succeeded.");
        return Task.FromResult(true);
    }
}
