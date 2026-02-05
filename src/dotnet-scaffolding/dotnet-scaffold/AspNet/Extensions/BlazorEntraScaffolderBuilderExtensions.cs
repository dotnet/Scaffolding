// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.Helpers;
using Microsoft.DotNet.Scaffolding.Core.Model;
using Microsoft.DotNet.Scaffolding.Internal;
using Microsoft.DotNet.Scaffolding.TextTemplating;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Models;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;

namespace Microsoft.DotNet.Scaffolding.Core.Hosting;

/// <summary>
/// Provides extension methods for <see cref="IScaffoldBuilder"/> to add Blazor Entra ID scaffolding steps.
/// </summary>
internal static class BlazorEntraScaffolderBuilderExtensions
{
    /// <summary>
    /// Adds a step to register the application in Entra ID.
    /// </summary>
    /// <param name="builder">The scaffold builder.</param>
    /// <returns>The updated scaffold builder.</returns>
    /// <exception cref="InvalidOperationException">Thrown when required properties are missing.</exception>
    public static IScaffoldBuilder WithAddClientSecretStep(this IScaffoldBuilder builder)
    {
        return builder.WithStep<AddClientSecretStep>(config =>
        {
            var step = config.Step;
            var context = config.Context;
            context.Properties.TryGetValue(nameof(EntraIdModel), out var entraIdModel);
            EntraIdModel entra = entraIdModel as EntraIdModel ??
                throw new InvalidOperationException("missing 'EntraIdModel' in 'ScaffolderContext.Properties'");

            context.Properties.TryGetValue(nameof(EntraIdSettings), out var entraIdSettings);
            EntraIdSettings entraSettings = entraIdSettings as EntraIdSettings ??
                throw new InvalidOperationException("missing 'EntraIdSettings' in 'ScaffolderContext.Properties'");

            if (string.IsNullOrEmpty(entraSettings.Project))
            {
                throw new InvalidOperationException("Project path is not set in EntraIdSettings.");
            }

            step.ProjectPath = entraSettings.Project;

            if (string.IsNullOrEmpty(entraSettings.Username))
            {
                throw new InvalidOperationException("Username is not set in EntraIdSettings.");
            }
            step.Username = entra.Username;

            if (string.IsNullOrEmpty(entraSettings.TenantId))
            {
                throw new InvalidOperationException("TenantId is not set in EntraIdSettings.");
            }
            step.TenantId = entra.TenantId;

            if (context.Properties.TryGetValue("ClientId", out var clientIdObj) && clientIdObj is string clientId)
            {
                step.ClientId = clientId;
            }
        });
    }

    /// <summary>
    /// Adds a step to register the application in Entra ID.
    /// </summary>
    /// <param name="builder">The scaffold builder.</param>
    /// <returns>The updated scaffold builder.</returns>
    /// <exception cref="InvalidOperationException">Thrown when required properties are missing.</exception>
    public static IScaffoldBuilder WithRegisterAppStep(this IScaffoldBuilder builder)
    {
        return builder.WithStep<RegisterAppStep>(config =>
        {
            var step = config.Step;
            var context = config.Context;
            context.Properties.TryGetValue(nameof(EntraIdModel), out var entraIdModel);
            EntraIdModel entra = entraIdModel as EntraIdModel ??
                throw new InvalidOperationException("missing 'EntraIdModel' in 'ScaffolderContext.Properties'");
            context.Properties.TryGetValue(nameof(EntraIdSettings), out var entraIdSettings);
            EntraIdSettings entraSettings = entraIdSettings as EntraIdSettings ??
                throw new InvalidOperationException("missing 'EntraIdSettings' in 'ScaffolderContext.Properties'");

            bool.TryParse(entraSettings.Application, out var selectSelected);

            if (!string.IsNullOrEmpty(entraSettings.SelectApplication))
            {
                string id = entraSettings.SelectApplication.Split(" ").Last();
                step.ClientId = id;
                context.Properties["ClientId"] = id;
            }

            if (selectSelected)
            {
                step.SkipStep = true;
            }

            if (string.IsNullOrEmpty(entraSettings.Project))
            {
                throw new InvalidOperationException("Project path is not set in EntraIdSettings.");
            }
            step.ProjectPath = entraSettings.Project;
            if (string.IsNullOrEmpty(entraSettings.Username))
            {
                throw new InvalidOperationException("Username is not set in EntraIdSettings.");
            }
            step.Username = entra.Username;
            if (string.IsNullOrEmpty(entraSettings.TenantId))
            {
                throw new InvalidOperationException("TenantId is not set in EntraIdSettings.");
            }
            step.TenantId = entra.TenantId;

        });
    }

    /// <summary>
    /// Adds a step to update the application settings in Entra ID.
    /// </summary>
    /// <param name="builder">The scaffold builder.</param>
    /// <returns>The updated scaffold builder.</returns>
    /// <exception cref="InvalidOperationException">Thrown when required properties are missing.</exception>
    public static IScaffoldBuilder WithUpdateAppSettingsStep(this IScaffoldBuilder builder)
    {
        return builder.WithStep<UpdateAppSettingsStep>(config =>
        {
            var step = config.Step;
            var context = config.Context;
            context.Properties.TryGetValue(nameof(EntraIdModel), out var entraIdModel);
            EntraIdModel entra = entraIdModel as EntraIdModel ??
                throw new InvalidOperationException("missing 'EntraIdModel' in 'ScaffolderContext.Properties'");

            context.Properties.TryGetValue(nameof(EntraIdSettings), out var entraIdSettings);
            EntraIdSettings entraSettings = entraIdSettings as EntraIdSettings ??
                throw new InvalidOperationException("missing 'EntraIdSettings' in 'ScaffolderContext.Properties'");

            if (string.IsNullOrEmpty(entraSettings.Project))
            {
                throw new InvalidOperationException("Project path is not set in EntraIdSettings.");
            }

            if (string.IsNullOrEmpty(entraSettings.Username))
            {
                throw new InvalidOperationException("Username is not set in EntraIdSettings.");
            }
            step.Username = entraSettings.Username;

            step.ProjectPath = entraSettings.Project;

            if (string.IsNullOrEmpty(entraSettings.TenantId))
            {
                throw new InvalidOperationException("TenantId is not set in EntraIdSettings.");
            }
            step.TenantId = entra.TenantId;

            if (context.Properties.TryGetValue("ClientId", out var clientIdObj) && clientIdObj is string clientId)
            {
                step.ClientId = clientId;
            }

            if (context.Properties.TryGetValue("ClientSecret", out var clientSecretObj) && clientSecretObj is string clientSecret)
            {
                step.ClientSecret = clientSecret;
            }
        });
    }

    /// <summary>
    /// Adds a step to update the application authorization in Entra ID.
    /// </summary>
    /// <param name="builder">The scaffold builder.</param>
    /// <returns>The updated scaffold builder.</returns>
    /// <exception cref="InvalidOperationException">Thrown when required properties are missing.</exception>
    public static IScaffoldBuilder WithUpdateAppAuthorizationStep(this IScaffoldBuilder builder)
    {
        return builder.WithStep<UpdateAppAuthorizationStep>(config =>
        {
            var step = config.Step;
            var context = config.Context;
            context.Properties.TryGetValue(nameof(EntraIdModel), out var entraIdModel);
            EntraIdModel entra = entraIdModel as EntraIdModel ??
                throw new InvalidOperationException("missing 'EntraIdModel' in 'ScaffolderContext.Properties'");
            context.Properties.TryGetValue(nameof(EntraIdSettings), out var entraIdSettings);
            EntraIdSettings entraSettings = entraIdSettings as EntraIdSettings ??
                throw new InvalidOperationException("missing 'EntraIdSettings' in 'ScaffolderContext.Properties'");

            if (string.IsNullOrEmpty(entraSettings.Project))
            {
                throw new InvalidOperationException("Project path is not set in EntraIdSettings.");
            }

            step.ProjectPath = entraSettings.Project;

            if (context.Properties.TryGetValue("ClientId", out var clientIdObj) && clientIdObj is string clientId)
            {
                step.ClientId = clientId;
            }
            else
            {
                step.SkipStep = true;
            }
        });
    }

    /// <summary>
    /// Adds a step to detect Blazor WebAssembly project.
    /// </summary>
    /// <param name="builder">The scaffold builder.</param>
    /// <returns>The updated scaffold builder.</returns>
    /// <exception cref="InvalidOperationException">Thrown when required properties are missing.</exception>
    public static IScaffoldBuilder WithDetectBlazorWasmStep(this IScaffoldBuilder builder)
    {
        return builder.WithStep<DetectBlazorWasmStep>(config =>
        {
            var step = config.Step;
            var context = config.Context;
            context.Properties.TryGetValue(nameof(EntraIdSettings), out var entraIdSettings);
            EntraIdSettings entraSettings = entraIdSettings as EntraIdSettings ??
                throw new InvalidOperationException("missing 'EntraIdSettings' in 'ScaffolderContext.Properties'");
            if (string.IsNullOrEmpty(entraSettings.Project))
            {
                throw new InvalidOperationException("Project path is not set in EntraIdSettings.");
            }
            step.ProjectPath = entraSettings.Project;
        });
    }

    /// <summary>
    /// Adds a step to install necessary packages for Entra ID.
    /// </summary>
    /// <param name="builder">The scaffold builder.</param>
    /// <returns>The updated scaffold builder.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the project path is not set.</exception>
    public static IScaffoldBuilder WithEntraAddPackagesStep(this IScaffoldBuilder builder)
    {
        return builder.WithStep<WrappedAddPackagesStep>(config =>
        {
            var step = config.Step;
            var context = config.Context;
            List<Package> packages = [
                PackageConstants.AspNetCorePackages.MicrosoftIdentityWebPackage
            ];

            if (context.Properties.TryGetValue(nameof(EntraIdSettings), out var entraIdSettings) &&
                entraIdSettings is EntraIdSettings entraSettings)
            {
                if (string.IsNullOrEmpty(entraSettings.Project))
                {
                    throw new InvalidOperationException("Project path is not set in EntraIdSettings.");
                }

                step.ProjectPath = entraSettings.Project;
                step.Packages = packages;
            }
            else
            {
                step.SkipStep = true;
                return;
            }
        });
    }

    /// <summary>
    /// Adds a step to install necessary packages for Blazor WebAssembly Entra ID.
    /// </summary>
    /// <param name="builder">The scaffold builder.</param>
    /// <returns>The updated scaffold builder.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the project path is not set.</exception>
    public static IScaffoldBuilder WithEntraBlazorWasmAddPackagesStep(this IScaffoldBuilder builder)
    {
        return builder.WithStep<WrappedAddPackagesStep>(config =>
        {
            var step = config.Step;
            var context = config.Context;

            if (context.Properties.TryGetValue("IsBlazorWasmProject", out var isBlazorWasm) && isBlazorWasm is bool wasmProject && wasmProject)
            {
                if (context.Properties.TryGetValue("BlazorWasmClientProjectPath", out var clientProjectPath) && clientProjectPath is string projectPath && !string.IsNullOrEmpty(projectPath))
                {
                    step.ProjectPath = projectPath;

                    List<Package> packages = new List<Package>
                    {
                        PackageConstants.AspNetCorePackages.AspNetCoreComponentsWebAssemblyAuthenticationPackage
                    };
                    if (context.Properties.TryGetValue(nameof(EntraIdSettings), out var entraIdSettings) &&
                        entraIdSettings is EntraIdSettings entraSettings)
                    {
                        if (string.IsNullOrEmpty(entraSettings.Project))
                        {
                            throw new InvalidOperationException("Project path is not set in EntraIdSettings.");
                        }

                        step.Packages = packages;
                    }
                }
                else
                {
                    step.SkipStep = true;
                    return;
                }
            }
            else
            {
                step.SkipStep = true;
                return;
            }
        });
    }

    /// <summary>
    /// Adds a step to apply code changes for Entra ID.
    /// </summary>
    /// <param name="builder">The scaffold builder.</param>
    /// <returns>The updated scaffold builder.</returns>
    /// <exception cref="InvalidOperationException">Thrown when required properties are missing.</exception>
    public static IScaffoldBuilder WithEntraIdCodeChangeStep(this IScaffoldBuilder builder)
    {
        builder = builder.WithStep<WrappedCodeModificationStep>(config =>
        {
            var step = config.Step;
            var context = config.Context;
            //get needed properties and cast them as needed
            context.Properties.TryGetValue(nameof(EntraIdSettings), out var entraIdSettings);
            EntraIdSettings entraSettings = entraIdSettings as EntraIdSettings ??
                throw new InvalidOperationException("missing 'EntraIdSettings' in 'ScaffolderContext.Properties'");
            string? targetFrameworkFolder = "net11.0"; //TODO invoke TargetFrameworkHelpers.GetTargetFrameworkFolder(entraSettings?.Project); when other tfm supported
            var codeModificationFilePath = GlobalToolFileFinder.FindCodeModificationConfigFile("blazorEntraChanges.json", System.Reflection.Assembly.GetExecutingAssembly(), targetFrameworkFolder);
            context.Properties.TryGetValue(nameof(EntraIdModel), out var entraIdModel);
            EntraIdModel entraModel = entraIdModel as EntraIdModel ??
                throw new InvalidOperationException("missing 'EntraIdModel' in 'ScaffolderContext.Properties'");
            config.Context.Properties.TryGetValue(Internal.Constants.StepConstants.CodeModifierProperties, out var codeModifierPropertiesObj);
            var codeModifierProperties = codeModifierPropertiesObj as Dictionary<string, string>;


            //initialize CodeModificationStep's properties
            if (!string.IsNullOrEmpty(codeModificationFilePath) &&
                entraSettings is not null &&
                codeModifierProperties is not null &&
                entraModel is not null)
            {
                step.CodeModifierConfigPath = codeModificationFilePath;
                foreach (var kvp in codeModifierProperties)
                {
                    step.CodeModifierProperties.TryAdd(kvp.Key, kvp.Value);
                }

                step.ProjectPath = entraSettings.Project!;
                step.CodeChangeOptions = entraModel.ProjectInfo?.CodeChangeOptions ?? [];
            }
            else
            {
                step.SkipStep = true;
                return;
            }
        });

        return builder;
    }

    /// <summary>
    /// Adds a step to apply code changes for Blazor WebAssembly Entra ID.
    /// </summary>
    /// <param name="builder">The scaffold builder.</param>
    /// <returns>The updated scaffold builder.</returns>
    /// <exception cref="InvalidOperationException">Thrown when required properties are missing.</exception>
    public static IScaffoldBuilder WithEntraIdBlazorWasmCodeChangeStep(this IScaffoldBuilder builder)
    {
        builder = builder.WithStep<WrappedCodeModificationStep>(config =>
        {
            var step = config.Step;
            var context = config.Context;
            //get needed properties and cast them as needed
            if (context.Properties.TryGetValue("BlazorWasmClientProjectPath", out var blazorWasmProject) && blazorWasmProject is string clientProjectPath && !string.IsNullOrEmpty(clientProjectPath))
            {
                string targetFrameworkFolder = "net11.0"; //TODO invoke TargetFrameworkHelpers.GetTargetFrameworkFolder(clientProjectPath); when other tfm supported    
                string? codeModificationFilePath = GlobalToolFileFinder.FindCodeModificationConfigFile("blazorWasmEntraChanges.json", System.Reflection.Assembly.GetExecutingAssembly(), targetFrameworkFolder);
                step.ProjectPath = clientProjectPath;
                context.Properties.TryGetValue(nameof(EntraIdModel), out var entraIdModel);
                EntraIdModel entraModel = entraIdModel as EntraIdModel ??
                    throw new InvalidOperationException("missing 'EntraIdModel' in 'ScaffolderContext.Properties'");
                context.Properties.TryGetValue(nameof(EntraIdSettings), out var entraIdSettings);
                EntraIdSettings entraSettings = entraIdSettings as EntraIdSettings ??
                    throw new InvalidOperationException("missing 'EntraIdSettings' in 'ScaffolderContext.Properties'");
                config.Context.Properties.TryGetValue(Internal.Constants.StepConstants.CodeModifierProperties, out var codeModifierPropertiesObj);
                var codeModifierProperties = codeModifierPropertiesObj as Dictionary<string, string>;
                //initialize CodeModificationStep's properties
                if (!string.IsNullOrEmpty(codeModificationFilePath) &&
                    entraSettings is not null &&
                    codeModifierProperties is not null &&
                    entraModel is not null)
                {
                    step.CodeModifierConfigPath = codeModificationFilePath;
                    foreach (var kvp in codeModifierProperties)
                    {
                        step.CodeModifierProperties.TryAdd(kvp.Key, kvp.Value);
                    }

                    step.CodeChangeOptions = entraModel.ProjectInfo?.CodeChangeOptions ?? [];
                }
                else
                {
                    step.SkipStep = true;
                    return;
                }
            }
            else
            {
                step.SkipStep = true;
                return;
            }
        });

        return builder;
    }

    /// <summary>
    /// Adds a step for Entra ID text templating.
    /// </summary>
    /// <param name="builder">The scaffold builder.</param>
    /// <returns>The updated scaffold builder.</returns>
    /// <exception cref="InvalidOperationException">Thrown when required properties are missing.</exception>
    public static IScaffoldBuilder WithEntraIdTextTemplatingStep(this IScaffoldBuilder builder)
    {
        builder = builder.WithStep<WrappedTextTemplatingStep>(config =>
        {
            var step = config.Step;
            var context = config.Context;
            context.Properties.TryGetValue(nameof(EntraIdModel), out var entraIdModelObj);
            EntraIdModel entraIdModel = entraIdModelObj as EntraIdModel ??
                throw new InvalidOperationException("missing 'EntraIdModel' in 'ScaffolderContext.Properties'");
            var templateFolderUtilities = new TemplateFoldersUtilities();

            if (entraIdModel.ProjectInfo is null || string.IsNullOrEmpty(entraIdModel.ProjectInfo.ProjectPath))
            {
                step.SkipStep = true;
                return;
            }
            
            var allBlazorIdentityFiles = templateFolderUtilities.GetAllT4TemplatesForTargetFramework(["BlazorEntraId"], entraIdModel.ProjectInfo.ProjectPath);
            var blazorEntraIdProperties = EntraIdHelper.GetTextTemplatingProperties(allBlazorIdentityFiles, entraIdModel);

            if (blazorEntraIdProperties is not null && blazorEntraIdProperties.Any())
            {
                step.TextTemplatingProperties = blazorEntraIdProperties;
                step.DisplayName = "Blazor Entra ID files";
                step.Overwrite = true;
            }
            else
            {
                step.SkipStep = true;
                return;
            }
        });

        return builder;
    }
}



