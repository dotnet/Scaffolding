// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Scaffolding.Core.Builder;

public interface IScaffoldRunnerBuilder
{
    ILoggingBuilder Logging { get; }
    IServiceCollection Services { get; }
    IEnumerable<IScaffoldBuilder> Scaffolders { get; }
    IScaffoldRunner Build();
    IScaffoldBuilder AddScaffolder(string name);
}
