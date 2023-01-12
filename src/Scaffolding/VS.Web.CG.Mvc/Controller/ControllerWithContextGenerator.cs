// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Shared;
using Microsoft.DotNet.Scaffolding.Shared.Project;
using Microsoft.DotNet.Scaffolding.Shared.ProjectModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Web.CodeGeneration;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.View;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Controller
{
    public class ControllerWithContextGenerator : ControllerGeneratorBase
    {
        private string _areaName = string.Empty;
        private readonly List<string> _views = new List<string>()
        {
            "Create",
            "Edit",
            "Details",
            "Delete",
            "List"
        };

        public ControllerWithContextGenerator(
            IProjectContext projectContext,
            IApplicationInfo applicationInfo,
            IModelTypesLocator modelTypesLocator,
            IEntityFrameworkService entityFrameworkService,
            ICodeGeneratorActionsService codeGeneratorActionsService,
            IServiceProvider serviceProvider,
            ILogger logger,
            IFileSystem fileSystem)
            : base(projectContext, applicationInfo, codeGeneratorActionsService, serviceProvider, logger)
        {

            if (modelTypesLocator == null)
            {
                throw new ArgumentNullException(nameof(modelTypesLocator));
            }

            if (entityFrameworkService == null)
            {
                throw new ArgumentNullException(nameof(entityFrameworkService));
            }
            FileSystem = fileSystem;
            ModelTypesLocator = modelTypesLocator;
            EntityFrameworkService = entityFrameworkService;
        }

        public override async Task Generate(CommandLineGeneratorModel controllerGeneratorModel)
        {
            Contract.Assert(!string.IsNullOrEmpty(controllerGeneratorModel.ModelClass));
            ValidateNameSpaceName(controllerGeneratorModel);
            controllerGeneratorModel.ValidateCommandline(Logger);
            if (CalledFromCommandline)
            {
                EFValidationUtil.ValidateEFDependencies(ProjectContext.PackageDependencies, controllerGeneratorModel.DatabaseProvider);
            }
            
            string outputPath = ValidateAndGetOutputPath(controllerGeneratorModel);
            _areaName = GetAreaName(ApplicationInfo.ApplicationBasePath, outputPath);

            var modelTypeAndContextModel = await ModelMetadataUtilities.ValidateModelAndGetEFMetadata(
                controllerGeneratorModel,
                EntityFrameworkService,
                ModelTypesLocator,
                Logger,
                _areaName);

            if (string.IsNullOrEmpty(controllerGeneratorModel.ControllerName))
            {
                //Todo: Pluralize model name
                controllerGeneratorModel.ControllerName = modelTypeAndContextModel.ModelType.Name + Constants.ControllerSuffix;
            }

            var namespaceName = string.IsNullOrEmpty(controllerGeneratorModel.ControllerNamespace)
                ? GetDefaultControllerNamespace(controllerGeneratorModel.RelativeFolderPath)
                : controllerGeneratorModel.ControllerNamespace;
            var templateModel = new ControllerWithContextTemplateModel(modelTypeAndContextModel.ModelType, modelTypeAndContextModel.DbContextFullName)
            {
                ControllerName = controllerGeneratorModel.ControllerName,
                AreaName = _areaName,
                UseAsync = controllerGeneratorModel.UseAsync, // This is no longer used for controllers with context.
                ControllerNamespace = namespaceName,
                ModelMetadata = modelTypeAndContextModel.ContextProcessingResult.ModelMetadata,
                NullableEnabled = "enable".Equals(ProjectContext.Nullable, StringComparison.OrdinalIgnoreCase)
            };

            await CodeGeneratorActionsService.AddFileFromTemplateAsync(outputPath, GetTemplateName(controllerGeneratorModel), TemplateFolders, templateModel);
            Logger.LogMessage(string.Format(MessageStrings.AddedController, outputPath.Substring(ApplicationInfo.ApplicationBasePath.Length)));

            await GenerateViewsIfRequired(controllerGeneratorModel, modelTypeAndContextModel, templateModel.ControllerRootName);

            if (modelTypeAndContextModel.ContextProcessingResult.ContextProcessingStatus == ContextProcessingStatus.ContextAddedButRequiresConfig)
            {
                throw new Exception(string.Format("{0} {1}" ,MessageStrings.ScaffoldingSuccessful_unregistered,
                    MessageStrings.Scaffolding_additionalSteps));
            }
        }

        private async Task GenerateViewsIfRequired(CommandLineGeneratorModel controllerGeneratorModel,
            ModelTypeAndContextModel modelTypeAndContextModel,
            string controllerRootName)
        {
            if (!controllerGeneratorModel.IsRestController && !controllerGeneratorModel.NoViews)
            {
                var viewGenerator = ActivatorUtilities.CreateInstance<EFModelBasedViewScaffolder>(ServiceProvider);

                var areaPath = string.IsNullOrEmpty(_areaName) ? string.Empty : Path.Combine("Areas", _areaName);
                var viewBaseOutputPath = Path.Combine(
                    ApplicationInfo.ApplicationBasePath,
                    areaPath,
                    Constants.ViewsFolderName,
                    controllerRootName);

                var viewGeneratorModel = new ViewGeneratorModel()
                {
                    UseDefaultLayout = controllerGeneratorModel.UseDefaultLayout,
                    PartialView = false,
                    LayoutPage = controllerGeneratorModel.LayoutPage,
                    Force = controllerGeneratorModel.Force,
                    RelativeFolderPath = viewBaseOutputPath,
                    ReferenceScriptLibraries = controllerGeneratorModel.ReferenceScriptLibraries,
                    BootstrapVersion = controllerGeneratorModel.BootstrapVersion
                };

                var viewAndTemplateNames = new Dictionary<string, string>();
                foreach (var viewTemplate in _views)
                {
                    var viewName = viewTemplate == "List" ? "Index" : viewTemplate;
                    viewAndTemplateNames.Add(viewName, viewTemplate);
                }
                await viewGenerator.GenerateViews(viewAndTemplateNames, viewGeneratorModel, modelTypeAndContextModel, viewBaseOutputPath);
            }
        }

        protected override string GetTemplateName(CommandLineGeneratorModel generatorModel)
        {
            return generatorModel.IsRestController ? Constants.ApiControllerWithContextTemplate : Constants.MvcControllerWithContextTemplate;
        }

        //IFileSystem is DefaultFileSystem in commandline scenarios and SimulationModeFileSystem in VS scenarios.
        private bool CalledFromCommandline => !(FileSystem is SimulationModeFileSystem);

        protected IFileSystem FileSystem { get; private set; }
        protected IModelTypesLocator ModelTypesLocator { get; private set; }
        protected IEntityFrameworkService EntityFrameworkService { get; private set; }
    }
}
