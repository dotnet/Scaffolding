// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Runtime.InteropServices;
using Microsoft.DotNet.Scaffolding.Helpers.General;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Scaffolding.Helpers.Services.Environment;

internal class MacMsbuildEnvironmentVariableProvider : IEnvironmentVariableProvider
{
    private const string MacOSMonoFrameworkMSBuildExtensionsDir = "/Library/Frameworks/Mono.framework/External/xbuild";

    private readonly IAppSettings _settings;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger _logger;

    public MacMsbuildEnvironmentVariableProvider(IAppSettings settings, IFileSystem fileSystem, ILogger logger)
    {
        _settings = settings;
        _fileSystem = fileSystem;
        _logger = logger;
    }

    /// <inheritdoc />
    public ValueTask<IEnumerable<KeyValuePair<string, string>>?> GetEnvironmentVariablesAsync()
    {
        // Note: try to do it for Mac and Linux.
        return !RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? GetMacEnvironmentVariablesAsync()
            : new ValueTask<IEnumerable<KeyValuePair<string, string>>?>((IEnumerable<KeyValuePair<string, string>>?)null);
    }

    private ValueTask<IEnumerable<KeyValuePair<string, string>>?> GetMacEnvironmentVariablesAsync()
    {
        var variables = new Dictionary<string, string>();
        var msbuildPath = _settings.Workspace().MSBuildPath;

        // If the user-settable MSBuildPath is null/empty at this point, then use the
        // "MSBuildExtensionsPath" environment variable which *should* have been set
        // by the MSBuildLocator.
        if (string.IsNullOrEmpty(msbuildPath))
        {
            msbuildPath = System.Environment.GetEnvironmentVariable("MSBuildExtensionsPath");
        }

        var msbuildExtensionsPath = GetMacOSMSBuildExtensionsPath(msbuildPath);

        if (!string.IsNullOrEmpty(msbuildExtensionsPath))
        {
            _settings.Workspace().MSBuildPath = msbuildExtensionsPath;
            variables.Add(Constants.EnvironmentVariables.MSBuildExtensionsPath32, msbuildExtensionsPath);
            variables.Add(Constants.EnvironmentVariables.MSBuildExtensionsPath, msbuildExtensionsPath);

            var msbuildExePath = Path.Combine(msbuildExtensionsPath, "MSBuild.dll");
            variables.Add(Constants.EnvironmentVariables.MSBUILD_EXE_PATH, msbuildExePath);

            var msbuildSdksPath = Path.Combine(msbuildExtensionsPath, "Sdks");
            variables.Add(Constants.EnvironmentVariables.MSBuildSDKsPath, msbuildSdksPath);
        }

        variables.Add(Constants.EnvironmentVariables.USERPROFILE, System.Environment.GetEnvironmentVariable("HOME") ?? string.Empty);

        return new ValueTask<IEnumerable<KeyValuePair<string, string>>?>(variables);
    }

    private string? GetMacOSMSBuildExtensionsPath(string? suppliedPath)
    {
        const string DefaultDotnetSdkLocation = "/usr/local/share/dotnet/sdk/";

        if (string.IsNullOrEmpty(suppliedPath) || !suppliedPath.StartsWith(DefaultDotnetSdkLocation, StringComparison.Ordinal))
        {
            return suppliedPath;
        }

        string? msbuildExtensionsPath = null;

        if (_fileSystem.DirectoryExists(MacOSMonoFrameworkMSBuildExtensionsDir))
        {
            // Check to see if the specified MSBuildPath contains the Mono.framework build extensions.
            var monoExtensionDirectories = _fileSystem.EnumerateDirectories(MacOSMonoFrameworkMSBuildExtensionsDir, "*.*", SearchOption.TopDirectoryOnly);
            var createTempExtensionsDir = false;

            foreach (var monoExtensionDir in monoExtensionDirectories)
            {
                var dotnetExtensionDir = Path.Combine(suppliedPath, Path.GetFileName(monoExtensionDir));
                if (!_fileSystem.DirectoryExists(dotnetExtensionDir))
                {
                    createTempExtensionsDir = true;
                    break;
                }
            }

            // If the specified MSBuildPath does not contain the Mono.framework build extensions, create a temp
            // directory that we'll use to symlink everything.
            if (createTempExtensionsDir)
            {
                var homeDir = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
                //var versionDir = Path.GetFileName(suppliedPath.TrimEndPathSeparators());
                var versionDir = suppliedPath;
                msbuildExtensionsPath = Path.Combine(homeDir, ".dotnet-upgrade-assistant", "dotnet-sdk", versionDir);

                if (!_fileSystem.DirectoryExists(msbuildExtensionsPath))
                {
                    _fileSystem.CreateDirectory(msbuildExtensionsPath);
                }

                // First, create symbolic links to all of the dotnet MSBuild file system entries.
                CreateSymbolicLinks(msbuildExtensionsPath, suppliedPath);

                // Then create the symbolic links to the Mono.framework/External/xbuild system entries.
                CreateSymbolicLinks(msbuildExtensionsPath, MacOSMonoFrameworkMSBuildExtensionsDir);
            }
        }

        return msbuildExtensionsPath ?? suppliedPath;
    }

    private static void CreateSymbolicLinks(string targetDir, string sourceDir)
    {
        foreach (var entry in Directory.EnumerateFileSystemEntries(sourceDir))
        {
            var target = Path.Combine(targetDir, Path.GetFileName(entry));

            var fileInfo = new FileInfo(target);
            if (fileInfo.Exists)
            {
                if (fileInfo.LinkTarget is not null && fileInfo.LinkTarget.Equals(entry, StringComparison.Ordinal))
                {
                    continue;
                }

                File.Delete(target);
            }
            else
            {
                var dirInfo = new DirectoryInfo(target);
                if (dirInfo.Exists)
                {
                    if (dirInfo.LinkTarget is not null && dirInfo.LinkTarget.Equals(entry, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    Directory.Delete(target);
                }
            }

            File.CreateSymbolicLink(target, entry);
        }
    }
}
