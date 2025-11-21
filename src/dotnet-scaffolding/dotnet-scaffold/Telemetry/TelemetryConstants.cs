// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This file defines constant values used for telemetry reporting in the dotnet-scaffold tool.
// These constants represent tool types and operation outcomes for consistent telemetry event logging.

namespace Microsoft.DotNet.Tools.Scaffold.Telemetry;

/// <summary>
/// Provides constant string values for telemetry event properties and outcomes.
/// </summary>
internal static class TelemetryConstants
{
    /// <summary>
    /// Indicates the tool is running as a global tool.
    /// </summary>
    public static readonly string GlobalTool = "Global";

    /// <summary>
    /// Indicates the tool is running as a local tool.
    /// </summary>
    public static readonly string LocalTool = "Local";

    /// <summary>
    /// Represents a successful operation outcome.
    /// </summary>
    public static readonly string Success = "Success";

    /// <summary>
    /// Represents a failed operation outcome.
    /// </summary>
    public static readonly string Failure = "Failure";

    /// <summary>
    /// Represents an unknown operation outcome.
    /// </summary>
    public static readonly string Unknown = "Unknown";
}
