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
        RegisterMsbuild();
    }

    /// <summary>
    /// Find a compatible dotnet SDK on disk and register its MSBuild.
    /// Picks the newest SDK whose major version is less than or equal to the current runtime's major version,
    /// so that loading MSBuild assemblies does not pull in a higher System.Runtime than the runtime supports.
    /// Falls back to <see cref="MSBuildLocator.RegisterDefaults"/> when no compatible SDK is found.
    /// </summary>
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

            int runtimeMajor = Environment.Version.Major;

            // Find SDKs whose major version is compatible with the running runtime.
            // A .NET X runtime can safely load MSBuild from SDK version X.y.z (same major)
            // but NOT from a higher major version (e.g. SDK 11.x on .NET 8 runtime).
            var sdkPaths = Directory.GetDirectories(sdkBasePath)
                .Select(d => new { Path = d, DirName = new DirectoryInfo(d).Name })
                .Where(d => Version.TryParse(d.DirName.Split('-')[0], out _))
                .Select(d => new { d.Path, Version = Version.Parse(d.DirName.Split('-')[0]) })
                .Where(d => d.Version.Major <= runtimeMajor)
                .OrderByDescending(d => d.Version)
                .Select(d => d.Path);

            if (!sdkPaths.Any())
            {
                _logger.LogInformation($"Could not find a .NET SDK compatible with runtime version {Environment.Version} at the default locations.");
                MSBuildLocator.RegisterDefaults();
                return;
            }

            foreach (var sdkPath in sdkPaths)
            {
                var msbuildPath = Path.Combine(sdkPath, "MSBuild.dll");
                if (File.Exists(msbuildPath))
                {
                    // Register the best compatible SDK
                    MSBuildLocator.RegisterMSBuildPath(sdkPath);
                    break;
                }
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
