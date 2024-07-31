// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.Scaffolding.CodeModification.Helpers;

public static class EfDbContextHelpers
{
    /// <summary>
    /// Given a model class' ISymbol, initialize an EfModelProperties object.
    /// </summary>
    public static EfModelProperties? GetModelProperties(ISymbol modelSymbol)
    {
        if (modelSymbol is INamedTypeSymbol namedTypeSymbol)
        {
            var allModelProperties = namedTypeSymbol.GetMembers().OfType<IPropertySymbol>();
            var primaryKey = namedTypeSymbol?.GetMembers().OfType<IPropertySymbol>().FirstOrDefault(IsPrimaryKey);
            if (primaryKey != null)
            {
                EfModelProperties efModelProperties = new()
                {
                    PrimaryKeyName = primaryKey.Name,
                    //using the same type name for both short and long type name.
                    //unwanted values for base types ('Int32' instead of 'int')
                    PrimaryKeyShortTypeName = primaryKey.Type.ToDisplayString(),
                    PrimaryKeyTypeName = primaryKey.Type.ToDisplayString(),
                    AllModelProperties = allModelProperties.ToList()
                };

                return efModelProperties;
            }
        }

        return null;
    }

    /// <summary>
    /// Following the official EF Core (https://learn.microsoft.com/en-us/ef/core/modeling/keys?tabs=data-annotations) guide on assigning primary keys to extract primary key info
    /// TODO : account for [PrimaryLKey] attribute and composite keys.
    /// </summary>
    private static bool IsPrimaryKey(IPropertySymbol propertySymbol)
    {
        return HasPrimaryKeyAttribute(propertySymbol) ||
            IsPrimaryKeyByConvention(propertySymbol);
    }

    public static bool IsPrimaryKeyByConvention(IPropertySymbol propertySymbol)
    {
        var propertyName = propertySymbol.Name;
        var containingTypeName = propertySymbol.ContainingType.Name;

        return propertyName.Equals("Id", StringComparison.OrdinalIgnoreCase) || propertyName.Equals($"{containingTypeName}Id", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasPrimaryKeyAttribute(IPropertySymbol propertySymbol)
    {
        return propertySymbol.GetAttributes().Any(a => a.AttributeClass is not null && a.AttributeClass.Name.Equals("KeyAttribute"));
    }

    /// <summary>
    /// check for the specific DbSet variable in a given DbContext's ISymbol.
    /// return the DbSet property's name.
    /// </summary>
    public static string? GetEntitySetVariableName(ISymbol dbContextSymbol, string modelTypeName)
    {
        if (dbContextSymbol is INamedTypeSymbol dbContextTypeSymbol)
        {
            string dbSetType = $"Microsoft.EntityFrameworkCore.DbSet<{modelTypeName}>";
            //get the DbSet pertaining our given modelSymbol
            var dbSetProperty = dbContextTypeSymbol.GetMembers().OfType<IPropertySymbol>().FirstOrDefault(p =>
                p.Type is INamedTypeSymbol &&
                p.Type.ToDisplayString().Equals(dbSetType));

            if (dbSetProperty != null)
            {
                return dbSetProperty.Name;
            }
        }

        return null;
    }
}
