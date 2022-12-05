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
using Microsoft.DotNet.Scaffolding.Shared;
using Microsoft.DotNet.Scaffolding.Shared.CodeModifier;
using Microsoft.DotNet.Scaffolding.Shared.Project;
using Microsoft.DotNet.Scaffolding.Shared.ProjectModel;
using Microsoft.VisualBasic;
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
        private readonly IConnectionStringsWriter _connectionStringsWriter;

        //private const strings
        private const string ConfigureServices = nameof(ConfigureServices);
        private const string IServiceCollection = nameof(IServiceCollection);
        private const string WebApplicationCreateBuilder = "WebApplication.CreateBuilder";
        private const string AddRazorPages = "Services.AddRazorPages()";
        private const string CreateBuilder = "CreateBuilder(args)";
        private const string Main = nameof(Main);

        public DbContextEditorServices(
            IProjectContext projectContext,
            IApplicationInfo applicationInfo,
            IFilesLocator filesLocator,
            ITemplating templatingService,
            IConnectionStringsWriter connectionStringsWriter)
            : this (projectContext, applicationInfo, filesLocator, templatingService, connectionStringsWriter, DefaultFileSystem.Instance)
        {
        }

        internal DbContextEditorServices(
            IProjectContext projectContext,
            IApplicationInfo applicationInfo,
            IFilesLocator filesLocator,
            ITemplating templatingService,
            IConnectionStringsWriter connectionStringsWriter,
            IFileSystem fileSystem)
        {
            _projectContext = projectContext;
            _applicationInfo = applicationInfo;
            _filesLocator = filesLocator;
            _templatingService = templatingService;
            _fileSystem = fileSystem;
            _connectionStringsWriter = connectionStringsWriter;
        }

        public async Task<SyntaxTree> AddNewContext(NewDbContextTemplateModel dbContextTemplateModel)
        {
            if (dbContextTemplateModel == null)
            {
                throw new ArgumentNullException(nameof(dbContextTemplateModel));
            }

            var templateName = "NewLocalDbContext.cshtml";
            return await AddNewContextItemsInternal(templateName, dbContextTemplateModel);
        }

        private async Task<SyntaxTree> AddNewContextItemsInternal(string templateName, NewDbContextTemplateModel dbContextTemplateModel)
        {
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


        public EditSyntaxTreeResult AddModelToContext(ModelType dbContext, ModelType modelType, bool nullableEnabled)
        {
            return AddModelToContext(dbContext, modelType, new Dictionary<string, string>() { { nameof(nullableEnabled), nullableEnabled.ToString() } });
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

        public EditSyntaxTreeResult EditStartupForNewContext(
            ModelType startUp,
            string dbContextTypeName,
            string dbContextNamespace,
            string dataBaseName,
            bool useSqlite,
            bool useTopLevelStatements)
        {
            Contract.Assert(startUp != null && startUp.TypeSymbol != null);
            Contract.Assert(!string.IsNullOrEmpty(dbContextTypeName));
            Contract.Assert(!string.IsNullOrEmpty(dataBaseName));

            var parameters = new Dictionary<string, string>
            {
                { nameof(NewDbContextTemplateModel.DbContextTypeName),  dbContextTypeName },
                { nameof(NewDbContextTemplateModel.DbContextNamespace),  dbContextNamespace },
                { "dataBaseName", dataBaseName},
                { "databaseType", useSqlite ? EfConstants.SQLite : EfConstants.SqlServer },
                { "useTopLevelStatements", useTopLevelStatements.ToString() }
            };

            return EditStartupForNewContext(startUp, parameters);
        }

        /// <summary>
        /// Get the StatementSyntax that adds the db context to the WebApplicationBuilder.
        /// </summary>
        /// <param name="rootNode">Using the base class to allow this var to be either CompilationUnitSyntax or a MethodBodySyntax
        ///                         To get the WebApplicationBuilder variable name
        /// </param>
        /// <param name="dbContextTypeName"></param>
        /// <param name="dataBaseName"></param>
        /// <param name="dataContextTypeString"></param>
        internal StatementSyntax GetAddDbContextStatement(SyntaxNode rootNode, string dbContextTypeName, string dataBaseName, DbType dataContextTypeString)
        {
            //get leading trivia. there should be atleast one member var statementLeadingTrivia = classSyntax.ChildNodes()
            var statementLeadingTrivia = rootNode.ChildNodes().First()?.GetLeadingTrivia().ToString();
            string textToAddAtEnd = AddDbContextString(minimalHostingTemplate: true, statementLeadingTrivia, dataContextTypeString);
            _connectionStringsWriter.AddConnectionString(dbContextTypeName, dataBaseName, dataContextTypeString);
            textToAddAtEnd = Environment.NewLine + textToAddAtEnd;

            //get builder identifier string, should exist
            var builderExpression = rootNode.ChildNodes().Where(st => st.ToString().Contains(WebApplicationCreateBuilder)).FirstOrDefault() as MemberDeclarationSyntax;
            var builderIdentifierString = GetBuilderIdentifier(builderExpression);

            //create syntax expression that adds DbContext
            //added InvalidOperationExceptino if Configuration.GetConnectionString returns null.
            string thang = string.Format(
                textToAddAtEnd,
                string.Format("{0}.Services", builderIdentifierString),
                dbContextTypeName,
                string.Format("{0}.Configuration", builderIdentifierString),
                string.Format(" ?? throw new InvalidOperationException(\"Connection string '{0}' not found.\")", dbContextTypeName));
        
            Console.WriteLine(thang);
            var expression = SyntaxFactory.ParseStatement(string.Format(textToAddAtEnd,
                    string.Format("{0}.Services", builderIdentifierString),
                    dbContextTypeName,
                    string.Format("{0}.Configuration", builderIdentifierString),
                    string.Format(" ?? throw new InvalidOperationException(\"Connection string '{0}' not found.\")", dbContextTypeName))).WithLeadingTrivia(SyntaxFactory.Whitespace(statementLeadingTrivia));

            return expression;
        }

        private string GetBuilderIdentifier(MemberDeclarationSyntax builderMember)
        {
            if (builderMember != null)
            {
                var builderVariable =
                    builderMember
                        ?.ChildNodes().Where(st => st is StatementSyntax).FirstOrDefault()
                        ?.ChildNodes().Where(decl => decl is VariableDeclarationSyntax).FirstOrDefault();

                var builderIdentifierString = string.Empty;
                if (builderVariable != null)
                {
                    var builderIdentifier = builderVariable as VariableDeclarationSyntax;
                    builderIdentifierString = builderIdentifier.Variables.FirstOrDefault()?.Identifier.ToString();
                }
                if (!string.IsNullOrEmpty(builderIdentifierString))
                {
                    return builderIdentifierString;
                }
            }
            return "builder";
        }
        
        private string AddDbContextString(bool minimalHostingTemplate, string statementLeadingTrivia, DbType databaseType)
        {
            string textToAddAtEnd = string.Empty;
            string additionalNewline = Environment.NewLine;
            string additionalLeadingTrivia = minimalHostingTemplate ? string.Empty : "    ";
            string leadingTrivia = minimalHostingTemplate ? string.Empty : statementLeadingTrivia;
            switch (databaseType)
            {
                case DbType.SQLite: 
                    textToAddAtEnd =
                        leadingTrivia + "{0}.AddDbContext<{1}>(options =>" + additionalNewline +
                        statementLeadingTrivia + additionalLeadingTrivia + "    options.UseSqlite({2}.GetConnectionString(\"{1}\"){3}));" + Environment.NewLine;
                    break;
            
                case DbType.SqlServer:
                    textToAddAtEnd =
                        leadingTrivia + "{0}.AddDbContext<{1}>(options =>" + additionalNewline +
                        statementLeadingTrivia + additionalLeadingTrivia + "    options.UseSqlServer({2}.GetConnectionString(\"{1}\"){3}));" + Environment.NewLine;
                    break;
                
                case DbType.CosmosDb:
                    textToAddAtEnd =
                        leadingTrivia + "{0}.AddDbContext<{1}>(options =>" + additionalNewline +
                        statementLeadingTrivia + additionalLeadingTrivia + "    options.UseCosmos({2}.GetConnectionString(\"{1}\"), \"DATABASE_NAME\"));" + Environment.NewLine;
                    break;
                
                case DbType.Postgres:
                    textToAddAtEnd =
                        leadingTrivia + "{0}.AddDbContext<{1}>(options =>" + additionalNewline +
                        statementLeadingTrivia + additionalLeadingTrivia + "    options.UseNpgsql({2}.GetConnectionString(\"{1}\"){3}));" + Environment.NewLine;
                    break;

                default:
                    break;
            }

            return textToAddAtEnd;
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
                    namedType.Name == "IConfiguration") 
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

        // Check for the model property on the input dbContext, as well as anything it inherits from.
        private bool IsModelPropertyExists(ITypeSymbol dbContext, string modelTypeFullName)
        {
            ITypeSymbol workingDbContext = dbContext;
            do
            {
                if (IsModelPropertyExistsOnSymbol(workingDbContext, modelTypeFullName))
                {
                    return true;
                }

                workingDbContext = workingDbContext.BaseType;
            } while (workingDbContext != null);

            return false;
        }

        private bool IsModelPropertyExistsOnSymbol(ITypeSymbol dbContext, string modelTypeFullName)
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

        public EditSyntaxTreeResult AddModelToContext(ModelType dbContext, ModelType modelType, IDictionary<string, string> parameters)
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
                    parameters.TryGetValue("nullableEnabled", out var nullableEnabled);
                    var nullableEnabledBool = string.IsNullOrEmpty(nullableEnabled) ? true : nullableEnabled.Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase);
                    var nullabilityClause = nullableEnabledBool ? " = default!;" : "";
                    // Todo : Need pluralization for property name below.
                    // It is not always safe to just use DbSet<modelType.Name> as there can be multiple class names in different namespaces.
                    var dbSetProperty = "public DbSet<" + modelType.FullName + "> " + safeModelName + " { get; set; }" + nullabilityClause + Environment.NewLine;
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

        public EditSyntaxTreeResult EditStartupForNewContext(ModelType startUp, IDictionary<string, string> parameters)
        {
            var declarationReference = startUp.TypeSymbol.DeclaringSyntaxReferences.FirstOrDefault();
            if (declarationReference != null)
            {
                //get all params
                parameters.TryGetValue(nameof(NewDbContextTemplateModel.DbContextTypeName), out var dbContextTypeName);
                parameters.TryGetValue("dataBaseName", out var dataBaseName);
                parameters.TryGetValue("databaseType", out var dataContextTypeString);
                DbType dataContextType = DbType.SqlServer;
                if (Enum.TryParse(typeof(DbType), dataContextTypeString, ignoreCase:true, out var dataContextTypeObj))
                {
                    dataContextType = (DbType)dataContextTypeObj;
                }
                parameters.TryGetValue("useTopLevelStatements", out var useTopLevelStatementsString);
                var useTopLevelStatements = useTopLevelStatementsString.Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase);
                parameters.TryGetValue(nameof(NewDbContextTemplateModel.DbContextNamespace), out var dbContextNamespace);
                Contract.Assert(!string.IsNullOrEmpty(dbContextTypeName));
                Contract.Assert(!string.IsNullOrEmpty(dataBaseName));

                //continue if got the prerequistite variables
                var sourceTree = declarationReference.SyntaxTree;
                var rootNode = sourceTree.GetRoot();

                var startUpClassNode = rootNode.FindNode(declarationReference.Span);

                var configRootProperty = TryGetIConfigurationRootProperty(startUp.TypeSymbol);
                //if using Startup.cs, the ConfigureServices method should exist. 
                if (startUpClassNode.ChildNodes()
                    .FirstOrDefault(n =>
                        n is MethodDeclarationSyntax syntax &&
                        syntax.Identifier.ToString() == ConfigureServices)
                            is MethodDeclarationSyntax configServicesMethod && configRootProperty != null)
                {
                    var servicesParam = configServicesMethod.ParameterList.Parameters
                        .FirstOrDefault(p => p.Type.ToString().Equals(IServiceCollection));

                    var statementLeadingTrivia = configServicesMethod.Body.OpenBraceToken.LeadingTrivia.ToString() + "    ";
                    if (servicesParam != null)
                    {
                        string textToAddAtEnd = AddDbContextString(minimalHostingTemplate: false, statementLeadingTrivia, dataContextType);
                        _connectionStringsWriter.AddConnectionString(dbContextTypeName, dataBaseName, dataContextType);
                        if (configServicesMethod.Body.Statements.Any())
                        {
                            textToAddAtEnd = Environment.NewLine + textToAddAtEnd;
                        }

                        //string.Empty instead of InvalidOperationException for legacy scenarios.
                        var expression = SyntaxFactory.ParseStatement(string.Format(textToAddAtEnd,
                            servicesParam.Identifier,
                            dbContextTypeName,
                            configRootProperty.Name,
                            string.Empty));

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
                //minimal hosting scenario
                else
                {
                    var statementLeadingTrivia = string.Empty;
                    StatementSyntax dbContextExpression = null;
                    var compilationSyntax = rootNode as CompilationUnitSyntax;
                    if (!useTopLevelStatements)
                    {
                        MethodDeclarationSyntax methodSyntax = DocumentBuilder.GetMethodFromSyntaxRoot(compilationSyntax, Main);
                        dbContextExpression = GetAddDbContextStatement(methodSyntax.Body, dbContextTypeName, dbContextNamespace, dataContextType);
                    }
                    else if (useTopLevelStatements)
                    {
                        dbContextExpression = GetAddDbContextStatement(compilationSyntax, dbContextTypeName, dbContextNamespace, dataContextType);
                    }

                    if (statementLeadingTrivia != null && dbContextExpression != null)
                    {
                        var newRoot = compilationSyntax;
                        //add additional namespaces
                        var namespacesToAdd = new[] { "Microsoft.EntityFrameworkCore", "Microsoft.Extensions.DependencyInjection", dbContextNamespace };
                        foreach (var namespaceName in namespacesToAdd)
                        {
                            newRoot = RoslynCodeEditUtilities.AddUsingDirectiveIfNeeded(namespaceName, newRoot);
                        }
                        if (!useTopLevelStatements)
                        {
                            MethodDeclarationSyntax methodSyntax = DocumentBuilder.GetMethodFromSyntaxRoot(newRoot, Main);
                            var modifiedBlock = methodSyntax.Body;
                            var statementToInsertAround = methodSyntax.Body.ChildNodes().Where(st => st.ToString().Contains(AddRazorPages)).FirstOrDefault();
                            if (statementToInsertAround == null)
                            {
                                statementToInsertAround = methodSyntax.Body.ChildNodes().Where(st => st.ToString().Contains(CreateBuilder)).FirstOrDefault();
                                modifiedBlock = methodSyntax.Body.InsertNodesAfter(statementToInsertAround, new List<StatementSyntax>() { dbContextExpression });
                            }
                            else
                            {
                                modifiedBlock = methodSyntax.Body.InsertNodesBefore(statementToInsertAround, new List<StatementSyntax>() { dbContextExpression });
                            }
                            var modifiedMethod = methodSyntax.WithBody(modifiedBlock);
                            newRoot = newRoot.ReplaceNode(methodSyntax, modifiedMethod);
                        }
                        else
                        {
                            var statementToInsertAfter = newRoot.Members.Where(st => st.ToString().Contains(AddRazorPages)).FirstOrDefault();
                            if (statementToInsertAfter == null)
                            {
                                statementToInsertAfter = newRoot.Members.Where(st => st.ToString().Contains(CreateBuilder)).FirstOrDefault();
                            }

                            newRoot = newRoot.InsertNodesAfter(statementToInsertAfter, new List<GlobalStatementSyntax>() { SyntaxFactory.GlobalStatement(dbContextExpression) });
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
