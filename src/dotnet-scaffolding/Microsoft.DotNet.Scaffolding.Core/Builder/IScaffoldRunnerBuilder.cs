// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.CommandLine.Invocation;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;

namespace Microsoft.DotNet.Scaffolding.Core.Builder;

/// <summary>
/// Interface for configuring and building a scaffold runner with services and scaffolders.
/// </summary>
public interface IScaffoldRunnerBuilder
{
    /// <summary>
    /// Gets the service provider created by the ScaffoldRunnerBuilder. This is useful for accessing services off the IScaffoldRunnerBuilder.
    /// returns null IF accessed before Build()
    /// </summary>
    IServiceProvider? ServiceProvider { get; }
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
    IReadOnlyDictionary<ScaffolderCatagory, List<ScaffoldBuilder>> Scaffolders { get; }

    /// <summary>
    /// Builds the <see cref="IScaffoldRunner"/> from the scaffolders and services configured in the builder.
    /// </summary>
    IScaffoldRunner Build();

    /// <summary>
    /// Adds a new <see cref="IScaffoldBuilder"/> to the builder with the specified name to the specified scaffolder category.
    /// </summary>
    /// /// <param name="category">The category of the scaffolder, either AspNet or Aspire.</param>
    /// <param name="name">The name of the scaffolder. This will be used as the command line command to execute that scaffolder.</param>
    IScaffoldBuilder AddScaffolder(ScaffolderCatagory category, string name);

    /// <summary>
    /// Adds a new <see cref="ScaffolderOption"/> to the builder.
    /// </summary>
    /// <param name="option">The option to add to the larger "dotnet-scaffold" tool.</param>
    void AddOption(ScaffolderOption option);

    /// <summary>
    /// Adds a handler to the RootCommand doing the action passed in the handle parameter.
    /// </summary>
    void AddHandler(Func<InvocationContext, Task> handle);
}
