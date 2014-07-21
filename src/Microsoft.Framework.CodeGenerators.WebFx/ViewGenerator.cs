// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.Framework.CodeGeneration;
using Microsoft.Framework.CodeGeneration.CommandLine;
using Microsoft.Framework.CodeGeneration.EntityFramework;
using Microsoft.Framework.CodeGeneration.Templating;
using Microsoft.Framework.Runtime;

namespace Microsoft.Framework.CodeGenerators.WebFx
{
    [Alias("view")]
    public class ViewGenerator : ICodeGenerator
    {
        private readonly ILogger _logger;
        private readonly IModelTypesLocator _modelTypesLocator;
        private readonly ILibraryManager _libraryManager;
        private readonly IEntityFrameworkService _entityFrameworkService;
        private readonly IFilesLocator _filesLocator;
        private readonly ITemplating _templateService;
        private readonly IApplicationEnvironment _environment;

        // Todo: Instead of each generator taking ILogger provide it in some base class?
        // However for it to be effective, it should be property dependecy injection rather
        // than constructor injection.
        public ViewGenerator(
            [NotNull]ILibraryManager libraryManager,
            [NotNull]IApplicationEnvironment environment,
            [NotNull]IModelTypesLocator modelTypesLocator,
            [NotNull]IEntityFrameworkService entityFrameworkService,
            [NotNull]ITemplating templateService, 
            [NotNull]IFilesLocator filesLocator,
            [NotNull]ILogger logger)
        {
            _libraryManager = libraryManager;
            _environment = environment;
            _modelTypesLocator = modelTypesLocator;
            _entityFrameworkService = entityFrameworkService;
            _templateService = templateService;
            _logger = logger;
            _filesLocator = filesLocator;
        }

        public async void GenerateCode([NotNull]ViewGeneratorModel viewGeneratorModel)
        {
            // Validate model
            string validationMessage;
            ITypeSymbol model, dataContext;

            if (!TryValidateType(viewGeneratorModel.ModelClass, "model", out model, out validationMessage) ||
                !TryValidateType(viewGeneratorModel.DataContextClass, "dataContext", out dataContext, out validationMessage))
            {
                throw new Exception(validationMessage);
            }

            // Validation successful
            Contract.Assert(model != null, "Validation succeded but model type not set");
            Contract.Assert(dataContext != null, "Validation succeded but DataContext type not set");

            if (string.IsNullOrEmpty(viewGeneratorModel.ViewName))
            {
                throw new Exception("The ViewName cannot be empty");
            }

            var templateFileName = viewGeneratorModel.TemplateName + ".cshtml";
            var templateSearchPaths = TemplateFolders;
            var templatePath = _filesLocator.GetFilePath(templateFileName, templateSearchPaths);
            if (string.IsNullOrEmpty(templatePath))
            {
                throw new Exception(string.Format(
                    "Template file {0} not found within search paths {1}",
                    templateFileName,
                    string.Join(";", templateSearchPaths)));
            }

            Contract.Assert(File.Exists(templatePath));
            var templateContent = File.ReadAllText(templatePath);

            var dbContextFullName = FullNameForType(dataContext);
            var modelTypeFullName = FullNameForType(model);

            var modelMetadata = _entityFrameworkService.GetModelMetadata(
                dbContextFullName,
                modelTypeFullName);

            var templateModel = new ViewGeneratorTemplateModel()
            {
                ViewDataTypeName = modelTypeFullName,
                ViewName = viewGeneratorModel.ViewName,
                LayoutPageFile = viewGeneratorModel.LayoutPage,
                IsLayoutPageSelected = viewGeneratorModel.UseLayout,
                IsPartialView = viewGeneratorModel.PartialView,
                ReferenceScriptLibraries = viewGeneratorModel.ReferenceScriptLibraries,
                ModelMetadata = modelMetadata,
                JQueryVersion = "1.10.2" //Todo
            };

            var templateResult = await _templateService.RunTemplateAsync(templateContent, templateModel);

            if (templateResult.ProcessingException != null)
            {
                throw new Exception(string.Format(
                    "There was an error running the template {0}: {1}",
                    templateFileName,
                    templateResult.ProcessingException.Message));
            }

            var outputPath = Path.Combine(_environment.ApplicationBasePath, "Views", model.Name, viewGeneratorModel.ViewName + ".cshtml");
            if (!Directory.Exists(Path.GetDirectoryName(outputPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            }
            File.WriteAllText(outputPath, templateResult.GeneratedText);
        }

        private bool TryValidateType(string typeName, string argumentName,
            out ITypeSymbol type, out string errorMessage)
        {
            errorMessage = string.Empty;
            type = null;

            if (string.IsNullOrEmpty(typeName))
            {
                //Perhaps for these kind of checks, the validation could be in the API.
                errorMessage = string.Format("Please provide a valid {0}", argumentName);
                return false;
            }

            var candidateModelTypes = _modelTypesLocator.GetType(typeName).ToList();

            int count = candidateModelTypes.Count;
            if (count == 0)
            {
                errorMessage = string.Format("A type with the name {0} does not exist", typeName);
                return false;
            }

            if (count > 1)
            {
                errorMessage = string.Format(
                    "Multiple types matching the name {0} exist:{1}, please use a fully qualified name",
                    typeName,
                    string.Join(",", candidateModelTypes.Select(t => t.Name).ToArray()));
                return false;
            }

            type = candidateModelTypes.First();
            return true;
        }

        // ToDo: Perhaps find some existing utility in Roslyn or provide this as API?
        private string FullNameForType([NotNull]ISymbol symbol)
        {
            if (symbol.ContainingNamespace != null & string.IsNullOrEmpty(symbol.ContainingNamespace.Name))
            {
                return symbol.Name;
            }
            return FullNameForType(symbol.ContainingNamespace) + "." + symbol.Name;
        }

        private string[] TemplateFolders
        {
            get
            {
                string templatesFolderName = "Templates";
                var templateFolders = new List<string>();

                var webFxProjReference = GetProjectReference("Microsoft.Framework.CodeGenerators.WebFx");
                if (webFxProjReference != null)
                {
                    templateFolders.Add(Path.Combine(
                        Path.GetDirectoryName(webFxProjReference.ProjectPath),
                        templatesFolderName));
                }
                //Todo: Get the path of  executing assembly and add it to template folders
                //var webFxAssemblyLocation = typeof(ViewGenerator).GetTypeInfo().Assembly.CodeBase;
                //if (!string.IsNullOrEmpty(webFxAssemblyLocation))
                //{
                //    templateFolders.Add(Path.Combine(webFxAssemblyLocation, templatesFolderName));
                //}
                return templateFolders.ToArray();
            }
        }

        private IMetadataProjectReference GetProjectReference(string projectReferenceName)
        {
            return _libraryManager
                .GetLibraryExport(_environment.ApplicationName)
                .MetadataReferences
                .OfType<IRoslynMetadataReference>()
                .Where(reference =>
                {
                    var compilation = reference.MetadataReference as CompilationReference;
                    return compilation != null &&
                        string.Equals(projectReferenceName, compilation.Compilation.AssemblyName);
                })
                .OfType<IMetadataProjectReference>()
                .FirstOrDefault();
        }
    }
}