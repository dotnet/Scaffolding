// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.Scaffolding.Internal.Telemetry;

internal static class TelemetryConstants
{
    public static readonly string TELEMETRY_OPTOUT = "DOTNET_SCAFFOLD_TELEMETRY_OPTOUT";
    public static readonly string SKIP_FIRST_TIME_EXPERIENCE = "DOTNET_SCAFFOLD_SKIP_FIRST_TIME_EXPERIENCE";
    public static readonly string DOTNET_SCAFFOLD_TELEMETRY_STATE = "DOTNET_SCAFFOLD_TELEMETRY_STATE";
    public static readonly string TELEMETRY_STATE_ENABLED = "enabled";
    public static readonly string TELEMETRY_STATE_DISABLED = "disabled";
    public static readonly string SENTINEL_SUFFIX = "FirstUseSentinel";
    public static readonly string CONNECTION_STRING = "InstrumentationKey=469489a6-628b-4bb9-80db-ec670f70d874;";
    public const string PII = "|PII|";
}
