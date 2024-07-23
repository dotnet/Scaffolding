// Copyright (c) Microsoft Corporation. All rights reserved.
using System.Runtime.InteropServices;
using Microsoft.DotNet.Scaffolding.Helpers.Extensions;
using Microsoft.DotNet.Scaffolding.Helpers.General;
using Microsoft.DotNet.Scaffolding.Helpers.Services;
using Microsoft.DotNet.Scaffolding.Helpers.Services.Environment;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Setup.Configuration;

namespace Microsoft.DotNet.Scaffolding.Helpers.Environment;
internal class WindowsEnvironmentVariableProvider : IEnvironmentVariableProvider
{
    private const int REGDB_E_CLASSNOTREG = unchecked((int)0x80040154);

    private readonly IAppSettings _settings;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger _logger;

    public WindowsEnvironmentVariableProvider(IAppSettings settings, IFileSystem fileSystem, ILogger logger)
    {
        _settings = settings;
        _fileSystem = fileSystem;
        _logger = logger;
    }

    /// <inheritdoc />
    public ValueTask<IEnumerable<KeyValuePair<string, string>>?> GetEnvironmentVariablesAsync()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? GetWindowsEnvironmentVariablesAsync()
            : new ValueTask<IEnumerable<KeyValuePair<string, string>>?>((IEnumerable<KeyValuePair<string, string>>?)null);
    }

    private ValueTask<IEnumerable<KeyValuePair<string, string>>?> GetWindowsEnvironmentVariablesAsync()
    {
        var variables = new Dictionary<string, string>();

        var workspace = _settings.Workspace();
        var (visualStudioPath, visualStudioVersion) = GetLatestVisualStudioPath(workspace.VisualStudioPath);
        if (!string.IsNullOrEmpty(visualStudioPath))
        {
            // TODO mark this class as IStartup explicitly so it does initialize all settings it can.
            workspace.VisualStudioPath ??= visualStudioPath.WithOsPathSeparators();
            workspace.MSBuildPath ??= Path.Combine(visualStudioPath, "MSBuild");

            variables.Add(Constants.EnvironmentVariables.VSINSTALLDIR, workspace.VisualStudioPath);
            variables.Add(Constants.EnvironmentVariables.MSBuildExtensionsPath32, workspace.MSBuildPath);
            variables.Add(Constants.EnvironmentVariables.MSBuildExtensionsPath, workspace.MSBuildPath);
        }

        if (visualStudioVersion is int version)
        {
            workspace.VisualStudioVersion ??= $"{version}.0";

            variables.Add("VisualStudioVersion", workspace.VisualStudioVersion);
        }

        variables.Add(Constants.EnvironmentVariables.USERPROFILE, System.Environment.GetEnvironmentVariable(Constants.EnvironmentVariables.USERPROFILE) ?? string.Empty);

        return new ValueTask<IEnumerable<KeyValuePair<string, string>>?>(variables);
    }

    private (string? Path, int? Version) GetLatestVisualStudioPath(string? suppliedPath)
    {
        var latest = GetLatestVisualStudio(suppliedPath);

        if (latest.InstallPath is null)
        {
            _logger.LogInformation("Did not find a Visual Studio instance");
            return default;
        }

        if (_fileSystem.DirectoryExists(latest.InstallPath))
        {
            return (latest.InstallPath, latest.Version.Major);
        }
        else
        {
           return default;
        }
    }

    private (string? InstallPath, Version Version) GetLatestVisualStudio(string? suppliedPath)
    {
        var resultVersion = new Version(0, 0);
        string? resultPath = null;

        try
        {
            // This code is not obvious. See the sample (link above) for reference.
            var query = (ISetupConfiguration2)GetQuery();
            var e = query.EnumAllInstances();

            int fetched;
            var instances = new ISetupInstance[1];
            do
            {
                // Call e.Next to query for the next instance (single item or nothing returned).
                e.Next(1, instances, out fetched);
                if (fetched <= 0)
                {
                    continue;
                }

                var instance = (ISetupInstance2)instances[0];
                var state = instance.GetState();

                if (!Version.TryParse(instance.GetInstallationVersion(), out var version))
                {
                    continue;
                }

                // If the install was complete and a valid version, consider it.
                if (state == InstanceState.Complete ||
                    (state.HasFlag(InstanceState.Registered) && state.HasFlag(InstanceState.NoRebootRequired)))
                {
                    var instanceHasMSBuild = false;

                    foreach (var package in instance.GetPackages())
                    {
                        if (string.Equals(package.GetId(), "Microsoft.Component.MSBuild", StringComparison.OrdinalIgnoreCase))
                        {
                            instanceHasMSBuild = true;
                            break;
                        }
                    }

                    if (instanceHasMSBuild && instance is not null)
                    {
                        var installPath = instance.GetInstallationPath();

                        if (suppliedPath is not null && string.Equals(suppliedPath, installPath, StringComparison.OrdinalIgnoreCase))
                        {
                            return (installPath, version);
                        }
                        else if (version > resultVersion)
                        {
                             resultPath = installPath;
                            resultVersion = version;
                        }
                    }
                }
            }
            while (fetched > 0);
        }
        catch (COMException)
        {
        }
        catch (DllNotFoundException)
        {
            // This is OK, VS "15" or greater likely not installed.
        }

        return (resultPath, resultVersion);
    }

    private static ISetupConfiguration GetQuery()
    {
        try
        {
            // Try to CoCreate the class object.
            return new SetupConfiguration();
        }
        catch (COMException ex) when (ex.ErrorCode == REGDB_E_CLASSNOTREG)
        {
            // Try to get the class object using app-local call.
            var result = NativeMethods.GetSetupConfiguration(out var query, IntPtr.Zero);

            if (result < 0)
            {
                throw;
            }

            return query;
        }
    }
}

