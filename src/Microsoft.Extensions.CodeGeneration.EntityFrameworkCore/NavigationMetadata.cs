using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Microsoft.Extensions.CodeGeneration.EntityFrameworkCore
{
    public class NavigationMetadata
    {
        public NavigationMetadata(INavigation navigation, Type dbContextType)
        {
            Contract.Assert(navigation != null);
            Contract.Assert(navigation.IsDependentToPrincipal());

            AssociationPropertyName = navigation.Name;
            DisplayPropertyName = AssociationPropertyName; //Needs further implementation

            var otherEntityType = navigation.ForeignKey.ResolveOtherEntityType(navigation.DeclaringEntityType);
            EntitySetName = ModelMetadata.GetEntitySetName(dbContextType, otherEntityType.ClrType);
            TypeName = otherEntityType.ClrType.GetTypeInfo().FullName;
            ShortTypeName = otherEntityType.ClrType.GetTypeInfo().Name;
            PrimaryKeyNames = navigation.ForeignKey.PrincipalKey.Properties.Select(pk => pk.Name).ToArray();
            ForeignKeyPropertyNames = navigation.ForeignKey.Properties
                .Where(p => p.DeclaringEntityType == navigation.DeclaringEntityType)
                .Select(p => p.Name)
                .ToArray();
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
