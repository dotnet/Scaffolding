// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.CodeModification;
using Microsoft.DotNet.Scaffolding.Roslyn.Services;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Commands.Common;

internal class ProjectInfo
{
    //Project info
    public string? ProjectPath { get; set; }
    public CodeService? CodeService { get; set; }
    public CodeChangeOptions? CodeChangeOptions { get; set; }
}
