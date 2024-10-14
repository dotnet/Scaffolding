// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.Scaffolding.Internal.Telemetry;

internal abstract class TelemetryEventBase(string name)
{
    private readonly Dictionary<string, string> _properties = [];
    private readonly Dictionary<string, double> _measurements = [];

    public string Name => name;
    public IReadOnlyDictionary<string, string> Properties => _properties;
    public IReadOnlyDictionary<string, double> Measurements => _measurements;

    protected void SetProperty(string name, bool value) => SetProperty(name, value.ToString());
    protected void SetProperty(string name, string value) => _properties.TryAdd(name, value);
    protected string? GetProperty(string name)
    {
        if (_properties.TryGetValue(name, out var value))
        {
            return value;
        }

        return null;
    }

    protected void SetMeasurement(string name, double value) => _measurements.TryAdd(name, value);
    protected double? GetMeasurement(string name)
    {
        if (_measurements.TryGetValue(name, out double value))
        {
            return value;
        }

        return null;
    }
}
