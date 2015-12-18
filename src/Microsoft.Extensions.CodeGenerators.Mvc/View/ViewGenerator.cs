// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.Extensions.CodeGeneration;
using Microsoft.Extensions.CodeGeneration.CommandLine;
using Microsoft.Extensions.CodeGeneration.EntityFramework;
using Microsoft.Extensions.CodeGenerators.Mvc.Dependency;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.CodeGenerators.Mvc.View
{
    /// <summary>
    /// Both the command line entry point for View generation (implements ICodeGenerator with Alias attribute)
    /// and View generator itself (extends CommonGeneratorBase).
    /// Consider separating if there are going to multiple view generators like Controllers.
    /// </summary>
    [Alias("view")]
    public class ViewGenerator : CommonGeneratorBase, ICodeGenerator
    {
        private readonly IModelTypesLocator _modelTypesLocator;
        private readonly IEntityFrameworkService _entityFrameworkService;
        private readonly ILibraryManager _libraryManager;
        private readonly ICodeGeneratorActionsService _codeGeneratorActionsService;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;

        // Todo: Instead of each generator taking services, provide them in some base class?
        // However for it to be effective, it should be property dependecy injection rather
        // than constructor injection.
        public ViewGenerator(
            ILibraryManager libraryManager,
            IApplicationEnvironment environment,
            IModelTypesLocator modelTypesLocator,
            IEntityFrameworkService entityFrameworkService,
            ICodeGeneratorActionsService codeGeneratorActionsService,
            IServiceProvider serviceProvider,
            ILogger logger)
            : base(environment)
        {
            if (libraryManager == null)
            {
                throw new ArgumentNullException(nameof(libraryManager));
            }

            if (environment == null)
            {
                throw new ArgumentNullException(nameof(environment));
            }

            if (modelTypesLocator == null)
            {
                throw new ArgumentNullException(nameof(modelTypesLocator));
            }

            if (entityFrameworkService == null)
            {
                throw new ArgumentNullException(nameof(entityFrameworkService));
            }

            if (codeGeneratorActionsService == null)
            {
                throw new ArgumentNullException(nameof(codeGeneratorActionsService));
            }

            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _libraryManager = libraryManager;
            _codeGeneratorActionsService = codeGeneratorActionsService;
            _modelTypesLocator = modelTypesLocator;
            _entityFrameworkService = entityFrameworkService;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public virtual IEnumerable<string> TemplateFolders
        {
            get
            {
                return TemplateFoldersUtilities.GetTemplateFolders(
                    containingProject: Constants.ThisAssemblyName,
                    applicationBasePath: ApplicationEnvironment.ApplicationBasePath,
                    baseFolders: new[] { "ViewGenerator" },
                    libraryManager: _libraryManager);
            }
        }

        public async Task GenerateCode(ViewGeneratorModel viewGeneratorModel)
        {
            if (viewGeneratorModel == null)
            {
                throw new ArgumentNullException(nameof(viewGeneratorModel));
            }

            if (string.IsNullOrEmpty(viewGeneratorModel.ViewName))
            {
                throw new ArgumentException(CodeGenerators.Mvc.MessageStrings.ViewNameRequired);
            }

            if (string.IsNullOrEmpty(viewGeneratorModel.TemplateName))
            {
                throw new ArgumentException(CodeGenerators.Mvc.MessageStrings.TemplateNameRequired);
            }

            var outputPath = ValidateAndGetOutputPath(viewGeneratorModel, outputFileName: viewGeneratorModel.ViewName + Constants.ViewExtension);
            var modelTypeAndContextModel = await ValidateModelAndGetMetadata(viewGeneratorModel);

            var layoutDependencyInstaller = ActivatorUtilities.CreateInstance<MvcLayoutDependencyInstaller>(_serviceProvider);
            await layoutDependencyInstaller.Execute();

            if (viewGeneratorModel.ViewName.EndsWith(Constants.ViewExtension, StringComparison.OrdinalIgnoreCase))
            {
                int viewNameLength = viewGeneratorModel.ViewName.Length - Constants.ViewExtension.Length;
                viewGeneratorModel.ViewName = viewGeneratorModel.ViewName.Substring(0, viewNameLength);
            }

            bool isLayoutSelected = !viewGeneratorModel.PartialView &&
                (viewGeneratorModel.UseDefaultLayout || !String.IsNullOrEmpty(viewGeneratorModel.LayoutPage));

            var templateModel = new ViewGeneratorTemplateModel()
            {
                ViewDataTypeName = modelTypeAndContextModel.ModelType.FullName,
                ViewDataTypeShortName = modelTypeAndContextModel.ModelType.Name,
                ViewName = viewGeneratorModel.ViewName,
                LayoutPageFile = viewGeneratorModel.LayoutPage,
                IsLayoutPageSelected = isLayoutSelected,
                IsPartialView = viewGeneratorModel.PartialView,
                ReferenceScriptLibraries = viewGeneratorModel.ReferenceScriptLibraries,
                ModelMetadata = modelTypeAndContextModel.ContextProcessingResult.ModelMetadata,
                JQueryVersion = "1.10.2" //Todo
            };

            var templateName = viewGeneratorModel.TemplateName + Constants.RazorTemplateExtension;
            await _codeGeneratorActionsService.AddFileFromTemplateAsync(outputPath, templateName, TemplateFolders, templateModel);
            _logger.LogMessage("Added View : " + outputPath.Substring(ApplicationEnvironment.ApplicationBasePath.Length));

            await layoutDependencyInstaller.InstallDependencies();

            if (modelTypeAndContextModel.ContextProcessingResult.ContextProcessingStatus == ContextProcessingStatus.ContextAddedButRequiresConfig)
            {
                throw new Exception(string.Format("{0} {1}", CodeGenerators.Mvc.MessageStrings.ScaffoldingSuccessful_unregistered, CodeGenerators.Mvc.MessageStrings.Scaffolding_additionalSteps));
            }
        }

        // Todo: This method is duplicated with the ControllerWithContext generator.
        private async Task<ModelTypeAndContextModel> ValidateModelAndGetMetadata(CommonCommandLineModel commandLineModel)
        {
            ModelType model = ValidationUtil.ValidateType(commandLineModel.ModelClass, "model", _modelTypesLocator);
            ModelType dataContext = ValidationUtil.ValidateType(commandLineModel.DataContextClass, "dataContext", _modelTypesLocator, throwWhenNotFound: false);

            // Validation successful
            Contract.Assert(model != null, CodeGenerators.Mvc.MessageStrings.ValidationSuccessfull_modelUnset);

            var dbContextFullName = dataContext != null ? dataContext.FullName : commandLineModel.DataContextClass;

            var modelMetadata = await _entityFrameworkService.GetModelMetadata(
                dbContextFullName,
                model);

            return new ModelTypeAndContextModel()
            {
                ModelType = model,
                DbContextFullName = dbContextFullName,
                ContextProcessingResult = modelMetadata
            };
        }
    }
}