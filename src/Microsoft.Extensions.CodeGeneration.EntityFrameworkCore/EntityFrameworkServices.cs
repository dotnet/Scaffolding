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
using Microsoft.AspNet.Hosting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Data.Entity;
using Microsoft.Extensions.CompilationAbstractions;
using Microsoft.Extensions.PlatformAbstractions;

namespace Microsoft.Extensions.CodeGeneration.EntityFramework
{
    public class EntityFrameworkServices : IEntityFrameworkService
    {
        private readonly IDbContextEditorServices _dbContextEditorServices;
        private readonly IApplicationEnvironment _environment;
        private readonly ILibraryManager _libraryManager;
        private readonly ILibraryExporter _libraryExporter;
        private readonly IAssemblyLoadContext _loader;
        private readonly IModelTypesLocator _modelTypesLocator;
        private readonly IPackageInstaller _packageInstaller;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;
        private static int _counter = 1;
        private const string EFSqlServerPackageName = "EntityFramework.MicrosoftSqlServer";
        private const string EFSqlServerPackageVersion = "7.0.0-*";

        public EntityFrameworkServices(
            ILibraryManager libraryManager,
            ILibraryExporter libraryExporter,
            IApplicationEnvironment environment,
            IAssemblyLoadContextAccessor loader,
            IModelTypesLocator modelTypesLocator,
            IDbContextEditorServices dbContextEditorServices,
            IPackageInstaller packageInstaller,
            IServiceProvider serviceProvider,
            ILogger logger)
        {
            if (libraryManager == null)
            {
                throw new ArgumentNullException(nameof(libraryManager));
            }

            if (libraryExporter == null)
            {
                throw new ArgumentNullException(nameof(libraryExporter));
            }

            if (environment == null)
            {
                throw new ArgumentNullException(nameof(environment));
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

            _libraryManager = libraryManager;
            _libraryExporter = libraryExporter;
            _environment = environment;
            _loader = loader.GetLoadContext(typeof(EntityFrameworkServices).GetTypeInfo().Assembly);
            _modelTypesLocator = modelTypesLocator;
            _dbContextEditorServices = dbContextEditorServices;
            _packageInstaller = packageInstaller;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task<ContextProcessingResult> GetModelMetadata(string dbContextFullTypeName, ModelType modelTypeSymbol)
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

                dbContextType = CompileAndGetDbContext(dbContextFullTypeName, c =>
                {
                    c = c.AddSyntaxTrees(dbContextSyntaxTree);
                    if (startUpEditResult.Edited)
                    {
                        c = c.ReplaceSyntaxTree(startUpEditResult.OldTree, startUpEditResult.NewTree);
                    }
                    return c;
                });

                // Add file information
                dbContextSyntaxTree = dbContextSyntaxTree.WithFilePath(GetPathForNewContext(dbContextTemplateModel.DbContextTypeName));
            }
            else
            {
                var addResult = _dbContextEditorServices.AddModelToContext(dbContextSymbols.First(), modelTypeSymbol);
                if (addResult.Edited)
                {
                    state = ContextProcessingStatus.ContextEdited;
                    dbContextSyntaxTree = addResult.NewTree;
                    dbContextType = CompileAndGetDbContext(dbContextFullTypeName, c =>
                    {
                        var oldTree = c.SyntaxTrees.FirstOrDefault(t => t.FilePath == addResult.OldTree.FilePath);
                        Debug.Assert(oldTree != null);
                        return c.ReplaceSyntaxTree(oldTree, addResult.NewTree);
                    });
                }
                else
                {
                    dbContextType = _libraryExporter.GetReflectionType(_libraryManager, _environment, dbContextFullTypeName);

                    if (dbContextType == null)
                    {
                        throw new InvalidOperationException(string.Format(MessageStrings.DbContextTypeNotFound, dbContextFullTypeName));
                    }
                }
            }

            var modelTypeName = modelTypeSymbol.FullName;
            var modelType = _libraryExporter.GetReflectionType(_libraryManager, _environment, modelTypeName);

            if (modelType == null)
            {
                throw new InvalidOperationException(string.Format(MessageStrings.ModelTypeNotFound, modelTypeName));
            }

            _logger.LogMessage("Attempting to figure out the EntityFramework metadata for the model and DbContext");
            var metadata = GetModelMetadata(dbContextType, modelType, startupType);

            // Write the DbContext/Startup if getting the model metadata is successful
            if (dbContextSyntaxTree != null)
            {
                PersistSyntaxTree(dbContextSyntaxTree);
                if (state == ContextProcessingStatus.ContextAdded || state == ContextProcessingStatus.ContextAddedButRequiresConfig)
                {
                    _logger.LogMessage("Added DbContext : " + dbContextSyntaxTree.FilePath.Substring(_environment.ApplicationBasePath.Length));

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

        private Type CompileAndGetDbContext(string dbContextTypeName, Func<CodeAnalysis.Compilation, CodeAnalysis.Compilation> compilationModificationFunc)
        {
            Type dbContextType;

            _logger.LogMessage("Attempting to compile the application in memory with the added/modified DbContext");
            var projectCompilation = _libraryExporter.GetProject(_environment).Compilation;
            var newAssemblyName = projectCompilation.AssemblyName + _counter++;

            var newCompilation = compilationModificationFunc(projectCompilation).WithAssemblyName(newAssemblyName);

            var result = CommonUtilities.GetAssemblyFromCompilation(_loader, newCompilation);

            if (result.Success)
            {
                dbContextType = result.Assembly.GetType(dbContextTypeName);
                if (dbContextType == null)
                {
                    throw new InvalidOperationException(MessageStrings.DbContextCreationError_noTypeReturned);
                }
            }
            else
            {
                throw new InvalidOperationException(string.Format(MessageStrings.DbContextCreationError, string.Join("\n", result.ErrorMessages)));
            }

            return dbContextType;
        }

        private string GetPathForNewContext(string contextShortTypeName)
        {
            var appBasePath = _environment.ApplicationBasePath;
            var outputPath = Path.Combine(
                appBasePath,
                "Models",
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
            if (_libraryManager.GetLibrary(EFSqlServerPackageName) == null)
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

            Directory.CreateDirectory(Path.GetDirectoryName(newTree.FilePath));

            using (var fileStream = new FileStream(newTree.FilePath, FileMode.OpenOrCreate, FileAccess.Write))
            {
                using (var streamWriter = new StreamWriter(stream: fileStream, encoding: Encoding.UTF8))
                {
                    newTree.GetText().Write(streamWriter);
                }
            }
        }

        private ModelMetadata GetModelMetadata(Type dbContextType, Type modelType, ModelType startupType)
        {
            if (dbContextType == null)
            {
                throw new ArgumentNullException(nameof(dbContextType));
            }

            if (modelType == null)
            {
                throw new ArgumentNullException(nameof(modelType));
            }

            DbContext dbContextInstance;
            try
            {
                dbContextInstance = TryCreateContextUsingAppCode(dbContextType, startupType);
                if (dbContextInstance == null)
                {
                    dbContextInstance = Activator.CreateInstance(dbContextType) as DbContext;
                }
            }
            catch (Exception ex)
            {
                while (ex is TargetInvocationException)
                {
                    ex = ex.InnerException;
                }
                _logger.LogMessage("There was an error creating the DbContext instance to get the model.", LogMessageLevel.Error);
                throw ex;
            }

            if (dbContextInstance == null)
            {
                throw new InvalidOperationException(string.Format(
                    MessageStrings.TypeCastToDbContextFailed,
                    dbContextType.FullName));
            }

            var entityType = dbContextInstance.Model.FindEntityType(modelType);
            if (entityType == null)
            {
                throw new InvalidOperationException(string.Format(
                    MessageStrings.NoEntityOfTypeInDbContext,
                    modelType.FullName,
                    dbContextType.FullName));
            }

            return new ModelMetadata(entityType, dbContextType);
        }

        private DbContext TryCreateContextUsingAppCode(Type dbContextType, ModelType startupType)
        {
            var builder = new WebHostBuilder();
            if (startupType != null)
            {
                var reflectedStartupType = dbContextType.GetTypeInfo().Assembly.GetType(startupType.FullName);
                if (reflectedStartupType != null)
                {
                    builder.UseStartup(reflectedStartupType);
                }
            }
            var appServices = builder.Build().Services;
            return appServices.GetService(dbContextType) as DbContext;
        }
    }
}