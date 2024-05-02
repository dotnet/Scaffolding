// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.Scaffolding.Helpers.Roslyn;

public static class EfDbContextHelpers
{
    public static EfDbContextProperties GetEfDbContextProperties(ISymbol dbContextSymbol, ISymbol modelSymbol)
    {
        EfDbContextProperties efDbContextProperties = new();
        if (dbContextSymbol is INamedTypeSymbol dbContextTypeSymbol)
        {
            // Assuming there's only one DbSet property in the DbContext for simplicity
            var dbSetProperties = dbContextTypeSymbol.GetMembers().OfType<IPropertySymbol>()
                            .Where(p => p.Type is INamedTypeSymbol namedTypeSymbol &&
                                        namedTypeSymbol.Interfaces.Any(i => i.ToDisplayString() == "Microsoft.EntityFrameworkCore.DbSet"));

            if (dbSetProperties.Any())
            {
                var dbSetProperty = (INamedTypeSymbol)dbSetProperties.First();
                var entityType = (INamedTypeSymbol)dbSetProperties.First().Type;
                var primaryKey = entityType.GetMembers().OfType<IPropertySymbol>().FirstOrDefault(IsKeyProperty);
                if (primaryKey != null)
                {
                    efDbContextProperties.PrimaryKeyName = primaryKey.Name;
                    efDbContextProperties.PrimaryKeyShortTypeName = primaryKey.Type.Name;
                    efDbContextProperties.PrimaryKeyTypeName = primaryKey.Type.ToDisplayString();
                    efDbContextProperties.EntitySetName = $"{dbSetProperty.Name}";
                }
            }

            efDbContextProperties.ModelProperties = GetModelProperties(modelSymbol);
        }

        return efDbContextProperties;
    }

    private static List<IPropertySymbol> GetModelProperties(ISymbol modelSymbol)
    {
        List<IPropertySymbol> properties = [];

        if (modelSymbol is INamedTypeSymbol namedTypeSymbol)
        {
            properties.AddRange(namedTypeSymbol.GetMembers().OfType<IPropertySymbol>());
        }

        return properties;
    }

    private static bool IsKeyProperty(IPropertySymbol propertySymbol)
    {
        return propertySymbol.GetAttributes().Any(a => a.AttributeClass?.Name == "KeyAttribute");
    }
}

public class EfDbContextProperties
{
    public List<IPropertySymbol> ModelProperties { get; set; } = default!;
    public string PrimaryKeyName { get; set; } = default!;
    public string PrimaryKeyShortTypeName { get; set; } = default!;
    public string PrimaryKeyTypeName { get; set; } = default!;
    public string EntitySetName { get; set; } = default!;
}
