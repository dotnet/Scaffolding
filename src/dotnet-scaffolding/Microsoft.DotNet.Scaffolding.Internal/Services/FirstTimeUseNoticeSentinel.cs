// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.DotNet.Scaffolding.Internal.Telemetry;

namespace Microsoft.DotNet.Scaffolding.Internal.Services;

internal class FirstTimeUseNoticeSentinel : IFirstTimeUseNoticeSentinel
{
    private readonly IFileSystem _fileSystem;
    private readonly IEnvironmentService _environmentService;
    private readonly string _sentinel;
    private readonly string _dotnetUserProfileFolderPath;

    private string SentinelPath => Path.Combine(_dotnetUserProfileFolderPath, _sentinel);

    public FirstTimeUseNoticeSentinel(IFileSystem fileSystem, IEnvironmentService environmentService, string productName)
    {
        _fileSystem = fileSystem;
        _environmentService = environmentService;
        _sentinel = $"{ProductFullVersion}.{productName}{TelemetryConstants.SENTINEL_SUFFIX}";
        _dotnetUserProfileFolderPath = _environmentService.DotnetUserProfilePath;
        SkipFirstTimeExperience = _environmentService.GetEnvironmentVariableAsBool(TelemetryConstants.SKIP_FIRST_TIME_EXPERIENCE);
    }

    public string Title => Resources.Strings.FirstTimeUseNoticeTitleText;

    public string DisclosureText => string.Format(Resources.Strings.FirstTimeUseNoticeDisclosureText, TelemetryConstants.TELEMETRY_OPTOUT);

    public bool SkipFirstTimeExperience { get; }

    public void CreateIfNotExists()
    {
        if (!Exists())
        {
            if (!_fileSystem.DirectoryExists(_dotnetUserProfileFolderPath))
            {
                _fileSystem.CreateDirectory(_dotnetUserProfileFolderPath);
            }

            _fileSystem.WriteAllText(SentinelPath, string.Empty);
        }
    }

    public bool Exists() => _fileSystem.FileExists(SentinelPath);

    public string ProductFullVersion
        => Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ??
           typeof(FirstTimeUseNoticeSentinel).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
}
