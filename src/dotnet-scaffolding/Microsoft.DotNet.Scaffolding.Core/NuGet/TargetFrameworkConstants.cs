// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.Scaffolding.Core.Model;

internal class TargetFrameworkConstants
{
    public const string TargetFrameworkCliOption = "--framework";
    public const string TargetFrameworkDisplayName = "Target Framework";

    public const string Net8 = "net8.0";
    public const string Net9 = "net9.0";
    public const string Net10 = "net10.0";

    public static readonly List<string> SupportedTargetFrameworks = [ Net8, Net9, Net10];
}
