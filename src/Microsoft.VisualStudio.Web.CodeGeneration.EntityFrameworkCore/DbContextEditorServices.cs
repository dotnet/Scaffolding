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
using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Microsoft.VisualStudio.Web.CodeGeneration.Templating;
using Newtonsoft.Json.Linq;

namespace Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore
{
    public class DbContextEditorServices : IDbContextEditorServices
    {
        private readonly IApplicationInfo _applicationInfo;
        //private readonly ILibraryManager _libraryManager;
        private readonly ITemplating _templatingService;
        private readonly IFilesLocator _filesLocator;
        private readonly IFileSystem _fileSystem;
        private readonly IProjectContext _projectContext;

        public DbContextEditorServices(
            IProjectContext projectContext,
            IApplicationInfo applicationInfo,
            IFilesLocator filesLocator,
            ITemplating templatingService)
            : this (projectContext, applicationInfo, filesLocator, templatingService, DefaultFileSystem.Instance)
        {
        }

        internal DbContextEditorServices(
            IProjectContext projectContext,
            IApplicationInfo applicationInfo,
            IFilesLocator filesLocator,
            ITemplating templatingService,
            IFileSystem fileSystem)
        {
            _projectContext = projectContext;
            _applicationInfo = applicationInfo;
            _filesLocator = filesLocator;
            _templatingService = templatingService;
            _fileSystem = fileSystem;
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

                    var safeModelName = GetSafeModelName(modelType.Name, dbContext.TypeSymbol);
                    // Todo : Need pluralization for property name below.
                    // It is not always safe to just use DbSet<modelType.Name> as there can be multiple class names in different namespaces.
                    var dbSetProperty = "public DbSet<" + modelType.FullName + "> " + safeModelName + " { get; set; }" + Environment.NewLine;
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

        private string GetSafeModelName(string name, ITypeSymbol dbContext)
        {
            var safeName = name;

            int i = 1;
            // We don't expect users to have more than a few symbols having the naming as modelName_1, modelName_2, etc.
            while (dbContext.GetMembers(safeName).Any())
            {
                safeName = $"{name}_{i++}";
            }

            return safeName;
        }

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

                var configRootProperty = TryGetIConfigurationRootProperty(startUp.TypeSymbol);

                if (configServicesMethod != null && configRootProperty != null)
                {
                    var servicesParam = configServicesMethod.ParameterList.Parameters
                        .FirstOrDefault(p => p.Type.ToString() == "IServiceCollection") as ParameterSyntax;

                    if (servicesParam != null)
                    {
                        AddConnectionString(dbContextTypeName, dataBaseName);
                        var statementLeadingTrivia = configServicesMethod.Body.OpenBraceToken.LeadingTrivia.ToString() + "    ";

                        string textToAddAtEnd =
                            statementLeadingTrivia + "{0}.AddDbContext<{1}>(options =>" + Environment.NewLine +
                            statementLeadingTrivia + "        options.UseSqlServer({2}.GetConnectionString(\"{1}\")));" + Environment.NewLine;

                        if (configServicesMethod.Body.Statements.Any())
                        {
                            textToAddAtEnd = Environment.NewLine + textToAddAtEnd;
                        }

                        var expression = SyntaxFactory.ParseStatement(String.Format(textToAddAtEnd,
                            servicesParam.Identifier,
                            dbContextTypeName,
                            configRootProperty.Name));

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

        private IPropertySymbol TryGetIConfigurationRootProperty(ITypeSymbol startup)
        {
            var propertySymbols = startup.GetMembers()
                .Select(m => m as IPropertySymbol)
                .Where(s => s != null);

            foreach (var pSymbol in propertySymbols)
            {
                var namedType = pSymbol.Type as INamedTypeSymbol; //When can this go wrong?
                if (namedType != null &&
                    namedType.ContainingAssembly.Name == "Microsoft.Extensions.Configuration.Abstractions" &&
                    namedType.ContainingNamespace.ToDisplayString() == "Microsoft.Extensions.Configuration" &&
                    namedType.Name == "IConfigurationRoot") 
                {
                    return pSymbol;
                }
            }

            return null;
        }

        // Internal for unit tests.
        internal void AddConnectionString(string connectionStringName, string dataBaseName)
        {
            var appSettingsFile = Path.Combine(_applicationInfo.ApplicationBasePath, "appsettings.json");
            JObject content;
            bool writeContent = false;

            if (!_fileSystem.FileExists(appSettingsFile))
            {
                content = new JObject();
                writeContent = true;
            }
            else
            {
                content = JObject.Parse(_fileSystem.ReadAllText(appSettingsFile));
            }

            string connectionStringNodeName = "ConnectionStrings";

            if (content[connectionStringNodeName] == null)
            {
                writeContent = true;
                content[connectionStringNodeName] = new JObject();
            }

            if (content[connectionStringNodeName][connectionStringName] == null)
            {
                writeContent = true;
                content[connectionStringNodeName][connectionStringName] =
                    string.Format("Server=(localdb)\\mssqllocaldb;Database={0};Trusted_Connection=True;MultipleActiveResultSets=true",
                        dataBaseName);
            }
            
            // Json.Net loses comments so the above code if requires any changes loses
            // comments in the file. The writeContent bool is for saving
            // a specific case without losing comments - when no changes are needed.
            if (writeContent)
            {
                _fileSystem.WriteAllText(appSettingsFile, content.ToString());
            }
        }

        private bool IsModelPropertyExists(ITypeSymbol dbContext, string modelTypeFullName)
        {
            var propertySymbols = dbContext.GetMembers().Select(m => m as IPropertySymbol).Where(s => s != null);
            foreach (var pSymbol in propertySymbols)
            {
                var namedType = pSymbol.Type as INamedTypeSymbol; //When can this go wrong?
                if (namedType != null && namedType.IsGenericType && !namedType.IsUnboundGenericType &&
                    namedType.Name == "DbSet")
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
                    containingProject: "Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore",
                    applicationBasePath: _applicationInfo.ApplicationBasePath,
                    baseFolders: new[] { "DbContext" },
                    projectContext: _projectContext);
            }
        }
    }
}