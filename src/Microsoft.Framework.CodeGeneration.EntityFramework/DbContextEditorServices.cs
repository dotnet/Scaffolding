// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Framework.CodeGeneration;
using Microsoft.Framework.CodeGeneration.Templating;
using Microsoft.Framework.Runtime;

namespace Microsoft.Framework.CodeGeneration.EntityFramework
{
    public class DbContextEditorServices : IDbContextEditorServices
    {
        private readonly IApplicationEnvironment _environment;
        private readonly ILibraryManager _libraryManager;
        private readonly ITemplating _templatingService;
        private readonly IFilesLocator _filesLocator;
        private static int _counter = 1;

        public DbContextEditorServices(
            ILibraryManager libraryManager,
            IApplicationEnvironment environment,
            IFilesLocator filesLocator,
            ITemplating templatingService)
        {
            _libraryManager = libraryManager;
            _environment = environment;
            _filesLocator = filesLocator;
            _templatingService = templatingService;
        }

        public async Task<Compilation> AddNewContext([NotNull]string dbContextName,[NotNull]ITypeSymbol modelType)
        {
            var templateName = "NewLocalDbContext.cshtml";
            var templatePath = _filesLocator.GetFilePath(templateName, TemplateFolders);
            Contract.Assert(templatePath != null);
            Contract.Assert(File.Exists(templatePath));

            var templateModel = new NewDbContextTemplateModel(dbContextName, modelType);

            var templateContent = File.ReadAllText(templatePath);
            var templateResult = await _templatingService.RunTemplateAsync(templateContent, templateModel);

            if (templateResult.ProcessingException != null)
            {
                throw new Exception(string.Format(
                    "There was an error running the template {0}: {1}",
                    templatePath,
                    templateResult.ProcessingException.Message));
            }

            var newContextContent = templateResult.GeneratedText;

            var sourceText = SourceText.From(newContextContent);
            var tree = CSharpSyntaxTree.ParseText(sourceText);

            var projectCompilation = _libraryManager.GetProject(_environment).Compilation;
            var newAssemblyName = projectCompilation.AssemblyName + _counter++;
            return projectCompilation.AddSyntaxTrees(tree).WithAssemblyName(newAssemblyName);
        }

        private IEnumerable<string> TemplateFolders
        {
            get
            {
                return TemplateFoldersUtil.GetTemplateFolders(
                    containingProject: "Microsoft.Framework.CodeGeneration.EntityFramework",
                    libraryManager: _libraryManager,
                    appEnvironment: _environment);
            }
        }
    }
}