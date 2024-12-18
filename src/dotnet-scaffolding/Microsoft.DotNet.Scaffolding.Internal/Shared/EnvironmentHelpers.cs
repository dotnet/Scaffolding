// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Runtime.InteropServices;

namespace Microsoft.DotNet.Scaffolding.Internal.Shared;

internal static class EnvironmentHelpers
{
    public static bool GetEnvironmentVariableAsBool(string name, bool defaultValue = false)
    {
        var str = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrEmpty(str))
        {
            return defaultValue;
        }

        switch (str.ToLowerInvariant())
        {
            case "true":
            case "1":
            case "yes":
                return true;
            case "false":
            case "0":
            case "no":
                return false;
            default:
                return defaultValue;
        }
    }

    public static string GetUserProfilePath()
    {
        return Environment.GetEnvironmentVariable(
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "USERPROFILE"
                : "HOME") ?? "USERPROFILE";
    }
}
