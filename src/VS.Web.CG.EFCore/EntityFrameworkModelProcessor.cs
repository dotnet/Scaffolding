// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Design.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Microsoft.VisualStudio.Web.CodeGeneration.Utils;

namespace Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore
{
    internal class EntityFrameworkModelProcessor
    {
        private const string EFSqlServerPackageName = "Microsoft.EntityFrameworkCore.SqlServer";
        private const string NewDbContextFolderName = "Data";

        private string _dbContextFullTypeName;
        private ModelType _modelTypeSymbol;
        private string _areaName;
        private IDbContextEditorServices _dbContextEditorServices;
        private IModelTypesLocator _modelTypesLocator;
        private ILogger _logger;
        private ICodeGenAssemblyLoadContext _loader;
        private Workspace _workspace;
        private ReflectedTypesProvider _reflectedTypesProvider;
        private IProjectContext _projectContext;
        private IApplicationInfo _applicationInfo;
        private AssemblyAttributeGenerator _assemblyAttributeGenerator;
        private string _dbContextError;
        private SyntaxTree _dbContextSyntaxTree;
        private EditSyntaxTreeResult _startupEditResult;
        private IFileSystem _fileSystem;

        public EntityFrameworkModelProcessor (
            string dbContextFullTypeName,
            ModelType modelTypeSymbol,
            string areaName,
            ICodeGenAssemblyLoadContext loader,
            IDbContextEditorServices dbContextEditorServices,
            IModelTypesLocator modelTypesLocator,
            Workspace workspace,
            IProjectContext projectContext,
            IApplicationInfo applicationInfo,
            IFileSystem fileSystem,
            ILogger logger)
        {
            if (string.IsNullOrEmpty(dbContextFullTypeName))
            {
                throw new ArgumentException(nameof(dbContextFullTypeName));
            }

            _dbContextFullTypeName = dbContextFullTypeName;
            _modelTypeSymbol = modelTypeSymbol;
            _areaName = areaName;
            _dbContextEditorServices = dbContextEditorServices;
            _modelTypesLocator = modelTypesLocator;
            _logger = logger;
            _loader = loader;
            _projectContext = projectContext;
            _applicationInfo = applicationInfo;
            _fileSystem = fileSystem;
            _workspace = workspace;

            _assemblyAttributeGenerator = GetAssemblyAttributeGenerator();
        }

        public async Task Process()
        {
            var dbContextSymbols = _modelTypesLocator.GetType(_dbContextFullTypeName).ToList();
            var startupType = _modelTypesLocator.GetType("Startup").FirstOrDefault();
            var programType = _modelTypesLocator.GetType("Program").FirstOrDefault();

            ModelType dbContextSymbolInWebProject = null;
            if (!dbContextSymbols.Any())
            {
                await GenerateNewDbContextAndRegister(startupType, programType);
            }
            else if (TryGetDbContextSymbolInWebProject(dbContextSymbols, out dbContextSymbolInWebProject))
            {
                await AddModelTypeToExistingDbContextIfNeeded(dbContextSymbolInWebProject);
            }
            else
            {
                await EnsureDbContextInLibraryIsValid(dbContextSymbols.First());
            }

            var dbContextType = _reflectedTypesProvider.GetReflectedType(
                modelType: _dbContextFullTypeName,
                lookInDependencies: true);

            if (dbContextType == null)
            {
                throw new InvalidOperationException(_dbContextError);
            }

            var modelReflectionType = _reflectedTypesProvider.GetReflectedType(
                modelType: _modelTypeSymbol.FullName,
                lookInDependencies: true);
            if (modelReflectionType == null)
            {
                throw new InvalidOperationException(string.Format(MessageStrings.ModelTypeNotFound, _modelTypeSymbol.Name));
            }

            var reflectedStartupType = _reflectedTypesProvider.GetReflectedType(
                modelType: startupType.FullName,
                lookInDependencies: true);

            if (reflectedStartupType == null)
            {
                throw new InvalidOperationException(string.Format(MessageStrings.ModelTypeNotFound, reflectedStartupType.Name));
            }

            _logger.LogMessage(string.Format(MessageStrings.GettingEFMetadata, _modelTypeSymbol.Name));

            ModelMetadata = GetModelMetadata(dbContextType, modelReflectionType, reflectedStartupType);

            if (_dbContextSyntaxTree != null)
            {
                PersistSyntaxTree(_dbContextSyntaxTree);

                if (ContextProcessingStatus == ContextProcessingStatus.ContextAdded || ContextProcessingStatus == ContextProcessingStatus.ContextAddedButRequiresConfig)
                {
                    _logger.LogMessage(string.Format(MessageStrings.AddedDbContext, _dbContextSyntaxTree.FilePath.Substring(_applicationInfo.ApplicationBasePath.Length)));

                    if (ContextProcessingStatus != ContextProcessingStatus.ContextAddedButRequiresConfig)
                    {
                        PersistSyntaxTree(_startupEditResult.NewTree);
                    }
                    else
                    {
                        _logger.LogMessage(MessageStrings.AdditionalSteps);
                    }
                }
            }
        }


        public ContextProcessingStatus ContextProcessingStatus { get; private set;}
        public ModelMetadata ModelMetadata { get; private set;}

        /// <summary>
        /// Writes the DbContext to disk using the given Roslyn SyntaxTree.
        /// The method expects that SyntaxTree has a file path associated with it.
        /// Handles both writing a new file and editing an existing file.
        /// </summary>
        /// <param name="newTree"></param>
        private void PersistSyntaxTree(SyntaxTree newTree)
        {
            Debug.Assert(newTree != null);
            Debug.Assert(!String.IsNullOrEmpty(newTree.FilePath));

            _fileSystem.CreateDirectory(Path.GetDirectoryName(newTree.FilePath));
            _fileSystem.WriteAllText(newTree.FilePath, newTree.GetText().ToString());
        }

        private bool TryGetDbContextSymbolInWebProject(List<ModelType> dbContextSymbols, out ModelType dbContextSymbolInWebProject)
        {
            dbContextSymbolInWebProject = null;
            if (dbContextSymbols == null)
            {
                return false;
            }

            dbContextSymbolInWebProject = dbContextSymbols.FirstOrDefault(
                db => _projectContext.AssemblyName.Equals(db.TypeSymbol.ContainingAssembly.Name));

            return dbContextSymbolInWebProject != null;
        }

        private async Task EnsureDbContextInLibraryIsValid(ModelType dbContextSymbol)
        {
            var projectCompilation = await _workspace.CurrentSolution.Projects
                .First(project => project.AssemblyName == _projectContext.AssemblyName)
                .GetCompilationAsync();

            _reflectedTypesProvider = GetReflectedTypesProvider(
                projectCompilation,
                c =>
                {
                    c = c.AddSyntaxTrees(_assemblyAttributeGenerator.GenerateAttributeSyntaxTree());
                    return c;
                });

            var dbContextType = _reflectedTypesProvider.GetReflectedType(_dbContextFullTypeName, lookInDependencies:true);

            if (_reflectedTypesProvider.GetCompilationErrors() != null 
                && _reflectedTypesProvider.GetCompilationErrors().Any())
            {
                throw new InvalidOperationException(string.Format(
                    MessageStrings.FailedToCompileInMemory,
                    string.Join(Environment.NewLine, _reflectedTypesProvider.GetCompilationErrors())
                ));
            }

            var props = dbContextType.GetProperties()
                .Where(p => p.PropertyType.IsGenericType
                        && p.PropertyType.GenericTypeArguments.Any()
                        && p.PropertyType.Name.Equals("DbSet`1")
                        && p.PropertyType.Namespace.Equals("Microsoft.EntityFrameworkCore"));

            if (!props.Any(p => p.PropertyType.GenericTypeArguments[0].FullName.Equals(_modelTypeSymbol.FullName)))
            {
                throw new InvalidOperationException(string.Format(
                                        MessageStrings.ModelTypeCouldNotBeAdded,
                                        _modelTypeSymbol.FullName,
                                        _dbContextFullTypeName));
            }
        }

        private async Task AddModelTypeToExistingDbContextIfNeeded(ModelType dbContextSymbol)
        {
            var addResult = _dbContextEditorServices.AddModelToContext(dbContextSymbol, _modelTypeSymbol);
            var projectCompilation = await _workspace.CurrentSolution.Projects
                .First(project => project.AssemblyName == _projectContext.AssemblyName)
                .GetCompilationAsync();

            if (addResult.Edited)
            {
                ContextProcessingStatus = ContextProcessingStatus.ContextEdited;
                _dbContextSyntaxTree = addResult.NewTree;
                _logger.LogMessage(MessageStrings.CompilingWithModifiedDbContext);

                _reflectedTypesProvider = GetReflectedTypesProvider(
                    projectCompilation,
                    c =>
                    {
                        c = c.AddSyntaxTrees(_assemblyAttributeGenerator.GenerateAttributeSyntaxTree());
                        var oldTree = c.SyntaxTrees.FirstOrDefault(t => t.FilePath == addResult.OldTree.FilePath);
                        if (oldTree == null)
                        {
                            throw new InvalidOperationException(string.Format(
                                    MessageStrings.ModelTypeCouldNotBeAdded,
                                    _modelTypeSymbol.FullName,
                                    _dbContextFullTypeName));
                        }
                        return c.ReplaceSyntaxTree(oldTree, addResult.NewTree);
                    });

                var compilationErrors = _reflectedTypesProvider.GetCompilationErrors();
                _dbContextError = string.Format(
                    MessageStrings.DbContextCreationError,
                    (compilationErrors == null
                        ? string.Empty
                        : string.Join(Environment.NewLine, compilationErrors)));
            }
            else
            {
                _logger.LogMessage(MessageStrings.CompilingInMemory);

                _reflectedTypesProvider = GetReflectedTypesProvider(
                    projectCompilation,
                    c =>
                    {
                        c = c.AddSyntaxTrees(_assemblyAttributeGenerator.GenerateAttributeSyntaxTree());
                        return c;
                    });

                _dbContextError = string.Format(MessageStrings.DbContextTypeNotFound, _dbContextFullTypeName);
            }
        }

        private ReflectedTypesProvider GetReflectedTypesProvider(Compilation projectCompilation, Func<Compilation, Compilation> compilationModificationFunc)
        {
            return new ReflectedTypesProvider(
                projectCompilation,
                compilationModificationFunc,
                _projectContext,
                _loader,
                _logger);
        }

        private async Task GenerateNewDbContextAndRegister(ModelType startupType, ModelType programType)
        {
            AssemblyAttributeGenerator assemblyAttributeGenerator = GetAssemblyAttributeGenerator();
            _startupEditResult = new EditSyntaxTreeResult()
            {
                Edited = false
            };

            ValidateEFSqlServerDependency();

            // Create a new Context
            _logger.LogMessage(string.Format(MessageStrings.GeneratingDbContext, _dbContextFullTypeName));
            var dbContextTemplateModel = new NewDbContextTemplateModel(_dbContextFullTypeName, _modelTypeSymbol, programType);
            _dbContextSyntaxTree = await _dbContextEditorServices.AddNewContext(dbContextTemplateModel);
            ContextProcessingStatus = ContextProcessingStatus.ContextAdded;

            if (startupType != null)
            {
                _startupEditResult = _dbContextEditorServices.EditStartupForNewContext(startupType,
                    dbContextTemplateModel.DbContextTypeName,
                    dbContextTemplateModel.DbContextNamespace,
                    dataBaseName: dbContextTemplateModel.DbContextTypeName + "-" + Guid.NewGuid().ToString());
            }

            if (!_startupEditResult.Edited)
            {
                ContextProcessingStatus = ContextProcessingStatus.ContextAddedButRequiresConfig;

                // The created context would anyway fail to fetch metadata with a crypic message
                // It's better to throw with a meaningful message
                throw new InvalidOperationException(string.Format("{0} {1}", MessageStrings.FailedToEditStartup, MessageStrings.EnsureStartupClassExists));
            }
            _logger.LogMessage(MessageStrings.CompilingWithAddedDbContext);

            var projectCompilation = await _workspace.CurrentSolution.Projects
                .First(project => project.AssemblyName == _projectContext.AssemblyName)
                .GetCompilationAsync();

            _reflectedTypesProvider = GetReflectedTypesProvider(
                projectCompilation,
                c =>
                {
                    c = c.AddSyntaxTrees(assemblyAttributeGenerator.GenerateAttributeSyntaxTree());
                    c = c.AddSyntaxTrees(_dbContextSyntaxTree);
                    if (_startupEditResult.Edited)
                    {
                        c = c.ReplaceSyntaxTree(_startupEditResult.OldTree, _startupEditResult.NewTree);
                    }
                    return c;
                });

            var compilationErrors = _reflectedTypesProvider.GetCompilationErrors();
            _dbContextError = string.Format(
                MessageStrings.DbContextCreationError,
                (compilationErrors == null
                    ? string.Empty
                    : string.Join(Environment.NewLine, compilationErrors)));

            _dbContextSyntaxTree = _dbContextSyntaxTree.WithFilePath(GetPathForNewContext(dbContextTemplateModel.DbContextTypeName, _areaName));
        }

        private ModelMetadata GetModelMetadata(Type dbContextType, Type modelType, Type startupType)
        {
            if (dbContextType == null)
            {
                throw new ArgumentNullException(nameof(dbContextType));
            }

            if (modelType == null)
            {
                throw new ArgumentNullException(nameof(modelType));
            }

            DbContext dbContextInstance = TryCreateContextUsingAppCode(dbContextType, startupType);

            if (dbContextInstance == null)
            {
                throw new InvalidOperationException(string.Format(
                    MessageStrings.TypeCastToDbContextFailed,
                    dbContextType.FullName));
            }

            IEntityType entityType = null;
            try
            {
                entityType = dbContextInstance.Model.FindEntityType(modelType);
            }
            catch(Exception ex)
            {
                // We got an exception from the DbContext while finding the entityType.
                // The error here is useful to the user for taking corrective actions.
                _logger.LogMessage(ex.Message);
                throw;
            }

            if (entityType == null)
            {
                throw new InvalidOperationException(string.Format(
                    MessageStrings.NoEntityOfTypeInDbContext,
                    modelType.Name,
                    dbContextType.FullName));
            }

            return new ModelMetadata(entityType, dbContextType);
        }

        private DbContext TryCreateContextUsingAppCode(Type dbContextType, Type startupType)
        {
            try
            {
                // Use EF design APIs to get the DBContext instance.
                var operationHandler = new OperationReportHandler();
                var operationReporter = new OperationReporter(operationHandler);
                // EF infers the environment (Development/ Production) based on the environment variable
                // ASPNETCORE_ENVIRONMENT. This should already be set up by the CodeGeneration.Design process.
                var dbContextOperations = new DbContextOperations(
                    operationReporter,
                    dbContextType.GetTypeInfo().Assembly,
                    startupType.GetTypeInfo().Assembly,
                    Array.Empty<string>());

                var dbContextService = dbContextOperations.CreateContext(dbContextType.FullName);

                return dbContextService;
            }
            catch(Exception ex)
            {
                throw ex.Unwrap(_logger);
            }
        }

        private void ValidateEFSqlServerDependency()
        {
            if (_projectContext.GetPackage(EFSqlServerPackageName) == null)
            {
                throw new InvalidOperationException(MessageStrings.EFSqlServerPackageNotAvailable);
            }
        }

        private AssemblyAttributeGenerator GetAssemblyAttributeGenerator()
        {
            var originalAssembly = _loader.LoadFromName(
                new AssemblyName(
                    Path.GetFileNameWithoutExtension(_projectContext.AssemblyName)));
            return new AssemblyAttributeGenerator(originalAssembly);
        }

        private string GetPathForNewContext(string contextShortTypeName, string areaName)
        {
            var areaPath = string.IsNullOrEmpty(areaName) ? string.Empty : Path.Combine("Areas", areaName);
            var appBasePath = _applicationInfo.ApplicationBasePath;
            var outputPath = Path.Combine(
                appBasePath,
                areaPath,
                NewDbContextFolderName,
                contextShortTypeName + ".cs");

            if (File.Exists(outputPath))
            {
                // Odd case, a file exists with the same name as the DbContextTypeName but perhaps
                // the type defined in that file is different, what should we do in this case?
                // How likely is the above scenario?
                // Perhaps we can enumerate files with prefix and generate a safe name? For now, just throw.
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture,
                    MessageStrings.DbContextCreationError_fileExists,
                    outputPath));
            }

            return outputPath;
        }
    }
}