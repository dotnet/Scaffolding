// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
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

        public async Task<SyntaxTree> AddNewContext([NotNull]NewDbContextTemplateModel dbContextTemplateModel)
        {
            var templateName = "NewLocalDbContext.cshtml";
            var templatePath = _filesLocator.GetFilePath(templateName, TemplateFolders);
            Contract.Assert(File.Exists(templatePath));

            var templateContent = File.ReadAllText(templatePath);
            var templateResult = await _templatingService.RunTemplateAsync(templateContent, dbContextTemplateModel);

            if (templateResult.ProcessingException != null)
            {
                throw new InvalidOperationException(string.Format(
                    "There was an error running the template {0}: {1}",
                    templatePath,
                    templateResult.ProcessingException.Message));
            }

            var newContextContent = templateResult.GeneratedText;

            var sourceText = SourceText.From(newContextContent);
            return CSharpSyntaxTree.ParseText(sourceText);
        }

        private IEnumerable<string> TemplateFolders
        {
            get
            {
                return TemplateFoldersUtilities.GetTemplateFolders(
                    containingProject: "Microsoft.Framework.CodeGeneration.EntityFramework",
                    baseFolders: new[] { "DbContext" },
                    applicationBasePath: _environment.ApplicationBasePath,
                    libraryManager: _libraryManager);
            }
        }
    }
}