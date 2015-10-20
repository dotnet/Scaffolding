// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Dnx.Runtime;
using Microsoft.Extensions.CodeGeneration.Templating;

namespace Microsoft.Extensions.CodeGeneration.EntityFramework
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

        public AddModelResult AddModelToContext(ModelType dbContext, ModelType modelType)
        {
            if (!IsModelPropertyExists(dbContext.TypeSymbol, modelType.FullName))
            {
                // Todo : Consider using DeclaringSyntaxtReference 
                var sourceLocation = dbContext.TypeSymbol.Locations.Where(l => l.IsInSource).FirstOrDefault();
                if (sourceLocation != null)
                {
                    var syntaxTree = sourceLocation.SourceTree;
                    var rootNode = syntaxTree.GetRoot();
                    var dbContextNode = rootNode.FindNode(sourceLocation.SourceSpan);
                    var lastNode = dbContextNode.ChildNodes().Last();

                    rootNode = RoslynCodeEditUtilities.AddUsingDirectiveIfNeeded(modelType.Namespace, rootNode);
                    rootNode = RoslynCodeEditUtilities.AddUsingDirectiveIfNeeded("Microsoft.Data.Entity", rootNode); //DbSet namespace

                    // Todo : Need pluralization for property name below.
                    var dbSetProperty = "public DbSet<" + modelType.Name + "> " + modelType.Name + " { get; set; }" + Environment.NewLine;
                    var propertyDeclarationWrapper = CSharpSyntaxTree.ParseText(dbSetProperty);

                    var newNode = rootNode.InsertNodesAfter(lastNode,
                            propertyDeclarationWrapper.GetRoot().WithTriviaFrom(lastNode).ChildNodes());

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
                    containingProject: "Microsoft.Extensions.CodeGeneration.EntityFramework",
                    applicationBasePath: _environment.ApplicationBasePath,
                    baseFolders: new[] { "DbContext" },
                    libraryManager: _libraryManager);
            }
        }
    }
}