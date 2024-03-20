// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.Scaffolding.ComponentModel
{
    public class DotNetToolInfo
    {
        public string PackageName { get; set; } = default!;
        public string Version { get; set; } = default!;
        public string Command { get; set; } = default!;

        public string ToDisplayString()
        {
            return $"{Command} ({PackageName} v{Version})";
        }
    }
}
