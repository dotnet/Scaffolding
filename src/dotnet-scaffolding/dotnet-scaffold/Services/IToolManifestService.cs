// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Tools.Scaffold.Models;

namespace Microsoft.DotNet.Tools.Scaffold.Services;

internal interface IToolManifestService
{
    bool AddTool(string toolName);
    ScaffoldManifest GetManifest();
    bool RemoveTool(string toolName);
}
