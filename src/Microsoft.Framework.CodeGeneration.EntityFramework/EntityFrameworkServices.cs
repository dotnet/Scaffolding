// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.Data.Entity;
using Microsoft.Framework.Runtime;

namespace Microsoft.Framework.CodeGeneration.EntityFramework
{
    public class EntityFrameworkServices : IEntityFrameworkService
    {
        private readonly IDbContextEditorServices _dbContextEditorServices;
        private readonly IApplicationEnvironment _environment;
        private readonly ILibraryManager _libraryManager;
        private readonly IAssemblyLoaderEngine _loader;
        private readonly IModelTypesLocator _modelTypesLocator;

        public EntityFrameworkServices(
            [NotNull]ILibraryManager libraryManager,
            [NotNull]IApplicationEnvironment environment,
            [NotNull]IAssemblyLoaderEngine loader,
            [NotNull]IModelTypesLocator modelTypesLocator,
            [NotNull]IDbContextEditorServices dbContextEditorServices)
        {
            _libraryManager = libraryManager;
            _environment = environment;
            _loader = loader;
            _modelTypesLocator = modelTypesLocator;
            _dbContextEditorServices = dbContextEditorServices;
        }

        public async Task<ModelMetadata> GetModelMetadata(string dbContextTypeName, ITypeSymbol modelTypeSymbol)
        {
            Type dbContextType;
            var dbContextSymbols = _modelTypesLocator.GetType(dbContextTypeName).ToList();

            if (dbContextSymbols.Count == 0)
            {
                var newCompilation = await _dbContextEditorServices
                    .AddNewContext(dbContextTypeName, modelTypeSymbol);

                Assembly newAssembly;
                IEnumerable<string> errorMessages;
                if (CommonUtil.TryGetAssemblyFromCompilation(_loader, newCompilation, out newAssembly, out errorMessages))
                {
                    dbContextType = newAssembly.GetType(dbContextTypeName);
                    if (dbContextType == null)
                    {
                        throw new Exception("There was an error creating a DbContext, there was no type returned after compiling the new assembly successfully");
                    }
                }
                else
                {
                    throw new Exception("There was an error creating a DbContext :" + string.Join("\n", errorMessages));
                }
            }
            else
            {
                dbContextType = _libraryManager.GetReflectionType(_environment, dbContextTypeName);

                if (dbContextType == null)
                {
                    throw new Exception("Could not get the reflection type for DbContext : " + dbContextTypeName);
                }
            }

            var modelTypeName = modelTypeSymbol.FullNameForSymbol();
            var modelType = _libraryManager.GetReflectionType(_environment, modelTypeName);

            if (modelType == null)
            {
                throw new Exception("Could not get the reflection type for Model : " + modelTypeName);
            }

            return GetModelMetadata(dbContextType, modelType);
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
                throw new Exception("There was an error creating the DbContext instance to get the model: " + ex);
            }

            if (dbContextInstance == null)
            {
                throw new Exception(string.Format(
                    "Instance of type {0} could not be cast to DbContext",
                    dbContextType.FullName));
            }

            var entityType = dbContextInstance.Model.GetEntityType(modelType);
            if (entityType == null)
            {
                throw new Exception(string.Format(
                    "There is no entity type {0} on DbContext {1}",
                    modelType.FullName,
                    dbContextType.FullName));
            }

            return new ModelMetadata(entityType);
        }
    }
}