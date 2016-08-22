// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore
{
    // ToDo: This takes depedency on EF, that will conflict with
    // app's EF dependency
    public class ModelMetadata : IModelMetadata
    {
        private PropertyMetadata[] _properties;
        private PropertyMetadata[] _primaryKeys;
        private NavigationMetadata[] _navigations;

        //Todo: Perhaps move the constructor to something line MetadataReader?
        public ModelMetadata(IEntityType entityType, Type dbContextType)
        {
            if (entityType == null)
            {
                throw new ArgumentNullException(nameof(entityType));
            }

            if (dbContextType == null)
            {
                throw new ArgumentNullException(nameof(dbContextType));
            }

            EntityType = entityType;
            DbContexType = dbContextType;
            EntitySetName = GetEntitySetName(DbContexType, EntityType.ClrType);
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
                    _properties = GetSortedProperties(EntityType);
                }
                return _properties;
            }
        }

        /// <summary>
        /// Sort properties according to reflection order.
        /// </summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        private PropertyMetadata[] GetSortedProperties(IEntityType entityType)
        {
            if(entityType == null)
            {
                throw new ArgumentNullException(nameof(entityType));
            }

            var properties = new Dictionary<string, PropertyMetadata>();
            var entityProperties = entityType.GetProperties()
                .Where(p => !p.IsShadowProperty)
                .Select(p => new PropertyMetadata(p, DbContexType));

            foreach(var p in entityProperties)
            {
                properties.Add(p.PropertyName, p);
            }

            var reflectedProperties = entityType.ClrType.GetProperties();
            var sortedProperties = new PropertyMetadata[entityProperties.Count()];
            int i = 0;
            foreach(var r in reflectedProperties)
            {
                if(properties.ContainsKey(r.Name))
                {
                    sortedProperties[i++] = properties[r.Name];
                }
            }

            return sortedProperties;
        }

        public PropertyMetadata[] PrimaryKeys
        {
            get
            {
                if (_primaryKeys == null)
                {
                    var primaryKey = EntityType.FindPrimaryKey();
                    if (primaryKey == null)
                    {
                        throw new InvalidOperationException("Primary key not found");
                    }

                    _primaryKeys = primaryKey
                        .Properties
                        .Select(p => new PropertyMetadata(p, DbContexType))
                        .ToArray();
                }
                return _primaryKeys;
            }
        }

        /// <summary>
        /// Only navigations that are dependent and has all properties defined
        /// in code (non-shadow properties) are returned as part of this.
        /// Typically this is used to create code for drop down lists
        /// to choose values from principal entity.
        /// </summary>
        public NavigationMetadata[] Navigations
        {
            get
            {
                if (_navigations == null)
                {
                    _navigations = EntityType.GetNavigations()
                        .Where(n => n.IsDependentToPrincipal() == true && n.ForeignKey.Properties.All(p => !p.IsShadowProperty))
                        .Select(n => new NavigationMetadata(n, DbContexType))
                        .ToArray();
                }
                return _navigations;
            }
        }

        internal static string GetEntitySetName(Type dbContextType, Type modelType)
        {
            if (dbContextType == null)
            {
                throw new ArgumentNullException(nameof(dbContextType));
            }

            if (modelType == null)
            {
                throw new ArgumentNullException(nameof(modelType));
            }

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
                return "Set<" + modelType.Name + ">()";
            }
        }
    }
}