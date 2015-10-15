// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Dnx.Runtime;
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
            [NotNull]ILibraryManager libraryManager,
            [NotNull]IApplicationEnvironment environment,
            [NotNull]IModelTypesLocator modelTypesLocator,
            [NotNull]IEntityFrameworkService entityFrameworkService,
            [NotNull]ICodeGeneratorActionsService codeGeneratorActionsService,
            [NotNull]IServiceProvider serviceProvider,
            [NotNull]ILogger logger)
            : base(environment)
        {
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

        public async Task GenerateCode([NotNull]ViewGeneratorModel viewGeneratorModel)
        {
            if (string.IsNullOrEmpty(viewGeneratorModel.ViewName))
            {
                throw new ArgumentException("The ViewName cannot be empty");
            }

            if (string.IsNullOrEmpty(viewGeneratorModel.TemplateName))
            {
                throw new ArgumentException("The TemplateName cannot be empty");
            }

            var outputPath = ValidateAndGetOutputPath(viewGeneratorModel, outputFileName: viewGeneratorModel.ViewName + Constants.ViewExtension);

            ModelType model = ValidationUtil.ValidateType(viewGeneratorModel.ModelClass, "model", _modelTypesLocator);
            ModelType dataContext = ValidationUtil.ValidateType(viewGeneratorModel.DataContextClass, "dataContext", _modelTypesLocator, throwWhenNotFound: false);

            // Validation successful
            Contract.Assert(model != null, "Validation succeded but model type not set");

            var dbContextFullName = dataContext != null ? dataContext.FullName : viewGeneratorModel.DataContextClass;
            var modelTypeFullName = model.FullName;

            var modelMetadata = await _entityFrameworkService.GetModelMetadata(
                dbContextFullName,
                model);

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
                ViewDataTypeName = modelTypeFullName,
                ViewDataTypeShortName = model.Name,
                ViewName = viewGeneratorModel.ViewName,
                LayoutPageFile = viewGeneratorModel.LayoutPage,
                IsLayoutPageSelected = isLayoutSelected,
                IsPartialView = viewGeneratorModel.PartialView,
                ReferenceScriptLibraries = viewGeneratorModel.ReferenceScriptLibraries,
                ModelMetadata = modelMetadata,
                JQueryVersion = "1.10.2" //Todo
            };

            var templateName = viewGeneratorModel.TemplateName + Constants.RazorTemplateExtension;
            await _codeGeneratorActionsService.AddFileFromTemplateAsync(outputPath, templateName, TemplateFolders, templateModel);
            _logger.LogMessage("Added View : " + outputPath.Substring(ApplicationEnvironment.ApplicationBasePath.Length));

            await layoutDependencyInstaller.InstallDependencies();
        }
    }
}