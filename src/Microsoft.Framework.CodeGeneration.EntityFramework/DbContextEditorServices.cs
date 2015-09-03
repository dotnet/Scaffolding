// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Dnx.Runtime;
using Microsoft.Framework.CodeGeneration.Templating;

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

            return CSharpSyntaxTree.ParseText(sourceText)
                .WithFilePath(GetPathForNewContext(dbContextTemplateModel.DbContextTypeName));
        }

        public AddModelResult AddModelToContext(ModelType dbContext, ModelType modelType)
        {
            if (!IsModelPropertyExists(dbContext.TypeSymbol, modelType.FullName))
            {
                // Todo : Need to add model and DbSet namespaces if required

                // Todo : Need pluralization for modelType.Name below as that's the property name
                var dbSetProperty = "public DbSet<" + modelType.FullName + "> " + modelType.Name + " { get; set; }" + Environment.NewLine;
                var propertyDeclarationWrapper = CSharpSyntaxTree.ParseText(dbSetProperty);

                // Todo : Consider using DeclaringSyntaxtReference 
                var sourceLocation = dbContext.TypeSymbol.Locations.Where(l => l.IsInSource).FirstOrDefault();
                if (sourceLocation != null)
                {
                    var syntaxTree = sourceLocation.SourceTree;
                    var rootNode = syntaxTree.GetRoot();
                    var dbContextNode = rootNode.FindNode(sourceLocation.SourceSpan);
                    var lastNode = dbContextNode.ChildNodes().Last();
                    var newNode = rootNode.InsertNodesAfter(lastNode,
                            propertyDeclarationWrapper.GetRoot().WithLeadingTrivia(lastNode.GetLeadingTrivia()).ChildNodes());

                    var modifiedTree = syntaxTree.WithRootAndOptions(newNode, syntaxTree.Options);

                    return new AddModelResult()
                    {
                        Added = true,
                        OldTree = syntaxTree,
                        NewTree = modifiedTree
                    };
                }
            }

            return new AddModelResult()
            {
                Added = false
            };
        }

        private string GetPathForNewContext(string contextTypeName)
        {
            //ToDo: What's the best place to write the DbContext?
            var appBasePath = _environment.ApplicationBasePath;
            var outputPath = Path.Combine(
                appBasePath,
                "Models",
                contextTypeName + ".cs");

            if (File.Exists(outputPath))
            {
                // Odd case, a file exists with the same name as the DbContextTypeName but perhaps
                // the type defined in that file is different, what should we do in this case?
                // How likely is the above scenario?
                // Perhaps we can enumerate files with prefix and generate a safe name? For now, just throw.
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture,
                    "There was an error creating a DbContext, the file {0} already exists",
                    outputPath));
            }

            return outputPath;
        }

        private bool IsModelPropertyExists(ITypeSymbol dbContext, string modelTypeFullName)
        {
            var propertySymbols = dbContext.GetMembers().Select(m => m as IPropertySymbol).Where(s => s != null);
            foreach (var pSymbol in propertySymbols)
            {
                var namedType = pSymbol.Type as INamedTypeSymbol; //When can this go wrong?
                if (namedType != null && namedType.IsGenericType && !namedType.IsUnboundGenericType &&
                    namedType.ContainingAssembly.Name == "EntityFramework.Core" &&
                    namedType.ContainingNamespace.ToDisplayString() == "Microsoft.Data.Entity" &&
                    namedType.Name == "DbSet") // What happens if the type is referenced in full in code??
                {
                    // Can we check for equality of typeSymbol itself?
                    if (namedType.TypeArguments.Any(t => t.ToDisplayString() == modelTypeFullName))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private IEnumerable<string> TemplateFolders
        {
            get
            {
                return TemplateFoldersUtilities.GetTemplateFolders(
                    containingProject: "Microsoft.Framework.CodeGeneration.EntityFramework",
                    applicationBasePath: _environment.ApplicationBasePath,
                    baseFolders: new[] { "DbContext" },
                    libraryManager: _libraryManager);
            }
        }
    }
}