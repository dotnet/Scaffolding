// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics.Contracts;
using System.Linq;
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
        public ModelMetadata([NotNull]IEntityType entityType)
        {
            EntityType = entityType;
        }

        public IEntityType EntityType { get; private set; }

        // This should use reflection to get the actual property name
        // on DbContext.
        public string DbSetName
        {
            get
            {
                Contract.Assert(EntityType != null);
                return "Set<" + EntityType.Name + ">()";
            }
        }

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
    }
}