// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Helpers.Roslyn;
using Microsoft.DotNet.Scaffolding.Helpers.Services;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Commands.Common;

internal class ProjectInfo
{
    //Project info
    public IAppSettings? AppSettings { get; set; }
    public ICodeService? CodeService { get; set; }
    public CodeChangeOptions? CodeChangeOptions { get; set; }
}
