// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Scaffolding.Helpers.General
{
    public static class Constants
    {
        public static class EnvironmentVariables
        {
            public const string MSBuildExtensionsPath32 = nameof(MSBuildExtensionsPath32);
            public const string MSBuildExtensionsPath = nameof(MSBuildExtensionsPath);
            public const string MSBUILD_EXE_PATH = nameof(MSBUILD_EXE_PATH);
            public const string MSBuildSDKsPath = nameof(MSBuildSDKsPath);
            public const string USERPROFILE = nameof(USERPROFILE);
            public const string VSINSTALLDIR = nameof(VSINSTALLDIR);
        }
    }
}
