// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Tools.Scaffold.Aspire.ScaffoldSteps;

public class ValidateContextStep : ScaffoldStep
{
    public required Func<ScaffolderContext, ILogger, bool> ValidateMethod { get; set; }
    private readonly ILogger _logger;

    public ValidateContextStep(ILogger<ValidateContextStep> logger)
    {
        _logger = logger;
    }

    public override Task ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
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
