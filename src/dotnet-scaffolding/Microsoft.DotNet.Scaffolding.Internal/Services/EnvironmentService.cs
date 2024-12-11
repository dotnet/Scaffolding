// Copyright (c) Microsoft Corporation. All rights reserved.
using Microsoft.DotNet.Scaffolding.Internal.Shared;

namespace Microsoft.DotNet.Scaffolding.Internal.Services;
/// <summary>
/// Wrapper over System.Environment abstraction for unit testing.
/// </summary>
internal class EnvironmentService : IEnvironmentService
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

    public static string LocalUserProfilePath => EnvironmentHelpers.GetUserProfilePath();

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
        return Environment.MachineName;
    }

    /// <inheritdoc />
    public string? GetEnvironmentVariable(string name)
    {
        return Environment.GetEnvironmentVariable(name);
    }

    /// <inheritdoc />
    public bool GetEnvironmentVariableAsBool(string name, bool defaultValue = false)
    {
        return EnvironmentHelpers.GetEnvironmentVariableAsBool(name, defaultValue);
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

    public string? GetMacAddress()
    {
        return MacAddressGetter.GetMacAddress();
    }

    private bool _didCheckForContainer;
    private IsDockerContainer _isDockerContainer;
    public IsDockerContainer IsDockerContainer
    {
        get
        {
            if (!_didCheckForContainer)
            {
                _didCheckForContainer = true;
                _isDockerContainer = DockerContainerDetector.IsDockerContainer();
            }

            return _isDockerContainer;
        }
    }
}

