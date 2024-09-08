// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.TextTemplating;
using Microsoft.DotNet.Scaffolding.TextTemplating.DbContext;

namespace Microsoft.DotNet.Scaffolding.Core.Builder;

internal static class ScaffolderBuilderExtensions
{
    /// <summary>
    /// Adds a new DbContext class 
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
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

    private static TextTemplatingProperty? GetDbContextTemplatingStepProperty(DbContextProperties? dbContextProperties)
    {
        //get .tt template file path
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
