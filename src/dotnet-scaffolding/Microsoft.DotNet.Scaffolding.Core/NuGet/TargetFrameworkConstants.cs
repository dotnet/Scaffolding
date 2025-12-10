// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Microsoft.DotNet.Scaffolding.Core.Model;

internal static class TargetFrameworkConstants
{
    public const string TargetFrameworkCliOption = "--framework";
    public const string TargetFrameworkDisplayName = "Target Framework";
    public const string TargetFrameworkDescription = "Specifies the target framework for the scaffolded project.";

    public const string Net8 = "net8.0";
    public const string Net9 = "net9.0";
    public const string Net10 = "net10.0";
    public const string Net11 = "net11.0";

    public static readonly ImmutableArray<string> SupportedTargetFrameworks = [ Net8, Net9, Net10, Net11];
}
