// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Dnx.Runtime;
using Microsoft.Extensions.CodeGeneration;
using Microsoft.Extensions.CodeGeneration.EntityFramework;
using Microsoft.Extensions.CodeGenerators.Mvc.Dependency;
using Microsoft.Extensions.CodeGenerators.Mvc.View;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.CodeGenerators.Mvc.Controller
{
    public class ControllerWithContextGenerator : ControllerGeneratorBase
    {
        private readonly List<string> _views = new List<string>()
        {
            "Create",
            "Edit",
            "Details",
            "Delete",
            "List"
        };

        public ControllerWithContextGenerator(
            [NotNull]ILibraryManager libraryManager,
            [NotNull]IApplicationEnvironment environment,
            [NotNull]IModelTypesLocator modelTypesLocator,
            [NotNull]IEntityFrameworkService entityFrameworkService,
            [NotNull]ICodeGeneratorActionsService codeGeneratorActionsService,
            [NotNull]IServiceProvider serviceProvider,
            [NotNull]ILogger logger)
            : base(libraryManager, environment, codeGeneratorActionsService, serviceProvider, logger)
        {
            ModelTypesLocator = modelTypesLocator;
            EntityFrameworkService = entityFrameworkService;
        }

        public override async Task Generate(CommandLineGeneratorModel controllerGeneratorModel)
        {
            Contract.Assert(!String.IsNullOrEmpty(controllerGeneratorModel.ModelClass));

            string outputPath = ValidateAndGetOutputPath(controllerGeneratorModel);
            var modelTypeAndContextModel = await ValidateModelAndGetMetadata(controllerGeneratorModel);

            if (string.IsNullOrEmpty(controllerGeneratorModel.ControllerName))
            {
                //Todo: Pluralize model name
                controllerGeneratorModel.ControllerName = modelTypeAndContextModel.ModelType.Name + Constants.ControllerSuffix;
            }

            var templateModel = new ControllerWithContextTemplateModel(modelTypeAndContextModel.ModelType, modelTypeAndContextModel.DbContextFullName)
            {
                ControllerName = controllerGeneratorModel.ControllerName,
                AreaName = string.Empty, //ToDo
                UseAsync = controllerGeneratorModel.UseAsync,
                ControllerNamespace = GetControllerNamespace(),
                ModelMetadata = modelTypeAndContextModel.ModelMetadata
            };

            await CodeGeneratorActionsService.AddFileFromTemplateAsync(outputPath, GetTemplateName(controllerGeneratorModel), TemplateFolders, templateModel);
            Logger.LogMessage("Added Controller : " + outputPath.Substring(ApplicationEnvironment.ApplicationBasePath.Length));

            await GenerateViewsIfRequired(controllerGeneratorModel, modelTypeAndContextModel, templateModel.ControllerRootName);
        }

        private async Task GenerateViewsIfRequired(CommandLineGeneratorModel controllerGeneratorModel,
            ModelTypeAndContextModel modelTypeAndContextModel,
            string controllerRootName)
        {
            if (!controllerGeneratorModel.IsRestController && !controllerGeneratorModel.NoViews)
            {
                var layoutDependencyInstaller = ActivatorUtilities.CreateInstance<MvcLayoutDependencyInstaller>(ServiceProvider);
                await layoutDependencyInstaller.Execute();

                foreach (var viewTemplate in _views)
                {
                    var viewName = viewTemplate == "List" ? "Index" : viewTemplate;
                    // ToDo: This is duplicated from ViewGenerator.
                    bool isLayoutSelected = controllerGeneratorModel.UseDefaultLayout ||
                        !String.IsNullOrEmpty(controllerGeneratorModel.LayoutPage);

                    var viewTemplateModel = new ViewGeneratorTemplateModel()
                    {
                        ViewDataTypeName = modelTypeAndContextModel.ModelType.FullName,
                        ViewDataTypeShortName = modelTypeAndContextModel.ModelType.Name,
                        ViewName = viewName,
                        LayoutPageFile = controllerGeneratorModel.LayoutPage,
                        IsLayoutPageSelected = isLayoutSelected,
                        IsPartialView = false,
                        ReferenceScriptLibraries = controllerGeneratorModel.ReferenceScriptLibraries,
                        ModelMetadata = modelTypeAndContextModel.ModelMetadata,
                        JQueryVersion = "1.10.2"
                    };

                    // Todo: Need logic for areas
                    var viewOutputPath = Path.Combine(
                        ApplicationEnvironment.ApplicationBasePath,
                        Constants.ViewsFolderName,
                        controllerRootName,
                        viewName + Constants.ViewExtension);

                    await CodeGeneratorActionsService.AddFileFromTemplateAsync(viewOutputPath,
                        viewTemplate + Constants.RazorTemplateExtension, TemplateFolders, viewTemplateModel);

                    Logger.LogMessage("Added View : " + viewOutputPath.Substring(ApplicationEnvironment.ApplicationBasePath.Length));
                }

                await layoutDependencyInstaller.InstallDependencies();
            }
        }

        private string GetTemplateName(CommandLineGeneratorModel generatorModel)
        {
            return generatorModel.IsRestController ? "ApiControllerWithContext.cshtml" : "MvcControllerWithContext.cshtml";
        }

        // Todo: This method is duplicated with the ViewGenerator.
        private async Task<ModelTypeAndContextModel> ValidateModelAndGetMetadata(CommonCommandLineModel commandLineModel)
        {
            ModelType model = ValidationUtil.ValidateType(commandLineModel.ModelClass, "model", ModelTypesLocator);
            ModelType dataContext = ValidationUtil.ValidateType(commandLineModel.DataContextClass, "dataContext", ModelTypesLocator, throwWhenNotFound: false);

            // Validation successful
            Contract.Assert(model != null, "Validation succeded but model type not set");

            var dbContextFullName = dataContext != null ? dataContext.FullName : commandLineModel.DataContextClass;

            var modelMetadata = await EntityFrameworkService.GetModelMetadata(
                dbContextFullName,
                model);

            return new ModelTypeAndContextModel()
            {
                ModelType = model,
                DbContextFullName = dbContextFullName,
                ModelMetadata = modelMetadata
            };
        }

        protected IModelTypesLocator ModelTypesLocator
        {
            get;
            private set;
        }
        protected IEntityFrameworkService EntityFrameworkService
        {
            get;
            private set;
        }
    }
}
