// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Tools.Scaffold.Aspire.ScaffoldSteps;
using Microsoft.DotNet.Scaffolding.TextTemplating.DbContext;

namespace Microsoft.DotNet.Scaffolding.Core.Hosting;

/// <summary>
/// Extension methods for <see cref="IScaffoldBuilder"/> to add common scaffolding steps.
/// </summary>
internal static class ScaffolderBuilderAspireExtensions
{
    /// <summary>
    /// Adds a step to the <see cref="IScaffoldBuilder"/> for adding a connection string to the project.
    /// </summary>
    /// <param name="builder">The scaffold builder to extend.</param>
    /// <returns>The scaffold builder with the connection string step added.</returns>
    public static IScaffoldBuilder WithAspireConnectionStringStep(
        this IScaffoldBuilder builder)
    {
        // Add a step to insert a connection string into the project configuration
        builder = builder.WithStep<AddAspireConnectionStringStep>(config =>
        {
            var step = config.Step;
            var context = config.Context;

            DbContextProperties? dbContextProperties = null;
            // Try to retrieve DbContextProperties from the context
            if (context.Properties.TryGetValue("DbContextProperties", out var dbContextPropertiesObj) &&
                dbContextPropertiesObj is DbContextProperties)
            {
                dbContextProperties = dbContextPropertiesObj as DbContextProperties;
            }

            // Skip step if required DbContextProperties are missing
            if (dbContextProperties is null ||
                string.IsNullOrEmpty(dbContextProperties.DbContextPath) ||
                string.IsNullOrEmpty(dbContextProperties.NewDbConnectionString))
            {
                config.Step.SkipStep = true;
                return;
            }

            // Retrieve the base project path from context
            context.Properties.TryGetValue("BaseProjectPath", out var baseProjectPathObj);
            var baseProjectPathVal = baseProjectPathObj?.ToString();
            if (string.IsNullOrEmpty(baseProjectPathVal))
            {
                throw new ArgumentException("'BaseProjectPath' not provided in 'WithAddDbContextStep' or provided in ScaffolderContext.Properties");
            }

            // Set properties for the AddAspireConnectionStringStep
            step.BaseProjectPath = baseProjectPathVal;
            step.ConnectionString = string.Format(dbContextProperties.NewDbConnectionString, dbContextProperties.DbContextName);
            step.ConnectionStringName = dbContextProperties.DbContextName;
        });

        return builder;
    }
}
