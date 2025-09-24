// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Runtime.InteropServices;
using Microsoft.DotNet.Scaffolding.Internal.Extensions;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using RuntimeEnvironment = Microsoft.DotNet.PlatformAbstractions.RuntimeEnvironment;

namespace Microsoft.DotNet.Scaffolding.Internal.Telemetry;

/// <summary>
/// This class provides common properties that are used in telemetry events.
/// Not caching or using any cached properties yet, will get to that soon.
/// </summary>
internal class TelemetryCommonProperties
{
    private readonly string _productVersion;
    private readonly IEnvironmentService _environmentService;
    public TelemetryCommonProperties(
        IEnvironmentService environmentService,
        string productVersion)
    {
        _productVersion = productVersion;
        _environmentService = environmentService;
    }

    private const string OSVersion = "OS Version";
    private const string OSPlatform = "OS Platform";
    private const string RuntimeId = "Runtime Id";
    private const string ProductVersion = "Product Version";
    private const string DockerContainer = "Docker Container";
    private const string MacAddressHash = "Mac Address Hash";
    private const string KernelVersion = "Kernel Version";

    public async Task<Dictionary<string, string>> GetTelemetryCommonPropertiesAsync()
    {
        return new Dictionary<string, string>
            {
                {OSVersion, RuntimeEnvironment.OperatingSystemVersion},
                {OSPlatform, RuntimeEnvironment.OperatingSystemPlatform.ToString()},
                {RuntimeId, RuntimeEnvironment.GetRuntimeIdentifier()},
                {ProductVersion, _productVersion},
                {DockerContainer, IsDockerContainer()},
                {MacAddressHash, await GetMacAddressAsync() },
                {KernelVersion, GetKernelVersion()}
            };
    }

    private async Task<string> GetMacAddressAsync()
    {
        string macAddress = await _environmentService.GetMacAddressAsync() ?? string.Empty;
        return macAddress != null ? macAddress.Hash() : Guid.NewGuid().ToString();
    }

    private string IsDockerContainer()
    {
        return _environmentService.IsDockerContainer.ToString();
    }

    /// <summary>
    /// Returns a string identifying the OS kernel.
    /// For Unix this currently comes from "uname -srv".
    /// For Windows this currently comes from RtlGetVersion().
    /// </summary>
    private static string GetKernelVersion()
    {
        return RuntimeInformation.OSDescription;
    }
}
