// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.Scaffolding.Helpers.General;

namespace Microsoft.DotNet.Scaffolding.Helpers.Roslyn;

internal static class EfDbContextHelpers
{
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
                    PrimaryKeyShortTypeName = primaryKey.Type.Name,
                    PrimaryKeyTypeName = primaryKey.Type.ToDisplayString(),
                    AllModelProperties = allModelProperties.ToList()
                };

                return efModelProperties;
            }
        }

        return null;
    }

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

    internal static string? GetEntitySetVariableName(ISymbol dbContextSymbol, string modelTypeName)
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
