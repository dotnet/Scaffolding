// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.Framework.CodeGeneration;
using Microsoft.Framework.CodeGeneration.CommandLine;
using Microsoft.Framework.CodeGeneration.EntityFramework;
using Microsoft.Framework.Runtime;

namespace Microsoft.Framework.CodeGenerators.Mvc
{
    [Alias("controller")]
    public class ControllerCodeGenerator : ICodeGenerator
    {
        private readonly IModelTypesLocator _modelTypesLocator;
        private readonly IEntityFrameworkService _entityFrameworkService;
        private readonly IApplicationEnvironment _applicationEnvironment;
        private readonly ICodeGeneratorActionsService _codeGeneratorActionsService;
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger _logger;

        private readonly List<string> _views = new List<string>()
        {
            "Create",
            "Edit",
            "Details",
            "Delete",
            "List"
        };

        public ControllerCodeGenerator(
            [NotNull]ILibraryManager libraryManager,
            [NotNull]IApplicationEnvironment environment,
            [NotNull]IModelTypesLocator modelTypesLocator,
            [NotNull]IEntityFrameworkService entityFrameworkService,
            [NotNull]ICodeGeneratorActionsService codeGeneratorActionsService,
            [NotNull]ILogger logger)
        {
            _libraryManager = libraryManager;
            _applicationEnvironment = environment;
            _codeGeneratorActionsService = codeGeneratorActionsService;
            _modelTypesLocator = modelTypesLocator;
            _entityFrameworkService = entityFrameworkService;
            _logger = logger;
        }

        public virtual IEnumerable<string> TemplateFolders
        {
            get
            {
                return TemplateFoldersUtilities.GetTemplateFolders(
                    containingProject: Constants.ThisAssemblyName,
                    baseFolders: new[] { typeof(ControllerCodeGenerator).Name, typeof(ViewGenerator).Name },
                    applicationBasePath: _applicationEnvironment.ApplicationBasePath,
                    libraryManager: _libraryManager);
            }
        }

        public async Task GenerateCode([NotNull]ControllerGeneratorModel controllerGeneratorModel)
        {
            // Validate model
            string validationMessage;
            ITypeSymbol model, dataContext;

            // Review: MVC scaffolding used ActiveProject's MSBuild RootNamespace property
            // That's not possible in command line scaffolding - the closest we can get is
            // the name of assembly??
            var appName = _libraryManager.GetLibraryInformation(_applicationEnvironment.ApplicationName).Name;
            var controllerNameSpace = appName + "." + Constants.ControllersFolderName;

            if (!string.IsNullOrEmpty(controllerGeneratorModel.ModelClass) && !string.IsNullOrEmpty(controllerGeneratorModel.DataContextClass))
            {
               if (!ValidationUtil.TryValidateType(controllerGeneratorModel.ModelClass, "model", _modelTypesLocator, out model, out validationMessage))
                {
                    throw new ArgumentException(validationMessage);
                }

                ValidationUtil.TryValidateType(controllerGeneratorModel.DataContextClass, "dataContext", _modelTypesLocator, out dataContext, out validationMessage);
                await GenerateController(controllerGeneratorModel, model, dataContext, controllerNameSpace);
            }
            else
            {
                await GenerateEmptyController(controllerGeneratorModel, controllerNameSpace);
            }
        }

        private async Task GenerateController(ControllerGeneratorModel controllerGeneratorModel,
            ITypeSymbol model,
            ITypeSymbol dataContext,
            string controllerNameSpace)
        {
            var controllerName = controllerGeneratorModel.ControllerName;
            if (string.IsNullOrEmpty(controllerName))
            {
                //Todo: Pluralize model name
                controllerName = model.Name + Constants.ControllerSuffix;
            }

            // Validation successful
            Contract.Assert(model != null, "Validation succeded but model type not set");

            string outputPath = ValidateAndGetOutputPath(controllerGeneratorModel);

            var dbContextFullName = dataContext != null ? dataContext.FullNameForSymbol() : controllerGeneratorModel.DataContextClass;
            var modelTypeFullName = model.FullNameForSymbol();

            var modelMetadata = await _entityFrameworkService.GetModelMetadata(
                dbContextFullName,
                model);

            var templateName = "ControllerWithContext.cshtml";
            var templateModel = new ControllerGeneratorTemplateModel(model, dbContextFullName)
            {
                ControllerName = controllerName,
                AreaName = string.Empty, //ToDo
                UseAsync = controllerGeneratorModel.UseAsync,
                ControllerNamespace = controllerNameSpace,
                ModelMetadata = modelMetadata
            };

            var appBasePath = _applicationEnvironment.ApplicationBasePath;
            await _codeGeneratorActionsService.AddFileFromTemplateAsync(outputPath, templateName, TemplateFolders, templateModel);
            _logger.LogMessage("Added Controller : " + outputPath.Substring(appBasePath.Length));

            if (!controllerGeneratorModel.NoViews)
            {
                foreach (var viewTemplate in _views)
                {
                    var viewName = viewTemplate == "List" ? "Index" : viewTemplate;
                    // ToDo: This is duplicated from ViewGenerator.
                    bool isLayoutSelected = controllerGeneratorModel.UseDefaultLayout ||
                        !String.IsNullOrEmpty(controllerGeneratorModel.LayoutPage);

                    var viewTemplateModel = new ViewGeneratorTemplateModel()
                    {
                        ViewDataTypeName = modelTypeFullName,
                        ViewDataTypeShortName = model.Name,
                        ViewName = viewName,
                        LayoutPageFile = controllerGeneratorModel.LayoutPage,
                        IsLayoutPageSelected = isLayoutSelected,
                        IsPartialView = false,
                        ReferenceScriptLibraries = controllerGeneratorModel.ReferenceScriptLibraries,
                        ModelMetadata = modelMetadata,
                        JQueryVersion = "1.10.2"
                    };

                    var viewOutputPath = Path.Combine(
                        appBasePath,
                        Constants.ViewsFolderName,
                        templateModel.ControllerRootName,
                        viewName + Constants.ViewExtension);

                    await _codeGeneratorActionsService.AddFileFromTemplateAsync(viewOutputPath,
                        viewTemplate + Constants.RazorTemplateExtension, TemplateFolders, viewTemplateModel);

                    _logger.LogMessage("Added View : " + viewOutputPath.Substring(appBasePath.Length));
                }
            }
        }

        private async Task GenerateEmptyController(ControllerGeneratorModel controllerGeneratorModel,
            string controllerNamespace)
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

            var templateModel = new ClassNameModel()
            {
                ClassName = controllerGeneratorModel.ControllerName,
                NamespaceName = controllerNamespace
            };

            var templateName = "EmptyController.cshtml";
            var outputPath = ValidateAndGetOutputPath(controllerGeneratorModel);
            await _codeGeneratorActionsService.AddFileFromTemplateAsync(outputPath, templateName, TemplateFolders, templateModel);
            _logger.LogMessage("Added Controller : " + outputPath.Substring(_applicationEnvironment.ApplicationBasePath.Length));
        }

        private string ValidateAndGetOutputPath(ControllerGeneratorModel controllerGeneratorModel)
        {
            var outputPath = Path.Combine(
                _applicationEnvironment.ApplicationBasePath,
                Constants.ControllersFolderName,
                controllerGeneratorModel.ControllerName + Constants.CodeFileExtension);

            if (File.Exists(outputPath) && !controllerGeneratorModel.Force)
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.CurrentCulture,
                    "View file {0} exists, use -f option to overwrite",
                    outputPath));
            }

            return outputPath;
        }
    }
}