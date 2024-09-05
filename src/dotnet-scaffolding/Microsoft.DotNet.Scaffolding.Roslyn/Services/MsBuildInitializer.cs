// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Runtime.InteropServices;
using Microsoft.Build.Locator;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Scaffolding.Roslyn.Services;

internal class MsBuildInitializer
{
    private readonly ILogger _logger;
    public MsBuildInitializer(ILogger logger)
    {
        _logger = logger;
    }

    public void Initialize()
    {
        RegisterMsbuild();
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
                _logger.LogInformation($"Could not find a .NET SDK at the default locations.");
                MSBuildLocator.RegisterDefaults();
                return;
            }

            //register newest MSBuild from the newest dotnet sdk installed.
            var sdkPath = Directory.GetDirectories(sdkBasePath)
                                  .OrderByDescending(d => new DirectoryInfo(d).Name)
                                  .FirstOrDefault();

            if (string.IsNullOrEmpty(sdkPath))
            {
                _logger.LogInformation($"Could not find a .NET SDK at the default locations.");
                MSBuildLocator.RegisterDefaults();
                return;
            }

            var msbuildPath = Path.Combine(sdkPath, "MSBuild.dll");
            if (File.Exists(msbuildPath))
            {
                // Register the latest SDK
                MSBuildLocator.RegisterMSBuildPath(sdkPath);
            }
            else
            {
                _logger.LogInformation($"MSBuild.dll not found in the SDK path '{sdkPath}'.");
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
            sdkBasePath = @"/usr/local/share/dotnet/x64/sdk";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            sdkBasePath = @"/usr/share/dotnet/sdk";
            if (!Directory.Exists(sdkBasePath))
            {
                sdkBasePath = @"/usr/local/share/dotnet/sdk";
            }
        }

        return sdkBasePath;
    }
}
