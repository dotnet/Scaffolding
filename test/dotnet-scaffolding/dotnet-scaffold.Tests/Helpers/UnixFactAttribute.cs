// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.Helpers;

internal sealed class UnixFactAttribute : FactAttribute
{
    public UnixFactAttribute()
    {
        if (OperatingSystem.IsWindows())
        {
            Skip = "Directory symlink creation requires additional privileges on Windows.";
        }
    }
}
