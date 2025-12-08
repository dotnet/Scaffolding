// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.Model;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Scaffolding.Internal.Telemetry;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Telemetry;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;

internal class WrappedAddPackagesStep : AddPackagesStep
{
    private readonly ILogger _logger;
    private readonly ITelemetryService _telemetryService;

    /// <summary>
    /// Constructor for WrappedAddPackagesStep.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="telemetryService">Telemetry service instance.</param>
    /// <param name="nugetVersionHelper">The NuGet version helper for package version resolution.</param>
    public WrappedAddPackagesStep(
        ILogger<WrappedAddPackagesStep> logger,
        ITelemetryService telemetryService,
        NuGetVersionService nugetVersionHelper) : base(logger, nugetVersionHelper)
    {
        _logger = logger;
        _telemetryService = telemetryService;
    }

    public override async Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
    {
        var result = await base.ExecuteAsync(context, cancellationToken);
        _telemetryService.TrackEvent(new WrappedStepTelemetryEvent(nameof(AddPackagesStep), context.Scaffolder.DisplayName, result));
        return result;
    }
}
