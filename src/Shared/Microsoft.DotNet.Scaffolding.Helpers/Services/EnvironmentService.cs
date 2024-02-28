// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.DotNet.Scaffolding.Helpers.Services;

/// <summary>
/// Wrapper over System.Environment abstraction for unit testing.
/// </summary>
public class EnvironmentService : IEnvironmentService
{
    private const string DOTNET_RUNNING_IN_CONTAINER = nameof(DOTNET_RUNNING_IN_CONTAINER);

    private static string? _nugetCache;
    public static string LocalNugetCachePath
    {
        get
        {
            if (string.IsNullOrEmpty(_nugetCache))
            {
                var nugetPackagesEnvironmentVariable = Environment.GetEnvironmentVariable("NUGET_PACKAGES");

                _nugetCache = string.IsNullOrWhiteSpace(nugetPackagesEnvironmentVariable)
                    ? Path.Combine(LocalUserProfilePath, ".nuget", "packages")
                    : nugetPackagesEnvironmentVariable;
            }

            return _nugetCache!;
        }
    }

    public static string LocalUserProfilePath => Environment.GetEnvironmentVariable(
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "USERPROFILE"
            : "HOME") ?? "USERPROFILE";

    public const string DotnetProfileDirectoryName = ".dotnet";
    private readonly IFileSystem _fileSystem;

    public EnvironmentService(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    /// <inheritdoc />
    public string CurrentDirectory => Environment.CurrentDirectory;

    /// <inheritdoc />
    public OperatingSystem OS => Environment.OSVersion;

    /// <inheritdoc />
    public bool Is64BitOperatingSystem => Environment.Is64BitOperatingSystem;

    /// <inheritdoc />
    public bool Is64BitProcess => Environment.Is64BitProcess;

    /// <inheritdoc />
    public string UserProfilePath => LocalNugetCachePath;

    /// <inheritdoc />
    public string? DomainName => System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().DomainName;

    /// <inheritdoc />
    public string NugetCachePath => LocalNugetCachePath;

    private string? _dotnetHomePath;

    /// <inheritdoc />
    public string DotnetUserProfilePath
    {
        get
        {
            if (string.IsNullOrEmpty(_dotnetHomePath))
            {
                var homePath = Environment.GetEnvironmentVariable("DOTNET_CLI_HOME");
                if (string.IsNullOrEmpty(homePath))
                {
                    homePath = UserProfilePath;
                }

                if (!string.IsNullOrEmpty(homePath))
                {
                    homePath = Path.Combine(LocalUserProfilePath, DotnetProfileDirectoryName);
                }

                _dotnetHomePath = homePath ?? string.Empty;
            }

            return _dotnetHomePath!;
        }
    }

    /// <inheritdoc />
    public string GetMachineName()
    {
        return Environment.MachineName;
    }

    /// <inheritdoc />
    public string? GetEnvironmentVariable(string name)
    {
        return Environment.GetEnvironmentVariable(name);
    }

    /// <inheritdoc />
    public void SetEnvironmentVariable(string name, string value, EnvironmentVariableTarget envTarget)
    {
        Environment.SetEnvironmentVariable(name, value, envTarget);
    }

    /// <inheritdoc />
    public string GetFolderPath(Environment.SpecialFolder specifalFolder)
    {
        return Environment.GetFolderPath(specifalFolder);
    }

    /// <inheritdoc />
    public string ExpandEnvironmentVariables(string name)
    {
        return Environment.ExpandEnvironmentVariables(name);
    }
}

