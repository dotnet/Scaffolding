// Copyright (c) Microsoft Corporation. All rights reserved.
namespace Microsoft.DotNet.Scaffolding.Internal.Services;

internal class AppSettings : IAppSettings
{
    private readonly Dictionary<string, object> _settings;

    public AppSettings()
    {
        _settings = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        GlobalProperties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public IDictionary<string, string> GlobalProperties { get; }

    /// <inheritdoc />
    public object? GetSettings(string sectionName)
    {
        if (string.IsNullOrEmpty(sectionName))
        {
            return null;
        }

        return _settings.TryGetValue(sectionName, out var settings) ? settings : null;
    }

    /// <inheritdoc />
    public void AddSettings(string sectionName, object settings)
    {
        if (string.IsNullOrEmpty(sectionName))
        {
            return;
        }

        _settings[sectionName] = settings;
    }
}
