using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using Microsoft.Build.Locator;

namespace Microsoft.DotNet.Scaffolding.Helpers.Services;

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
            //register newest MSBuild from the newest dotnet sdk installed.
            var sdkPath = Directory.GetDirectories(sdkBasePath)
                                  .OrderByDescending(d => new DirectoryInfo(d).Name)
                                  .FirstOrDefault();

            if (!Directory.Exists(sdkBasePath) || string.IsNullOrEmpty(sdkPath))
            {
                _logger.LogMessage($"Could not find a .NET SDK at the default locations.");
                MSBuildLocator.RegisterDefaults();
                return;
            }

            var msbuildPath = Path.Combine(sdkPath, "MSBuild.dll");
            if (File.Exists(msbuildPath))
            {
                // Register the latest SDK
                MSBuildLocator.RegisterMSBuildPath(sdkPath);
                //MSBuildLocator.RegisterDefaults();
                _logger.LogMessage($"Registered .NET SDK at {sdkPath}");
            }
            else
            {
                _logger.LogMessage($"MSBuild.dll not found in the SDK path '{sdkPath}'.");
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
