// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.TextTemplating;
using Microsoft.DotNet.Scaffolding.TextTemplating.DbContext;

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
            if (context.Properties.TryGetValue("DbContextProperties", out var dbContextPropertiesObj) &&
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

            context.Properties.TryGetValue("BaseProjectPath", out var baseProjectPathObj);
            context.Properties.TryGetValue("CodeModifierProperties", out var codeModifierPropertiesObj); 
            var baseProjectPathVal = baseProjectPathObj?.ToString();
            var codeModifierProperties = codeModifierPropertiesObj as IDictionary<string, string>;
            string? connectionStringName = null;
            codeModifierProperties?.TryGetValue("$(ConnectionStringName)", out connectionStringName);

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
