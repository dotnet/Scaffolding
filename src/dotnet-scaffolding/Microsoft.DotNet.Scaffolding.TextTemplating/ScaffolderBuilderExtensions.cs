// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.TextTemplating;
using Microsoft.DotNet.Scaffolding.TextTemplating.DbContext;

namespace Microsoft.DotNet.Scaffolding.Core.Builder;

/// <summary>
/// Extension methods for IScaffoldBuilder to add T4-based DbContext scaffolding steps.
/// </summary>
internal static class ScaffolderBuilderExtensions
{
    /// <summary>
    /// Adds a new DbContext class generation step using T4 text templating.
    /// </summary>
    /// <param name="builder">The scaffold builder to extend.</param>
    /// <returns>The updated scaffold builder.</returns>
    public static IScaffoldBuilder WithDbContextStep(
        this IScaffoldBuilder builder)
    {
        builder = builder.WithStep<TextTemplatingStep>(config =>
        {
            var step = config.Step;
            var context = config.Context;

            DbContextProperties? dbContextProperties = null;
            if (context.Properties.TryGetValue(nameof(DbContextProperties), out var dbContextPropertiesObj) &&
                dbContextPropertiesObj is DbContextProperties)
            {
                dbContextProperties = dbContextPropertiesObj as DbContextProperties;
            }

            var dbContextTextTemplatingProperty = GetDbContextTemplatingStepProperty(dbContextProperties);
            if (dbContextTextTemplatingProperty is null)
            {
                config.Step.SkipStep = true;
                return;
            }

            step.TextTemplatingProperties = [dbContextTextTemplatingProperty];
            step.DisplayName = $"{dbContextProperties?.DbContextName ?? Constants.NewDbContext}{Constants.CSharpExtension}";
        });

        return builder;
    }

    /// <summary>
    /// Gets the T4 text templating property for the DbContext step.
    /// </summary>
    /// <param name="dbContextProperties">The DbContext properties to use as the model.</param>
    /// <returns>A configured TextTemplatingProperty or null if not available.</returns>
    private static TextTemplatingProperty? GetDbContextTemplatingStepProperty(DbContextProperties? dbContextProperties)
    {
        // Get .tt template file path
        var allT4Templates = new TemplateFoldersUtilities().GetAllT4Templates(["DbContext"]);
        string? t4TemplatePath = allT4Templates.FirstOrDefault(x => x.EndsWith("NewDbContext.tt", StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrEmpty(t4TemplatePath) || dbContextProperties is null)
        {
            return null;
        }

        return new TextTemplatingProperty
        {
            TemplateType = typeof(NewDbContext),
            TemplatePath = t4TemplatePath,
            TemplateModelName = "Model",
            OutputPath = dbContextProperties.DbContextPath,
            TemplateModel = dbContextProperties
        };
    }
}
