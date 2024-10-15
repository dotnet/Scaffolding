// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.DotNet.PlatformAbstractions;
using Microsoft.DotNet.Scaffolding.Internal.Telemetry;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Scaffolding.Internal.Services;

internal class TelemetryService : ITelemetryService, IDisposable
{
    private readonly IFirstTimeUseNoticeSentinel _firstTimeUseNoticeSentinel;
    private readonly IEnvironmentService _environmentService;
    private readonly ILogger _logger;
    private readonly string? _currentSessionId;
    private TelemetryClient? _client;
    private TelemetryConfiguration? _telemetryConfig;
    private Dictionary<string, string> _commonProperties = [];
    private Dictionary<string, double> _commonMeasurements = [];
    private bool _enabled;
    private Task? _trackEventTask;

    public TelemetryService(IFirstTimeUseNoticeSentinel firstTimeUseNoticeSentinel, IEnvironmentService environmentService, ILogger<TelemetryService> logger)
    {
        _firstTimeUseNoticeSentinel = firstTimeUseNoticeSentinel;
        _environmentService = environmentService;
        _logger = logger;
        _enabled = !_environmentService.GetEnvironmentVariableAsBool(TelemetryConstants.TELEMETRY_OPTOUT) &&
           (_firstTimeUseNoticeSentinel.Exists() || _environmentService.GetEnvironmentVariableAsBool(TelemetryConstants.LAUNCHED_BY_DOTNET_SCAFFOLD));

        if (_enabled)
        {
            _logger.LogInformation("==========TELEMETRY ENABLED ===========");
            // Store the session ID in a static field so that it can be reused
            _currentSessionId = Guid.NewGuid().ToString();
            _trackEventTask = Task.Factory.StartNew(() => InitializeClient(), CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
        }
        else
        {
            _logger.LogInformation("==========TELEMETRY NOT ENABLED ===========");
        }
    }

    public void TrackEvent(string eventName, IReadOnlyDictionary<string, string> properties, IReadOnlyDictionary<string, double> measurements)
    {
        if (_trackEventTask is not null)
        {
            _trackEventTask = _trackEventTask.ContinueWith(
                x => TrackEventTask(eventName, properties, measurements),
                TaskScheduler.Default
            );
        }
    }

    private void InitializeClient()
    {
        _telemetryConfig = TelemetryConfiguration.CreateDefault();
        _telemetryConfig.ConnectionString = TelemetryConstants.CONNECTION_STRING;

        _client = new TelemetryClient(_telemetryConfig);
        _client.Context.Session.Id = _currentSessionId;
        _client.Context.Device.OperatingSystem = RuntimeEnvironment.OperatingSystem;
        //add common properties
        _commonProperties = new TelemetryCommonProperties(_environmentService, _firstTimeUseNoticeSentinel.ProductFullVersion).GetTelemetryCommonProperties();
    }

    private void TrackEventTask(
        string eventName,
        IReadOnlyDictionary<string, string> properties,
        IReadOnlyDictionary<string, double> measurements)
    {
        if (_client is null || !_enabled)
        {
            return;
        }

        try
        {
            Dictionary<string, string> eventProperties = GetEventProperties(properties);
            Dictionary<string, double> eventMeasurements = GetEventMeasures(measurements);

            _client.TrackEvent(PrependProducerNamespace(eventName), eventProperties, eventMeasurements);
            _client.Flush();
        }
        catch (Exception e)
        {
            Debug.Fail(e.ToString());
        }
    }

    private static string PrependProducerNamespace(string eventName)
    {
        return "dotnet/scaffolding/" + eventName;
    }

    private Dictionary<string, double> GetEventMeasures(IReadOnlyDictionary<string, double> measurements)
    {
        Dictionary<string, double> eventMeasurements = new Dictionary<string, double>(_commonMeasurements);
        if (measurements != null)
        {
            foreach (KeyValuePair<string, double> measurement in measurements)
            {
                eventMeasurements[measurement.Key] = measurement.Value;
            }
        }
        return eventMeasurements;
    }

    private Dictionary<string, string> GetEventProperties(IReadOnlyDictionary<string, string> properties)
    {
        if (properties != null)
        {
            var eventProperties = new Dictionary<string, string>(_commonProperties);
            foreach (KeyValuePair<string, string> property in properties)
            {
                eventProperties[property.Key] = property.Value;
            }
            return eventProperties;
        }
        else
        {
            return _commonProperties;
        }
    }

    public void Dispose()
    {
        _telemetryConfig?.Dispose();
        _client?.Flush();
    }
}
