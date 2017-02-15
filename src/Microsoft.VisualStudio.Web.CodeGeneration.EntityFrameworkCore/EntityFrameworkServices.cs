// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Design.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.ProjectModel;
using Microsoft.VisualStudio.Web.CodeGeneration.DotNet;
using Microsoft.VisualStudio.Web.CodeGeneration.Utils;

namespace Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore
{
    public class EntityFrameworkServices : IEntityFrameworkService
    {
        private readonly IDbContextEditorServices _dbContextEditorServices;
        private readonly IApplicationInfo _applicationInfo;
        private readonly ICodeGenAssemblyLoadContext _loader;
        private readonly IModelTypesLocator _modelTypesLocator;
        private readonly IPackageInstaller _packageInstaller;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;
        private const string EFSqlServerPackageName = "Microsoft.EntityFrameworkCore.SqlServer";
        private const string EFSqlServerPackageVersion = "7.0.0-*";
        private const string NewDbContextFolderName = "Data";
        private readonly Workspace _workspace;
        private readonly IProjectContext _projectContext;
        private readonly IFileSystem _fileSystem;


        public EntityFrameworkServices(
            IProjectContext projectContext,
            IApplicationInfo applicationInfo,
            ICodeGenAssemblyLoadContext loader,
            IModelTypesLocator modelTypesLocator,
            IDbContextEditorServices dbContextEditorServices,
            IPackageInstaller packageInstaller,
            IServiceProvider serviceProvider,
            Workspace workspace,
            IFileSystem fileSystem,
            ILogger logger)
        {
            if (projectContext == null)
            {
                throw new ArgumentNullException(nameof(projectContext));
            }

            if (applicationInfo == null)
            {
                throw new ArgumentNullException(nameof(applicationInfo));
            }

            if (loader == null)
            {
                throw new ArgumentNullException(nameof(loader));
            }

            if (modelTypesLocator == null)
            {
                throw new ArgumentNullException(nameof(modelTypesLocator));
            }

            if (dbContextEditorServices == null)
            {
                throw new ArgumentNullException(nameof(dbContextEditorServices));
            }

            if (packageInstaller == null)
            {
                throw new ArgumentNullException(nameof(packageInstaller));
            }

            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if(workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            if(fileSystem == null)
            {
                throw new ArgumentNullException(nameof(fileSystem));
            }

            _projectContext = projectContext;
            _applicationInfo = applicationInfo;
            _loader = loader;
            _modelTypesLocator = modelTypesLocator;
            _dbContextEditorServices = dbContextEditorServices;
            _packageInstaller = packageInstaller;
            _serviceProvider = serviceProvider;
            _logger = logger;
            _workspace = workspace;
            _fileSystem = fileSystem;
        }

        public async Task<ContextProcessingResult> GetModelMetadata(string dbContextFullTypeName, ModelType modelTypeSymbol, string areaName)
        {
            Type dbContextType;
            SyntaxTree dbContextSyntaxTree = null;

            EditSyntaxTreeResult startUpEditResult = new EditSyntaxTreeResult()
            {
                Edited = false
            };

            ContextProcessingStatus state = ContextProcessingStatus.ContextAvailable;

            var dbContextSymbols = _modelTypesLocator.GetType(dbContextFullTypeName).ToList();
            var startupType = _modelTypesLocator.GetType("Startup").FirstOrDefault();
            Type modelReflectionType = null;
            ReflectedTypesProvider reflectedTypesProvider = null;
            string dbContextError = string.Empty;

            AssemblyAttributeGenerator assemblyAttributeGenerator = GetAssemblyAttributeGenerator();

            if (dbContextSymbols.Count == 0)
            {
                await ValidateEFSqlServerDependency();

                // Create a new Context
                _logger.LogMessage("Generating a new DbContext class " + dbContextFullTypeName);
                var dbContextTemplateModel = new NewDbContextTemplateModel(dbContextFullTypeName, modelTypeSymbol);
                dbContextSyntaxTree = await _dbContextEditorServices.AddNewContext(dbContextTemplateModel);
                state = ContextProcessingStatus.ContextAdded;

                // Edit startup class to register the context using DI
                if (startupType != null)
                {
                    startUpEditResult = _dbContextEditorServices.EditStartupForNewContext(startupType,
                        dbContextTemplateModel.DbContextTypeName,
                        dbContextTemplateModel.DbContextNamespace,
                        dataBaseName: dbContextTemplateModel.DbContextTypeName + "-" + Guid.NewGuid().ToString());
                }

                if (!startUpEditResult.Edited)
                {
                    state = ContextProcessingStatus.ContextAddedButRequiresConfig;

                    // The created context would anyway fail to fetch metadata with a crypic message
                    // It's better to throw with a meaningful message
                    throw new InvalidOperationException(string.Format("{0} {1}", MessageStrings.FailedToEditStartup, MessageStrings.EnsureStartupClassExists));
                }
                _logger.LogMessage("Attempting to compile the application in memory with the added DbContext");

                var projectCompilation = _workspace.CurrentSolution.Projects
                    .First(project => project.AssemblyName == _projectContext.AssemblyName)
                    .GetCompilationAsync().Result;

                reflectedTypesProvider = new ReflectedTypesProvider(
                    projectCompilation,
                    c =>
                    {
                        c = c.AddSyntaxTrees(assemblyAttributeGenerator.GenerateAttributeSyntaxTree());
                        c = c.AddSyntaxTrees(dbContextSyntaxTree);
                        if (startUpEditResult.Edited)
                        {
                            c = c.ReplaceSyntaxTree(startUpEditResult.OldTree, startUpEditResult.NewTree);
                        }
                        return c;
                    },
                    _projectContext,
                    _loader,
                    _logger);

                var compilationErrors = reflectedTypesProvider.GetCompilationErrors();
                dbContextError = string.Format(
                    MessageStrings.DbContextCreationError,
                    (compilationErrors == null
                        ? string.Empty
                        : string.Join(Environment.NewLine, compilationErrors)));

                // Add file information
                dbContextSyntaxTree = dbContextSyntaxTree.WithFilePath(GetPathForNewContext(dbContextTemplateModel.DbContextTypeName, areaName));
            }
            else
            {
                var addResult = _dbContextEditorServices.AddModelToContext(dbContextSymbols.First(), modelTypeSymbol);
                var projectCompilation = _workspace.CurrentSolution.Projects
                    .First(project => project.AssemblyName == _projectContext.AssemblyName)
                    .GetCompilationAsync().Result;

                if (addResult.Edited)
                {
                    state = ContextProcessingStatus.ContextEdited;
                    dbContextSyntaxTree = addResult.NewTree;
                    _logger.LogMessage("Attempting to compile the application in memory with the modified DbContext");

                    reflectedTypesProvider = new ReflectedTypesProvider(
                        projectCompilation,
                        c =>
                        {
                            c = c.AddSyntaxTrees(assemblyAttributeGenerator.GenerateAttributeSyntaxTree());
                            var oldTree = c.SyntaxTrees.FirstOrDefault(t => t.FilePath == addResult.OldTree.FilePath);
                            if (oldTree == null)
                            {
                                throw new InvalidOperationException(string.Format(
                                        MessageStrings.ModelTypeCouldNotBeAdded,
                                        modelTypeSymbol.FullName,
                                        dbContextFullTypeName));
                            }
                            return c.ReplaceSyntaxTree(oldTree, addResult.NewTree);
                        },
                        _projectContext,
                        _loader,
                        _logger);

                    var compilationErrors = reflectedTypesProvider.GetCompilationErrors();
                    dbContextError = string.Format(
                        MessageStrings.DbContextCreationError,
                        (compilationErrors == null
                            ? string.Empty
                            : string.Join(Environment.NewLine, compilationErrors)));
                }
                else
                {
                    _logger.LogMessage("Attempting to compile the application in memory");

                    reflectedTypesProvider = new ReflectedTypesProvider(
                        projectCompilation,
                        c =>
                        {
                            c = c.AddSyntaxTrees(assemblyAttributeGenerator.GenerateAttributeSyntaxTree());
                            return c;
                        },
                        _projectContext,
                        _loader,
                        _logger);

                    dbContextError = string.Format(MessageStrings.DbContextTypeNotFound, dbContextFullTypeName);
                }
            }
            dbContextType = reflectedTypesProvider.GetReflectedType(
                modelType: dbContextFullTypeName,
                lookInDependencies: true);

            if (dbContextType == null)
            {
                throw new InvalidOperationException(dbContextError);
            }

            modelReflectionType = reflectedTypesProvider.GetReflectedType(
                modelType: modelTypeSymbol.FullName,
                lookInDependencies: true);
            if (modelReflectionType == null)
            {
                throw new InvalidOperationException(string.Format(MessageStrings.ModelTypeNotFound, modelTypeSymbol.Name));
            }

            var reflectedStartupType = reflectedTypesProvider.GetReflectedType(
                modelType: startupType.FullName,
                lookInDependencies: true);

            if (reflectedStartupType == null)
            {
                throw new InvalidOperationException(string.Format(MessageStrings.ModelTypeNotFound, reflectedStartupType.Name));
            }

            _logger.LogMessage("Attempting to figure out the EntityFramework metadata for the model and DbContext: "+modelTypeSymbol.Name);

            var metadata = GetModelMetadata(dbContextType, modelReflectionType, reflectedStartupType);

            // Write the DbContext/Startup if getting the model metadata is successful
            if (dbContextSyntaxTree != null)
            {
                PersistSyntaxTree(dbContextSyntaxTree);
                if (state == ContextProcessingStatus.ContextAdded || state == ContextProcessingStatus.ContextAddedButRequiresConfig)
                {
                    _logger.LogMessage("Added DbContext : " + dbContextSyntaxTree.FilePath.Substring(_applicationInfo.ApplicationBasePath.Length));

                    if (state != ContextProcessingStatus.ContextAddedButRequiresConfig)
                    {
                        PersistSyntaxTree(startUpEditResult.NewTree);
                    }
                    else
                    {
                        _logger.LogMessage("However there may be additional steps required for the generted code to work properly, refer to documentation <forward_link>.");
                    }
                }
            }
            return new ContextProcessingResult()
            {
                ContextProcessingStatus = state,
                ModelMetadata = metadata
            };
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

        private async Task ValidateEFSqlServerDependency()
        {
            if (_projectContext.GetPackage(EFSqlServerPackageName) == null)
            {
                await _packageInstaller.InstallPackages(new List<PackageMetadata>()
                {
                    new PackageMetadata()
                    {
                        Name = EFSqlServerPackageName,
                        Version = EFSqlServerPackageVersion
                    }
                });

                throw new InvalidOperationException(MessageStrings.ScaffoldingNeedsToRerun);
            }
        }

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
                var dbContextOperations = new DbContextOperations(
                    operationReporter,
                    dbContextType.GetTypeInfo().Assembly,
                    startupType.GetTypeInfo().Assembly,
                    "Development",
                    Path.GetDirectoryName(_projectContext.ProjectFullPath));

                var dbContextService = dbContextOperations.CreateContext(dbContextType.FullName);

                return dbContextService;
            }
            catch(Exception ex)
            {
                throw ex.Unwrap(_logger);
            }
        }

        public Task<ContextProcessingResult> GetModelMetadata(ModelType modelType)
        {
            if (modelType == null)
            {
                throw new ArgumentNullException(nameof(modelType));
            }

            var projectCompilation = _workspace.CurrentSolution.Projects
                    .First(project => project.AssemblyName == _projectContext.AssemblyName)
                    .GetCompilationAsync().Result;

            var reflectedTypesProvider = new ReflectedTypesProvider(
                projectCompilation,
                (c) => c,
                _projectContext,
                _loader,
                _logger);
            var modelReflectionType = reflectedTypesProvider.GetReflectedType(
                modelType: modelType.FullName,
                lookInDependencies: true);

            if (modelReflectionType == null)
            {
                throw new InvalidOperationException(string.Format(MessageStrings.ModelTypeNotFound, modelType.Name));
            }

            var modelMetadata = new CodeModelMetadata(modelReflectionType);
            return Task.FromResult(new ContextProcessingResult()
            {
                ContextProcessingStatus = ContextProcessingStatus.ContextAvailable,
                ModelMetadata = modelMetadata
            });
        }
    }
}