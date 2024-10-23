// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Microsoft.DotNet.Scaffolding.Internal.Services;

/// <summary>
/// Wrapper for ITelemetryService, and IFirstTimeUseNoticeSentinel.
/// Handles first time notice and to wait on telemetry tasks to be completed.
/// </summary>
internal interface IFirstPartyToolTelemetryWrapper
{
    //Handles first time notice and sentinel file creation
    void ConfigureFirstTimeTelemetry();

    //Calls ITelemetryService.Flush()
    void Flush();
}
