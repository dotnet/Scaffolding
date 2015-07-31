// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.Data.Entity;
using Microsoft.Dnx.Compilation;
using Microsoft.Dnx.Runtime;

namespace Microsoft.Framework.CodeGeneration.EntityFramework
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
        private readonly ILogger _logger;
        private static int _counter = 1;
        private const string EFSqlServerPackageName = "EntityFramework.SqlServer";
        private const string EFSqlServerPackageVersion = "7.0.0-*";

        public EntityFrameworkServices(
            [NotNull]ILibraryManager libraryManager,
            [NotNull]ILibraryExporter libraryExporter,
            [NotNull]IApplicationEnvironment environment,
            [NotNull]IAssemblyLoadContextAccessor loader,
            [NotNull]IModelTypesLocator modelTypesLocator,
            [NotNull]IDbContextEditorServices dbContextEditorServices,
            [NotNull]IPackageInstaller packageInstaller,
            [NotNull]ILogger logger)
        {
            _libraryExporter = libraryExporter;
            _environment = environment;
            _loader = loader.GetLoadContext(typeof(EntityFrameworkServices).GetTypeInfo().Assembly);
            _modelTypesLocator = modelTypesLocator;
            _dbContextEditorServices = dbContextEditorServices;
            _packageInstaller = packageInstaller;
            _logger = logger;
        }

        public async Task<ModelMetadata> GetModelMetadata(string dbContextTypeName, ModelType modelTypeSymbol)
        {
            Type dbContextType;
            var dbContextSymbols = _modelTypesLocator.GetType(dbContextTypeName).ToList();
            var isNewDbContext = false;
            SyntaxTree newDbContextTree = null;
            NewDbContextTemplateModel dbContextTemplateModel = null;

            if (dbContextSymbols.Count == 0)
            {
                isNewDbContext = true;
                await ValidateEFSqlServerDependency();

                _logger.LogMessage("Generating a new DbContext class " + dbContextTypeName);
                dbContextTemplateModel = new NewDbContextTemplateModel(dbContextTypeName, modelTypeSymbol);
                newDbContextTree = await _dbContextEditorServices.AddNewContext(dbContextTemplateModel);

                _logger.LogMessage("Attempting to compile the application in memory with the added DbContext");
                var projectCompilation = _libraryExporter.GetProject(_environment).Compilation;
                var newAssemblyName = projectCompilation.AssemblyName + _counter++;
                var newCompilation = projectCompilation.AddSyntaxTrees(newDbContextTree).WithAssemblyName(newAssemblyName);

                var result = CommonUtilities.GetAssemblyFromCompilation(_loader, newCompilation);
                if (result.Success)
                {
                    dbContextType = result.Assembly.GetType(dbContextTypeName);
                    if (dbContextType == null)
                    {
                        throw new InvalidOperationException("There was an error creating a DbContext, there was no type returned after compiling the new assembly successfully");
                    }
                }
                else
                {
                    throw new InvalidOperationException("There was an error creating a DbContext :" + string.Join("\n", result.ErrorMessages));
                }
            }
            else
            {
                dbContextType = _libraryExporter.GetReflectionType(_libraryManager, _environment, dbContextTypeName);

                if (dbContextType == null)
                {
                    throw new InvalidOperationException("Could not get the reflection type for DbContext : " + dbContextTypeName);
                }
            }

            var modelTypeName = modelTypeSymbol.FullName;
            var modelType = _libraryExporter.GetReflectionType(_libraryManager, _environment, modelTypeName);

            if (modelType == null)
            {
                throw new InvalidOperationException("Could not get the reflection type for Model : " + modelTypeName);
            }

            _logger.LogMessage("Attempting to figure out the EntityFramework metadata for the model and DbContext");
            var metadata = GetModelMetadata(dbContextType, modelType);

            // Write the DbContext if getting the model metadata is successful
            if (isNewDbContext)
            {
                await WriteDbContext(dbContextTemplateModel, newDbContextTree);
            }

            return metadata;
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

                throw new InvalidOperationException("Scaffolding should be run again since it needs to reload the application with the added package reference - just run the previous command one more time.");
            }
        }

        private async Task WriteDbContext(NewDbContextTemplateModel dbContextTemplateModel,
            SyntaxTree newDbContextTree)
        {
            //ToDo: What's the best place to write the DbContext?
            var appBasePath = _environment.ApplicationBasePath;
            var outputPath = Path.Combine(
                appBasePath,
                "Models",
                dbContextTemplateModel.DbContextTypeName + ".cs");

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

            var sourceText = await newDbContextTree.GetTextAsync();

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            using (var fileStream = new FileStream(outputPath, FileMode.CreateNew, FileAccess.Write))
            {
                using (var streamWriter = new StreamWriter(stream: fileStream, encoding: Encoding.UTF8))
                {
                    sourceText.Write(streamWriter);
                }
            }
            _logger.LogMessage("Added DbContext : " + outputPath.Substring(appBasePath.Length));
        }

        private ModelMetadata GetModelMetadata([NotNull]Type dbContextType, [NotNull]Type modelType)
        {
            DbContext dbContextInstance;
            try
            {
                dbContextInstance = Activator.CreateInstance(dbContextType) as DbContext;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("There was an error creating the DbContext instance to get the model: " + ex);
            }

            if (dbContextInstance == null)
            {
                throw new InvalidOperationException(string.Format(
                    "Instance of type {0} could not be cast to DbContext",
                    dbContextType.FullName));
            }

            var entityType = dbContextInstance.Model.FindEntityType(modelType);
            if (entityType == null)
            {
                throw new InvalidOperationException(string.Format(
                    "There is no entity type {0} on DbContext {1}",
                    modelType.FullName,
                    dbContextType.FullName));
            }

            return new ModelMetadata(entityType, dbContextType);
        }
    }
}