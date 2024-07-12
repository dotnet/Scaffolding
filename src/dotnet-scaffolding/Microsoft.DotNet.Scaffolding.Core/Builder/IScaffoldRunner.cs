// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;

namespace Microsoft.DotNet.Scaffolding.Core.Builder;

public interface IScaffoldRunner
{
    IEnumerable<IScaffolder>? Scaffolders { get; set; }
    Task RunAsync(string[] args);
}
