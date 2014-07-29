// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Metadata;

namespace Microsoft.Framework.CodeGeneration.EntityFramework
{
    // ToDo: This takes depedency on EF, that will conflict with
    // app's EF dependency
    public class ModelMetadata
    {
        private PropertyMetadata[] _properties;
        private PropertyMetadata[] _primaryKeys;


        //Todo: Perhaps move the constructor to something line MetadataReader?
        public ModelMetadata([NotNull]IEntityType entityType, [NotNull]Type dbContextType)
        {
            EntityType = entityType;
            DbContexType = dbContextType;
            EntitySetName = GetEntitySetName(DbContexType, EntityType.Type);
        }

        public IEntityType EntityType { get; private set; }

        public Type DbContexType { get; private set; }

        public string EntitySetName { get; private set; }

        public PropertyMetadata[] Properties
        {
            get
            {
                if (_properties == null)
                {
                    _properties = EntityType.Properties
                        .Select(p => new PropertyMetadata(EntityType, p))
                        .ToArray();
                }
                return _properties;
            }
        }

        public PropertyMetadata[] PrimaryKeys
        {
            get
            {
                if (_primaryKeys == null)
                {
                    _primaryKeys = EntityType.GetKey()
                        .Properties
                        .Select(p => new PropertyMetadata(EntityType, p))
                        .ToArray();
                }
                return _primaryKeys;
            }
        }

        private string GetEntitySetName([NotNull]Type dbContextType, [NotNull]Type modelType)
        {
            Type dbSetType = typeof(DbSet<>).MakeGenericType(modelType);

            var prop = dbContextType.GetRuntimeProperties()
                .Where(pi => pi.PropertyType == dbSetType)
                .FirstOrDefault();

            if (prop != null)
            {
                return prop.Name;
            }
            else
            {
                //Fallback to this or throw?
                return "DbSet<" + modelType.Name + ">";
            }
        }
    }
}