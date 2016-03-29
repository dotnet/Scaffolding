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
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.CodeGeneration.DotNet;
using Microsoft.Extensions.CodeGeneration.Templating;

namespace Microsoft.Extensions.CodeGeneration.EntityFrameworkCore
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

        public async Task<SyntaxTree> AddNewContext(NewDbContextTemplateModel dbContextTemplateModel)
        {
            if (dbContextTemplateModel == null)
            {
                throw new ArgumentNullException(nameof(dbContextTemplateModel));
            }

            var templateName = "NewLocalDbContext.cshtml";
            var templatePath = _filesLocator.GetFilePath(templateName, TemplateFolders);
            Contract.Assert(File.Exists(templatePath));

            var templateContent = File.ReadAllText(templatePath);
            var templateResult = await _templatingService.RunTemplateAsync(templateContent, dbContextTemplateModel);

            if (templateResult.ProcessingException != null)
            {
                throw new InvalidOperationException(string.Format(
                    MessageStrings.TemplateProcessingError,
                    templatePath,
                    templateResult.ProcessingException.Message));
            }

            var newContextContent = templateResult.GeneratedText;

            var sourceText = SourceText.From(newContextContent);

            return CSharpSyntaxTree.ParseText(sourceText);
        }

        public EditSyntaxTreeResult AddModelToContext(ModelType dbContext, ModelType modelType)
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

                    // Todo : Need pluralization for property name below.
                    var dbSetProperty = "public DbSet<" + modelType.Name + "> " + modelType.Name + " { get; set; }" + Environment.NewLine;
                    var propertyDeclarationWrapper = CSharpSyntaxTree.ParseText(dbSetProperty);

                    var newNode = rootNode.InsertNodesAfter(lastNode,
                            propertyDeclarationWrapper.GetRoot().WithTriviaFrom(lastNode).ChildNodes());

                    newNode = RoslynCodeEditUtilities.AddUsingDirectiveIfNeeded("Microsoft.EntityFrameworkCore", newNode as CompilationUnitSyntax); //DbSet namespace
                    newNode = RoslynCodeEditUtilities.AddUsingDirectiveIfNeeded(modelType.Namespace, newNode as CompilationUnitSyntax);

                    var modifiedTree = syntaxTree.WithRootAndOptions(newNode, syntaxTree.Options);

                    return new EditSyntaxTreeResult()
                    {
                        Edited = true,
                        OldTree = syntaxTree,
                        NewTree = modifiedTree
                    };
                }
            }

            return new EditSyntaxTreeResult()
            {
                Edited = false
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="startUp"></param>
        /// <returns></returns>
        public EditSyntaxTreeResult EditStartupForNewContext(ModelType startUp, string dbContextTypeName, string dbContextNamespace, string dataBaseName)
        {
            Contract.Assert(startUp != null && startUp.TypeSymbol != null);
            Contract.Assert(!String.IsNullOrEmpty(dbContextTypeName));
            Contract.Assert(!String.IsNullOrEmpty(dataBaseName));

            var declarationReference = startUp.TypeSymbol.DeclaringSyntaxReferences.FirstOrDefault();
            if (declarationReference != null)
            {
                var sourceTree = declarationReference.SyntaxTree;
                var rootNode = sourceTree.GetRoot();

                var startUpClassNode = rootNode.FindNode(declarationReference.Span);

                var configServicesMethod = startUpClassNode.ChildNodes()
                    .FirstOrDefault(n => n is MethodDeclarationSyntax
                        && ((MethodDeclarationSyntax)n).Identifier.ToString() == "ConfigureServices") as MethodDeclarationSyntax;

                if (configServicesMethod != null)
                {
                    var servicesParam = configServicesMethod.ParameterList.Parameters
                        .FirstOrDefault(p => p.Type.ToString() == "IServiceCollection") as ParameterSyntax;

                    if (servicesParam != null)
                    {
                        var statementLeadingTrivia = configServicesMethod.Body.OpenBraceToken.LeadingTrivia.ToString() + "    ";

                        string textToAddAtEnd =
                            statementLeadingTrivia + "{0}.AddDbContext<{1}>(options =>" + Environment.NewLine +
                            statementLeadingTrivia + "        options.UseSqlServer(@\"Server=(localdb)\\mssqllocaldb;Database={2};Trusted_Connection=True;MultipleActiveResultSets=true\"));" + Environment.NewLine;

                        if (configServicesMethod.Body.Statements.Any())
                        {
                            textToAddAtEnd = Environment.NewLine + textToAddAtEnd;
                        }

                        var expression = SyntaxFactory.ParseStatement(String.Format(textToAddAtEnd,
                            servicesParam.Identifier,
                            dbContextTypeName,
                            dataBaseName));

                        MethodDeclarationSyntax newConfigServicesMethod = configServicesMethod.AddBodyStatements(expression);

                        var newRoot = rootNode.ReplaceNode(configServicesMethod, newConfigServicesMethod);

                        var namespacesToAdd = new[] { "Microsoft.EntityFrameworkCore", "Microsoft.Extensions.DependencyInjection", dbContextNamespace };
                        foreach (var namespaceName in namespacesToAdd)
                        {
                            newRoot = RoslynCodeEditUtilities.AddUsingDirectiveIfNeeded(namespaceName, newRoot as CompilationUnitSyntax);
                        }

                        return new EditSyntaxTreeResult()
                        {
                            Edited = true,
                            OldTree = sourceTree,
                            NewTree = sourceTree.WithRootAndOptions(newRoot, sourceTree.Options)
                        };
                    }
                }
            }

            return new EditSyntaxTreeResult()
            {
                Edited = false
            };
        }

        private bool IsModelPropertyExists(ITypeSymbol dbContext, string modelTypeFullName)
        {
            var propertySymbols = dbContext.GetMembers().Select(m => m as IPropertySymbol).Where(s => s != null);
            foreach (var pSymbol in propertySymbols)
            {
                var namedType = pSymbol.Type as INamedTypeSymbol; //When can this go wrong?
                if (namedType != null && namedType.IsGenericType && !namedType.IsUnboundGenericType &&
                    namedType.ContainingAssembly.Name == "Microsoft.EntityFrameworkCore" &&
                    namedType.ContainingNamespace.ToDisplayString() == "Microsoft.EntityFrameworkCore" &&
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
                    containingProject: "Microsoft.Extensions.CodeGeneration.EntityFrameworkCore",
                    applicationBasePath: _environment.ApplicationBasePath,
                    baseFolders: new[] { "DbContext" },
                    libraryManager: _libraryManager);
            }
        }
    }
}