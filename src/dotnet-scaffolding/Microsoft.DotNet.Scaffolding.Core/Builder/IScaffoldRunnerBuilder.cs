// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Scaffolding.Core.Builder;

public interface IScaffoldRunnerBuilder
{
    /// <summary>
    /// Gets a collection of logging providers for the application to compose. This is useful for adding new logging providers.
    /// </summary>
    ILoggingBuilder Logging { get; }

    /// <summary>
    /// Gets a collection of services for the application to compose. This is useful for adding user provided or framework provided services.
    /// </summary>
    IServiceCollection Services { get; }

    /// <summary>
    /// Gets the collection of <see cref="IScaffoldBuilder"/>s being configured by this <see cref="IScaffoldRunnerBuilder"/>
    /// </summary>
    IEnumerable<IScaffoldBuilder> Scaffolders { get; }

    /// <summary>
    /// Builds the <see cref="IScaffoldRunner"/> from the scaffolders and services configured in the builder.
    /// </summary>
    IScaffoldRunner Build();

    /// <summary>
    /// Adds a new <see cref="IScaffoldBuilder"/> to the builder with the specified name.
    /// </summary>
    /// <param name="name">The name of the scaffolder. This will be used as the command line command to execute that scaffolder.</param>
    IScaffoldBuilder AddScaffolder(string name);
}
