// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.Tools.Scaffold.Services;

internal interface IToolManager
{
    bool AddTool(string packageName, string[] addSources, string? configFile, bool prerelease, string? version, bool global);
    bool RemoveTool(string packageName, bool global);
    void ListTools();
}
