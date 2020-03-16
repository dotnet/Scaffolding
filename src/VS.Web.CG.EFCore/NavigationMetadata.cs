// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore
{
    public class NavigationMetadata : INavigationMetadata
    {
        public NavigationMetadata(INavigation navigation, Type dbContextType)
        {
            Contract.Assert(navigation != null);
            Contract.Assert(navigation.IsOnDependent);

            AssociationPropertyName = navigation.Name;

            IEntityType otherEntityType = navigation.TargetEntityType;

            EntitySetName = ModelMetadata.GetEntitySetName(dbContextType, otherEntityType.ClrType);
            TypeName = otherEntityType.ClrType.GetTypeInfo().FullName;
            ShortTypeName = otherEntityType.ClrType.GetTypeInfo().Name;
            PrimaryKeyNames = navigation.ForeignKey.PrincipalKey.Properties.Select(pk => pk.Name).ToArray();
            ForeignKeyPropertyNames = navigation.ForeignKey.Properties
                .Where(p => p.DeclaringEntityType == navigation.DeclaringEntityType)
                .Select(p => p.Name)
                .ToArray();

            // The default for the display property is the primary key of the navigation. 
            DisplayPropertyName = PrimaryKeyNames[0];

            // If there is a non nullable string property in the navigation's target type, we use that instead. 
            var displayPropertyCandidate = navigation
                .TargetEntityType
                .GetProperties()
                .FirstOrDefault(p => !p.IsNullable && p.ClrType == typeof(string));

            if (displayPropertyCandidate != null)
            {
                DisplayPropertyName = displayPropertyCandidate.Name;
            }
        }

        public string AssociationPropertyName { get; set; }

        public string DisplayPropertyName { get; set; }

        public string EntitySetName { get; set; }

        public string[] ForeignKeyPropertyNames { get; set; }

        public string[] PrimaryKeyNames { get; set; }

        public string ShortTypeName { get; set; }

        public string TypeName { get; set; }
    }
}
