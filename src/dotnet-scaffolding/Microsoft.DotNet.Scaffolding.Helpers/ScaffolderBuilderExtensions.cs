// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Helpers.General;
using Microsoft.DotNet.Scaffolding.Helpers.Steps;
using Microsoft.DotNet.Scaffolding.Helpers.Templates.DbContext;
using Microsoft.DotNet.Scaffolding.TextTemplating;

namespace Microsoft.DotNet.Scaffolding.Core.Builder;

internal static class ScaffolderBuilderExtensions
{
    /// <summary>
    /// Adds a new DbContext class 
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static IScaffoldBuilder WithAddDbContextStep(
        this IScaffoldBuilder builder,
        bool addConnectionString = true,
        DbContextProperties? dbContextProperties = null,
        string? baseProjectPath = null)
    {
        builder = builder.WithStep<TextTemplatingStep>(config =>
        {
            var step = config.Step;
            var context = config.Context;
            if (dbContextProperties is null &&
                context.Properties.TryGetValue("DbContextProperties", out var dbContextPropertiesObj) &&
                dbContextPropertiesObj is DbContextProperties)
            {
                dbContextProperties = dbContextPropertiesObj as DbContextProperties;
            }

            if (dbContextProperties is null ||
                string.IsNullOrEmpty(dbContextProperties.DbContextPath))
            {
                throw new ArgumentException("'DbContextProperties' not provided in 'WithAddDbContextStep' or provided in ScaffolderContext.Properties");
            }

            GetDbContextTemplatingStepProperties(out var templatePath, out var templateType, out var templateModelName);
            step.TemplatePath = templatePath;
            step.TemplateType = templateType;
            step.TemplateModelName = templateModelName;
            step.TemplateModel = dbContextProperties;
            step.OutputPath = dbContextProperties.DbContextPath;
        });

        if (addConnectionString &&
            !string.IsNullOrEmpty(dbContextProperties?.NewDbConnectionString))
        {
            builder = builder.WithStep<AddConnectionStringStep>(config =>
            {
                var step = config.Step;
                var context = config.Context;
                if (!string.IsNullOrEmpty(baseProjectPath))
                {
                    step.BaseProjectPath = baseProjectPath;
                }
                else 
                {
                    context.Properties.TryGetValue("BaseProjectPath", out var baseProjectPathObj);
                    var baseProjectPathVal = baseProjectPathObj?.ToString();
                    if (string.IsNullOrEmpty(baseProjectPathVal))
                    {
                        throw new ArgumentException("'BaseProjectPath' not provided in 'WithAddDbContextStep' or provided in ScaffolderContext.Properties");
                    }

                    step.BaseProjectPath = baseProjectPathVal;
                }

                step.ConnectionString = dbContextProperties.NewDbConnectionString;
                step.ConnectionStringName = dbContextProperties.DbContextName;
                
            });
        }

        return builder;
    }

    private static void GetDbContextTemplatingStepProperties(
        out string templatePath,
        out Type templateType,
        out string templateModelName)
    {
        //get .tt template file path
        var templateUtilities = new TemplateFoldersUtilities();
        var allT4Templates = templateUtilities.GetAllT4Templates(["DbContext"]);
        string? t4TemplatePath = allT4Templates.FirstOrDefault(x => x.EndsWith("NewDbContext.tt", StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrEmpty(t4TemplatePath))
        {
            throw new Exception();
        }

        //get System.Type for NewDbContext
        templateType = typeof(NewDbContext);
        templatePath = t4TemplatePath;
        templateModelName = "Model";
    }
}
