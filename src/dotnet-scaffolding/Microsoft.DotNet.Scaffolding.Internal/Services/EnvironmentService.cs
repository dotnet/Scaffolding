// Copyright (c) Microsoft Corporation. All rights reserved.
using System.Runtime.InteropServices;

namespace Microsoft.DotNet.Scaffolding.Internal.Services;
/// <summary>
/// Wrapper over System.Environment abstraction for unit testing.
/// </summary>
public class EnvironmentService : IEnvironmentService
{
    private readonly IFileSystem _fileSystem;
    public EnvironmentService(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    private const string DOTNET_RUNNING_IN_CONTAINER = nameof(DOTNET_RUNNING_IN_CONTAINER);

    private static string? _nugetCache;
    public static string LocalNugetCachePath
    {
        get
        {
            if (string.IsNullOrEmpty(_nugetCache))
            {
                var nugetPackagesEnvironmentVariable = System.Environment.GetEnvironmentVariable("NUGET_PACKAGES");

                _nugetCache = string.IsNullOrWhiteSpace(nugetPackagesEnvironmentVariable)
                    ? Path.Combine(LocalUserProfilePath, ".nuget", "packages")
                    : nugetPackagesEnvironmentVariable;
            }

            return _nugetCache!;
        }
    }

    public static string LocalUserProfilePath
    {
        get
        {
            return System.Environment.GetEnvironmentVariable(
                    RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                        ? "USERPROFILE"
                        : "HOME") ?? "USERPROFILE";
        }
    }

    public const string DotnetProfileDirectoryName = ".dotnet";

    /// <inheritdoc />
    public string CurrentDirectory => System.Environment.CurrentDirectory;

    /// <inheritdoc />
    public OperatingSystem OS => System.Environment.OSVersion;

    /// <inheritdoc />
    public bool Is64BitOperatingSystem => System.Environment.Is64BitOperatingSystem;

    /// <inheritdoc />
    public bool Is64BitProcess => System.Environment.Is64BitProcess;

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
                var homePath = System.Environment.GetEnvironmentVariable("DOTNET_CLI_HOME");
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

    private string? _localUserFolderPath;
    public string LocalUserFolderPath
    {
        get
        {
            if (string.IsNullOrEmpty(_localUserFolderPath))
            {
                _localUserFolderPath = LocalUserProfilePath;
            }

            return _localUserFolderPath!;
        }
    }

    /// <inheritdoc />
    public string GetMachineName()
    {
        return System.Environment.MachineName;
    }

    /// <inheritdoc />
    public string? GetEnvironmentVariable(string name)
    {
        return System.Environment.GetEnvironmentVariable(name);
    }

    /// <inheritdoc />
    public void SetEnvironmentVariable(string name, string value, EnvironmentVariableTarget envTarget)
    {
        System.Environment.SetEnvironmentVariable(name, value, envTarget);
    }

    /// <inheritdoc />
    public string GetFolderPath(System.Environment.SpecialFolder specifalFolder)
    {
        return System.Environment.GetFolderPath(specifalFolder);
    }

    /// <inheritdoc />
    public string ExpandEnvironmentVariables(string name)
    {
        return System.Environment.ExpandEnvironmentVariables(name);
    }
}

