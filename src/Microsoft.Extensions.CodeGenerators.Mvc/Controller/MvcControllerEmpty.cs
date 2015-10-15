// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Dnx.Runtime;
using Microsoft.Extensions.CodeGeneration;
using Microsoft.Extensions.CodeGenerators.Mvc.Dependency;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.CodeGenerators.Mvc.Controller
{
    public class MvcControllerEmpty : ControllerGeneratorBase
    {
        public MvcControllerEmpty(
            [NotNull]ILibraryManager libraryManager,
            [NotNull]IApplicationEnvironment environment,
            [NotNull]ICodeGeneratorActionsService codeGeneratorActionsService,
            [NotNull]IServiceProvider serviceProvider,
            [NotNull]ILogger logger)
            : base(libraryManager, environment, codeGeneratorActionsService, serviceProvider, logger)
        {
        }

        public override async Task Generate(CommandLineGeneratorModel controllerGeneratorModel)
        {
            if (!string.IsNullOrEmpty(controllerGeneratorModel.ControllerName))
            {
                if (!controllerGeneratorModel.ControllerName.EndsWith(Constants.ControllerSuffix, StringComparison.Ordinal))
                {
                    controllerGeneratorModel.ControllerName = controllerGeneratorModel.ControllerName + Constants.ControllerSuffix;
                }
            }
            else
            {
                throw new ArgumentException("Controller name is required for an Empty Controller");
            }

            var layoutDependencyInstaller = ActivatorUtilities.CreateInstance<MvcLayoutDependencyInstaller>(ServiceProvider);
            await layoutDependencyInstaller.Execute();

            var templateModel = new ClassNameModel(className: controllerGeneratorModel.ControllerName, namespaceName: GetControllerNamespace());

            var templateName = "EmptyController.cshtml";
            var outputPath = ValidateAndGetOutputPath(controllerGeneratorModel);
            await CodeGeneratorActionsService.AddFileFromTemplateAsync(outputPath, templateName, TemplateFolders, templateModel);
            Logger.LogMessage("Added Controller : " + outputPath.Substring(ApplicationEnvironment.ApplicationBasePath.Length));

            await layoutDependencyInstaller.InstallDependencies();
        }
    }
}
