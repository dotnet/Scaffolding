// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.Tools.Scaffold.Models;

internal class ScaffoldManifest
{
    public required string Version { get; init; }
    public required IList<ScaffoldTool> Tools { get; init; }

    public bool HasTool(string name)
        => Tools.Any(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));
}
