// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Models;

internal class CrudModel
{
    public required string PageType { get; init; }
    public required DbContextInfo DbContextInfo { get; init; }
    public required ModelInfo ModelInfo { get; init; }
    public required ProjectInfo ProjectInfo { get; init; }
}
