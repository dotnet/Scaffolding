// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using Microsoft.Data.Entity;
using Microsoft.Framework.Runtime;

namespace Microsoft.Framework.CodeGeneration.EntityFramework
{
    public class EntityFrameworkServices : IEntityFrameworkService
    {
        private readonly IApplicationEnvironment _environment;
        private readonly ILibraryManager _libraryManager;

        public EntityFrameworkServices(
            [NotNull]ILibraryManager libraryManager,
            [NotNull]IApplicationEnvironment environment)
        {
            _libraryManager = libraryManager;
            _environment = environment;
        }

        public ModelMetadata GetModelMetadata(string dbContextTypeName, string modelTypeName)
        {
            var dbContextType = _libraryManager.GetReflectionType(_environment, dbContextTypeName);

            if (dbContextType == null)
            {
                throw new Exception("Could not get the reflection type for DbContext : " + dbContextTypeName);
            }

            var modelType = _libraryManager.GetReflectionType(_environment, modelTypeName);

            if (modelType == null)
            {
                throw new Exception("Could not get the reflection type for Model : " + modelTypeName);
            }

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
                    dbContextTypeName));
            }

            var entityType = dbContextInstance.Model.GetEntityType(modelType);
            if (entityType == null)
            {
                throw new Exception(string.Format(
                    "There is no entity type {0} on DbContext {1}",
                    modelTypeName,
                    dbContextTypeName));
            }

            return new ModelMetadata(entityType);
        }
    }
}