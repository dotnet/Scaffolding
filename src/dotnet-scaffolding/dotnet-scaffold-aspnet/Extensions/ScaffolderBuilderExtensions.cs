// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.TextTemplating;
using Microsoft.DotNet.Scaffolding.TextTemplating.DbContext;
using Constants = Microsoft.DotNet.Scaffolding.Internal.Constants;

namespace Microsoft.DotNet.Scaffolding.Core.Hosting;

internal static class ScaffolderBuilderExtensions
{
    public static IScaffoldBuilder WithConnectionStringStep(
        this IScaffoldBuilder builder)
    {
        builder = builder.WithStep<AddConnectionStringStep>(config =>
        {
            var step = config.Step;
            var context = config.Context;

            DbContextProperties? dbContextProperties = null;
            if (context.Properties.TryGetValue(Constants.StepConstants.DbContextProperties, out var dbContextPropertiesObj) &&
                dbContextPropertiesObj is DbContextProperties)
            {
                dbContextProperties = dbContextPropertiesObj as DbContextProperties;
            }

            if (dbContextProperties is null ||
                string.IsNullOrEmpty(dbContextProperties.DbContextPath) ||
                string.IsNullOrEmpty(dbContextProperties.NewDbConnectionString))
            {
                config.Step.SkipStep = true;
                return;
            }

            string? connectionStringName = null;
            context.Properties.TryGetValue(Constants.StepConstants.BaseProjectPath, out var baseProjectPathObj);
            context.Properties.TryGetValue(Constants.StepConstants.CodeModifierProperties, out var codeModifierPropertiesObj); 
            var baseProjectPathVal = baseProjectPathObj?.ToString();
            var codeModifierProperties = codeModifierPropertiesObj as IDictionary<string, string>;
            codeModifierProperties?.TryGetValue(Constants.CodeModifierPropertyConstants.ConnectionStringName, out connectionStringName);
            if (string.IsNullOrEmpty(baseProjectPathVal))
            {
                throw new ArgumentException("'BaseProjectPath' not provided in 'WithAddDbContextStep' or provided in ScaffolderContext.Properties");
            }

            step.BaseProjectPath = baseProjectPathVal;
            step.ConnectionString = string.Format(dbContextProperties.NewDbConnectionString, connectionStringName ?? dbContextProperties.DbContextName);
            step.ConnectionStringName = dbContextProperties.DbContextName;
        });

        return builder;
    }

    /// <summary>
    /// Adds a new IdentityDbContext class 
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static IScaffoldBuilder WithIdentityDbContextStep(
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
            step.DisplayName = $"{dbContextProperties?.DbContextName ?? Tools.Scaffold.AspNet.Common.Constants.Identity.DbContextName}{Tools.Scaffold.AspNet.Common.Constants.CSharpExtension}";
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
