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
    /// Searches multiple SDK locations: DOTNET_ROOT (set by Arcade / CI), then well-known system paths.
    /// Falls back to <see cref="MSBuildLocator.RegisterDefaults"/> when no compatible SDK is found.
    /// </summary>
    private void RegisterMsbuild()
    {
        if (!MSBuildLocator.IsRegistered)
        {
            var sdkSearchPaths = GetSdkSearchPaths();
            if (sdkSearchPaths.Count == 0)
            {
                _logger.LogInformation("Could not find any .NET SDK directories.");
                MSBuildLocator.RegisterDefaults();
                return;
            }

            int runtimeMajor = Environment.Version.Major;

            // Find SDKs whose major version is compatible with the running runtime.
            // A .NET X runtime can safely load MSBuild from SDK version X.y.z (same major)
            // but NOT from a higher major version (e.g. SDK 11.x on .NET 8 runtime).
            // Search across all discovered SDK locations.
            var sdkPaths = sdkSearchPaths
                .SelectMany(basePath => Directory.GetDirectories(basePath))
                .Select(d => new { Path = d, DirName = new DirectoryInfo(d).Name })
                .Where(d => Version.TryParse(d.DirName.Split('-')[0], out _))
                .Select(d => new { d.Path, Version = Version.Parse(d.DirName.Split('-')[0]) })
                .Where(d => d.Version.Major <= runtimeMajor)
                .OrderByDescending(d => d.Version)
                .Select(d => d.Path);

            if (!sdkPaths.Any())
            {
                _logger.LogInformation($"Could not find a .NET SDK compatible with runtime version {Environment.Version} at the searched locations.");
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

    /// <summary>
    /// Returns all existing SDK base directories to search, in priority order.
    /// Checks DOTNET_ROOT (set by Arcade CI and test helpers) first, then
    /// well-known system paths so that SDKs installed outside the default
    /// location are still discovered.
    /// </summary>
    private List<string> GetSdkSearchPaths()
    {
        var paths = new List<string>();

        // 1. Check DOTNET_ROOT — Arcade CI and test helpers set this to the repo's .dotnet/ directory
        var dotnetRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT");
        if (!string.IsNullOrEmpty(dotnetRoot))
        {
            var sdkPath = Path.Combine(dotnetRoot, "sdk");
            if (Directory.Exists(sdkPath))
            {
                paths.Add(sdkPath);
            }
        }

        // 2. Check well-known system SDK paths
        var systemPath = GetDefaultSystemSdkPath();
        if (!string.IsNullOrEmpty(systemPath) && Directory.Exists(systemPath) && !paths.Contains(systemPath))
        {
            paths.Add(systemPath);
        }

        return paths;
    }

    private string GetDefaultSystemSdkPath()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return @"C:\Program Files\dotnet\sdk";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var armPath = @"/usr/local/share/dotnet/sdk";
            return Directory.Exists(armPath) ? armPath : @"/usr/local/share/dotnet/x64";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return @"/usr/share/dotnet/sdk";
        }

        return string.Empty;
    }
}
