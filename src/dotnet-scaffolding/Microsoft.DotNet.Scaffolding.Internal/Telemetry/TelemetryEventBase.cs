// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Internal.Extensions;

namespace Microsoft.DotNet.Scaffolding.Internal.Telemetry;

internal abstract class TelemetryEventBase(string name)
{
    private readonly Dictionary<string, string> _properties = [];
    private readonly Dictionary<string, double> _measurements = [];
    
    public string Name => name;
    public IReadOnlyDictionary<string, string> Properties => _properties;
    public IReadOnlyDictionary<string, double> Measurements => _measurements;

    protected void SetProperty(string name, bool value, bool isPII = false)
    {
        SetProperty(name, value.ToString(), isPII);
    }

    protected void SetProperty(string name, IEnumerable<string> values, bool isPII = false)
    {
        var valueArray = values.ToArray();
        if (isPII)
        {
            for (int i = 0; i < values.Count(); i++)
            {
                valueArray[i] = valueArray[i].Hash();
            }
        }

        //instead of passing the correct isPII flag to the SetProperty method,
        //we are hashing each individual value in the array (if isPII is true),
        //and then joining the array into a single string to pass to the SetProperty method
        SetProperty(name, string.Join(",", valueArray));
    }

    protected void SetProperty(string name, string value, bool isPII = false)
    {
        if (isPII)
        {
            name = string.Concat(TelemetryConstants.PII, name);
        }

        _properties.TryAdd(name, value);
    }

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
