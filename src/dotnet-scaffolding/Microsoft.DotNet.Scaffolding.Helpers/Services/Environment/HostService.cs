// Copyright (c) Microsoft Corporation. All rights reserved.
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Scaffolding.Helpers.Services.Environment;

internal class HostService : IHostService
{
    private readonly ILogger _logger;
    private readonly IEnumerable<IEnvironmentVariableProvider> _providers;
    private Dictionary<string, string>? _variables;

    public HostService(ILogger<HostService> logger, IEnumerable<IEnvironmentVariableProvider> providers)
    {
        _logger = logger;
        _providers = providers;
    }

    /// <inheritdoc />
    public string GetInstallationPath()
    {
        return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
    }

    /// <inheritdoc />
    public async ValueTask<IDictionary<string, string>> GetEnvironmentVariablesAsync()
    {
        if (_variables is null)
        {
            var variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var provider in _providers)
            {
                try
                {
                    var providerVariables = await provider.GetEnvironmentVariablesAsync();
                    if (providerVariables is not null)
                    {
                        foreach (var kvp in providerVariables)
                        {
                            variables[kvp.Key] = kvp.Value;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                }
            }

            _variables = variables;
        }

        return _variables;
    }
}
