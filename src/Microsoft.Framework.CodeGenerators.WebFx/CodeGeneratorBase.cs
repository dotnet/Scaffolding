// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.Framework.CodeGeneration;
using Microsoft.Framework.CodeGeneration.Templating;
using Microsoft.Framework.Runtime;

namespace Microsoft.Framework.CodeGenerators.WebFx
{
    public abstract class CodeGeneratorBase : ICodeGenerator
    {
        protected CodeGeneratorBase(
            [NotNull]ILibraryManager libraryManager,
            [NotNull]IFilesLocator filesLocator,
            [NotNull]ITemplating templateService,
            [NotNull]IApplicationEnvironment applicationEnvironment)
        {
            LibraryManager = libraryManager;
            ApplicationEnvironment = applicationEnvironment;
            FilesLocator = filesLocator;
            TemplateService = templateService;
        }

        public virtual string[] TemplateFolders
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

        protected async Task AddFileFromTemplateAsync(
            [NotNull]string outputPath,
            [NotNull]string templateName,
            [NotNull]object templateModel)
        {
            var templateSearchPaths = TemplateFolders;
            var templatePath = FilesLocator.GetFilePath(templateName, templateSearchPaths);
            if (string.IsNullOrEmpty(templatePath))
            {
                throw new Exception(string.Format(
                    "Template file {0} not found within search paths {1}",
                    templateName,
                    string.Join(";", templateSearchPaths)));
            }

            Contract.Assert(File.Exists(templatePath));
            var templateContent = File.ReadAllText(templatePath);

            var templateResult = await TemplateService.RunTemplateAsync(templateContent, templateModel);

            if (templateResult.ProcessingException != null)
            {
                throw new Exception(string.Format(
                    "There was an error running the template {0}: {1}",
                    templatePath,
                    templateResult.ProcessingException.Message));
            }

            if (!Directory.Exists(Path.GetDirectoryName(outputPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            }
            File.WriteAllText(outputPath, templateResult.GeneratedText);
        }

        public ILibraryManager LibraryManager { get; private set; }

        public IApplicationEnvironment ApplicationEnvironment { get; private set; }

        public IFilesLocator FilesLocator { get; private set; }

        public ITemplating TemplateService { get; private set; }

        private IMetadataProjectReference GetProjectReference(string projectReferenceName)
        {
            return LibraryManager
                .GetLibraryExport(ApplicationEnvironment.ApplicationName)
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