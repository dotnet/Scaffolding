// Copyright (c) Microsoft Corporation. All rights reserved.

namespace Microsoft.DotNet.Scaffolding.Helpers.Services.Environment;

/// <summary>
/// Sets environment variables for this process.
/// </summary>
internal class EnvironmentVariablesStartup
{
    private readonly IHostService _hostService;
    private readonly IEnvironmentService _environment;
    private readonly IAppSettings _settings;

    public EnvironmentVariablesStartup(
        IHostService hostService,
        IEnvironmentService environment,
        IAppSettings settings)
    {
        _hostService = hostService;
        _environment = environment;
        _settings = settings;
    }

    /// <inheritdoc />
    public async ValueTask<bool> StartupAsync()
    {
        var environmentVariables = await _hostService.GetEnvironmentVariablesAsync();

        SetEnvironmentVariables(environmentVariables);

        return true;
    }

    private void SetEnvironmentVariables(IDictionary<string, string> environmentVariables)
    {
        foreach (var kvp in environmentVariables)
        {
            if (string.IsNullOrEmpty(kvp.Key))
            {
                continue;
            }

            _environment.SetEnvironmentVariable(kvp.Key, kvp.Value);
            _settings.GlobalProperties[kvp.Key] = kvp.Value;
        }
    }
}
