// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.Builder;

namespace Microsoft.DotNet.Scaffolding.Core.Scaffolders;

public interface IScaffolder
{
    string Name { get; }
    string DisplayName { get; }
    string Category { get; }
    string? Description { get; }
    IEnumerable<ScaffolderOption> Options { get; }
    Task ExecuteAsync(ScaffolderContext context);
}
