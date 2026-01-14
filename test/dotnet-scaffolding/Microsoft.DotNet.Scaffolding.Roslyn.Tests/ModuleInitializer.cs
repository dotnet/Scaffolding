// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Runtime.CompilerServices;
using Microsoft.Build.Locator;

namespace Microsoft.DotNet.Scaffolding.Roslyn.Tests;

internal static class ModuleInitializer
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        if (!MSBuildLocator.IsRegistered)
        {
            MSBuildLocator.RegisterDefaults();
        }
    }
}
