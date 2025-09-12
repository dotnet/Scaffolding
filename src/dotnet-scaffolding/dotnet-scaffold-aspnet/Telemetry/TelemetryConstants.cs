// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Telemetry;

/// <summary>
/// Constants used for telemetry event properties and values.
/// </summary>
internal static class TelemetryConstants
{
    /// <summary>Indicates an item was added.</summary>
    public static readonly string Added = "Added";
    /// <summary>Indicates no change occurred.</summary>
    public static readonly string NoChange = "NoChange";
    /// <summary>Indicates a successful result.</summary>
    public static readonly string Success = "Success";
    /// <summary>Indicates a failure result.</summary>
    public static readonly string Failure = "Failure";
    /// <summary>Indicates an unknown result.</summary>
    public static readonly string Unknown = "Unknown";
    /// <summary>Indicates a 'Yes' value.</summary>
    public static readonly string Yes = "Yes";
    /// <summary>Indicates a 'No' value.</summary>
    public static readonly string No = "No";
}
