// Copyright (c) Microsoft Corporation. All rights reserved.

namespace Microsoft.DotNet.Scaffolding.Helpers.Services;

/// <summary>
/// Abstracts out host properties. Host represents a tool running a the moment: VS, CLI etc.
/// </summary>
public interface IHostService
{
    /// <summary>
    /// Returns host installation directory.
    /// </summary>
    /// <returns></returns>
    string GetInstallationPath();

    /// <summary>
    /// Returns host specific environment variables to be set in current process.
    /// </summary>
    /// <returns></returns>
    ValueTask<IDictionary<string, string>> GetEnvironmentVariablesAsync();
}
