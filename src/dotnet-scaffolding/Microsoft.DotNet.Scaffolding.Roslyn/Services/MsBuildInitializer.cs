// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Runtime.InteropServices;
using Microsoft.Build.Locator;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Scaffolding.Roslyn.Services;

public class MsBuildInitializer
{
    private readonly ILogger _logger;
    public MsBuildInitializer(ILogger logger)
    {
        _logger = logger;
    }

    public void Initialize()
    {
        _logger.LogTrace($"Initializing MSBuild in '{nameof(MsBuildInitializer)}.Initialize()...'");
        RegisterMsbuild();
        _logger.LogTrace("Done.\n");
    }

    /// <summary>
    /// find the newest dotnet sdk on disk first, if none found, use the MsBuildLocator.RegisterDefaults().
    /// </summary>
    /// <returns></returns>
    private void RegisterMsbuild()
    {
        if (!MSBuildLocator.IsRegistered)
        {
            // Path to the directory containing the SDKs
            string sdkBasePath = GetDefaultSdkPath();
            if (!Directory.Exists(sdkBasePath))
            {
                _logger.LogDebug($"Could not find a .NET SDK at the default locations.");
                MSBuildLocator.RegisterDefaults();
                return;
            }

            //register newest MSBuild from the newest dotnet sdk installed.
            var sdkPaths = Directory.GetDirectories(sdkBasePath)
                .Select(d => new { Path = d, new DirectoryInfo(d).Name })
                .Where(d => Version.TryParse(d.Name.Split('-')[0], out _))
                .OrderByDescending(d => Version.Parse(d.Name.Split('-')[0]))
                .Select(d => d.Path);

            if (!sdkPaths.Any())
            {
                _logger.LogDebug($"Could not find a .NET SDK at the default locations.");
                MSBuildLocator.RegisterDefaults();
                return;
            }

            foreach (var sdkPath in sdkPaths)
            {
                if (File.Exists(sdkPath))
                {
                    // Register the latest SDK
                    _logger.LogTrace($"Registering MSBuild at path '{sdkPath}'");
                    MSBuildLocator.RegisterMSBuildPath(sdkPath);
                    break;
                }
            }

            if (!MSBuildLocator.IsRegistered)
            {
                _logger.LogDebug($"MSBuild.dll not found at default locations.");
                MSBuildLocator.RegisterDefaults();
            }
        }
    }

    private string GetDefaultSdkPath()
    {
        string sdkBasePath = string.Empty;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            sdkBasePath = @"C:\Program Files\dotnet\sdk";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            //check for the ARM sdk first
            sdkBasePath = @"/usr/local/share/dotnet/sdk";
            if (!Directory.Exists(sdkBasePath))
            {
                sdkBasePath = @"/usr/local/share/dotnet/x64";
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            sdkBasePath = @"/usr/share/dotnet/sdk";
        }

        return sdkBasePath;
    }
}
