// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Security;
using Microsoft.Win32;

namespace Microsoft.DotNet.Scaffolding.Internal.Services;
/// <summary>
/// Copied from dotnet/HttpRepl/blob/main/src/Microsoft.HttpRepl.Telemetry/DockerContainerDetectorForTelemetry.cs
/// </summary>
internal static class DockerContainerDetector
{
    public static IsDockerContainer IsDockerContainer()
    {
        if (OperatingSystem.IsWindows())
        {
            try
            {
                using (RegistryKey? subkey
                    = Registry.LocalMachine.OpenSubKey("System\\CurrentControlSet\\Control"))
                {
                    return subkey?.GetValue("ContainerType") != null
                        ? Services.IsDockerContainer.True
                        : Services.IsDockerContainer.False;
                }
            }
            catch (SecurityException)
            {
                return Services.IsDockerContainer.Unknown;
            }
        }
        else if (OperatingSystem.IsLinux())
        {   
            try
            {
                bool isDocker = File
                    .ReadAllText("/proc/1/cgroup")
                    .Contains("/docker/", StringComparison.Ordinal);

                return isDocker
                    ? Services.IsDockerContainer.True
                    : Services.IsDockerContainer.False;
            }
            catch (Exception ex) when (ex is IOException || ex.InnerException is IOException)
            {
                // in some environments (restricted docker container, shared hosting etc.),
                // procfs is not accessible and we get UnauthorizedAccessException while the
                // inner exception is set to IOException. Ignore and continue when that happens.
            }
        }
        else if (OperatingSystem.IsMacOS())
        {
            return Services.IsDockerContainer.False;
        }

        return Services.IsDockerContainer.Unknown;
    }
}

internal enum IsDockerContainer
{
    True,
    False,
    Unknown
}
