// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.DotNet.Scaffolding.CodeModification.Helpers;
using Microsoft.DotNet.Scaffolding.Roslyn.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Common;

/// <summary>
/// Provides static methods for analyzing classes related to Entity Framework DbContext and model classes.
/// </summary>
internal static class ClassAnalyzers
{
    /// <summary>
    /// Gets information about a DbContext, either existing or to be created, including its name, path, namespace, and entity set variable.
    /// </summary>
    /// <param name="projectPath">The path to the project.</param>
    /// <param name="existingDbContextClass">The symbol representing an existing DbContext class, or null if creating a new one.</param>
    /// <param name="dbContextClassName">The name of the DbContext class to use or create.</param>
    /// <param name="dbProvider">The database provider (e.g., SQL Server, SQLite).</param>
    /// <param name="modelInfo">Optional model information for generating entity set variable or DbSet statement.</param>
    /// <returns>A <see cref="DbContextInfo"/> object with details about the DbContext.</returns>
    internal static DbContextInfo GetDbContextInfo(
        string projectPath,
        ISymbol? existingDbContextClass,
        string dbContextClassName,
        string dbProvider,
        ModelInfo? modelInfo = null)
    {
        var dbContextInfo = new DbContextInfo();
        dbContextInfo.EfScenario = true;
        dbContextInfo.DatabaseProvider = dbProvider;
        if (existingDbContextClass is not null)
        {
            dbContextInfo.DbContextClassName = existingDbContextClass.Name;
            dbContextInfo.DbContextClassPath = existingDbContextClass.Locations.FirstOrDefault()?.SourceTree?.FilePath;
            dbContextInfo.DbContextNamespace = existingDbContextClass.ContainingNamespace.ToDisplayString();
            var discoveredEntitySetName = modelInfo is null
                ? string.Empty
                : EfDbContextHelpers.GetEntitySetVariableName(existingDbContextClass, modelInfo.ModelTypeName, modelInfo.ModelFullName) ?? string.Empty;

            // If the DbSet for this model doesn't exist in the existing context yet, prepare the statement to add it
            // and use the model type name as the controller's entity set reference.
            if (string.IsNullOrWhiteSpace(discoveredEntitySetName) && modelInfo is not null)
            {
                dbContextInfo.NewDbSetStatement = $"public DbSet<{modelInfo.ModelFullName}> {modelInfo.ModelTypeName} {{ get; set; }} = default!;";
                dbContextInfo.EntitySetVariableName = modelInfo.ModelTypeName;
            }
            else
            {
                dbContextInfo.EntitySetVariableName = discoveredEntitySetName;
            }
        }
        //properties for creating a new DbContext
        else
        {
            dbContextInfo.NewDbSetStatement = modelInfo is null ?
                string.Empty : $"public DbSet<{modelInfo.ModelFullName}> {modelInfo.ModelTypeName} {{ get; set; }} = default!;";
            dbContextInfo.DbContextClassName = dbContextClassName;
            dbContextInfo.DbContextClassPath = AspNetDbContextHelper.GetIdentityDataContextPath(projectPath, dbContextClassName);
            dbContextInfo.DatabaseProvider = dbProvider;
            dbContextInfo.EntitySetVariableName = modelInfo?.ModelTypeName;
        }

        if (!string.IsNullOrEmpty(dbContextInfo.DbContextNamespace) &&
            dbContextInfo.DbContextNamespace.Equals(Constants.GlobalNamespace, StringComparison.OrdinalIgnoreCase))
        {
            dbContextInfo.DbContextNamespace = string.Empty;
        }

        return dbContextInfo;
    }

    /// <summary>
    /// Gets information about an Identity DbContext, either existing or to be created, including its name, path, and namespace.
    /// </summary>
    /// <param name="projectPath">The path to the project.</param>
    /// <param name="existingDbContextClass">The symbol representing an existing Identity DbContext class, or null if creating a new one.</param>
    /// <param name="dbContextClassName">The name of the Identity DbContext class to use or create.</param>
    /// <param name="dbProvider">The database provider (e.g., SQL Server, SQLite).</param>
    /// <returns>A <see cref="DbContextInfo"/> object with details about the Identity DbContext.</returns>
    internal static DbContextInfo GetIdentityDbContextInfo(
        string projectPath,
        ISymbol? existingDbContextClass,
        string dbContextClassName,
        string dbProvider)
    {
        DbContextInfo dbContextInfo = new()
        {
            EfScenario = true,
            DatabaseProvider = dbProvider
        };

        if (existingDbContextClass is not null)
        {
            dbContextInfo.DbContextClassName = existingDbContextClass.Name;
            dbContextInfo.DbContextClassPath = existingDbContextClass.Locations.FirstOrDefault()?.SourceTree?.FilePath;
            dbContextInfo.DbContextNamespace = existingDbContextClass.ContainingNamespace.ToDisplayString();
            dbContextInfo.EntitySetVariableName = string.Empty;
        }
        //properties for creating a new DbContext
        else
        {
            dbContextInfo.NewDbSetStatement = string.Empty;
            dbContextInfo.DbContextClassName = dbContextClassName;
            dbContextInfo.DbContextClassPath = AspNetDbContextHelper.GetIdentityDataContextPath(projectPath, dbContextClassName);
            dbContextInfo.DbContextNamespace = $"{Path.GetFileNameWithoutExtension(projectPath)}.Data";
            dbContextInfo.DatabaseProvider = dbProvider;
            dbContextInfo.EntitySetVariableName = string.Empty;
        }

        //check for '<global namespace>' and remove it (for classes that don't define a namespace)
        if (!string.IsNullOrEmpty(dbContextInfo.DbContextNamespace) &&
            dbContextInfo.DbContextNamespace.Equals(Constants.GlobalNamespace, StringComparison.OrdinalIgnoreCase))
        {
            dbContextInfo.DbContextNamespace = string.Empty;
        }

        return dbContextInfo;
    }

    /// <summary>
    /// Gets information about a model class, including its name, namespace, full name, primary key, and properties.
    /// </summary>
    /// <param name="modelClassSymbol">The symbol representing the model class.</param>
    /// <returns>A <see cref="ModelInfo"/> object with details about the model class.</returns>
    internal static ModelInfo GetModelClassInfo(ISymbol modelClassSymbol)
    {
        var modelInfo = new ModelInfo();
        modelInfo.ModelTypeName = modelClassSymbol.Name;
        modelInfo.ModelNamespace = modelClassSymbol.ContainingNamespace.ToDisplayString();
        if (!string.IsNullOrEmpty(modelInfo.ModelNamespace))
        {
            if (modelInfo.ModelNamespace.Equals(Constants.GlobalNamespace, StringComparison.OrdinalIgnoreCase))
            {
                modelInfo.ModelNamespace = string.Empty;
                modelInfo.ModelFullName = modelInfo.ModelTypeName;
            }
            else
            {
                modelInfo.ModelFullName = $"{modelInfo.ModelNamespace}.{modelInfo.ModelTypeName}";
            }
        }
        else
        {
            modelInfo.ModelFullName = modelInfo.ModelTypeName;
        }

        var efModelProperties = EfDbContextHelpers.GetModelProperties(modelClassSymbol);
        if (efModelProperties != null)
        {
            modelInfo.PrimaryKeyName = efModelProperties.PrimaryKeyName;
            modelInfo.PrimaryKeyShortTypeName = efModelProperties.PrimaryKeyShortTypeName;
            modelInfo.PrimaryKeyTypeName = efModelProperties.PrimaryKeyTypeName;
            modelInfo.ModelProperties = efModelProperties.AllModelProperties;
        }

        return modelInfo;
    }

    /// <summary>
    /// Finds reference navigations on the model that are paired with a single scalar foreign key property,
    /// using [ForeignKey] attributes or EF Core naming conventions.
    /// </summary>
    /// <param name="modelInfo">The model information whose properties are analyzed.</param>
    /// <param name="dbContextSymbol">The existing DbContext symbol used to resolve principal entity sets, or null.</param>
    /// <returns>The foreign key navigations found on the model.</returns>
    internal static List<ForeignKeyNavigationInfo> GetForeignKeyNavigations(ModelInfo modelInfo, ISymbol? dbContextSymbol)
    {
        var navigations = new List<ForeignKeyNavigationInfo>();
        var modelProperties = modelInfo.ModelProperties;
        if (modelProperties is null)
        {
            return navigations;
        }

        foreach (var navigationProperty in modelProperties)
        {
            // Reference navigations only; strings, value types and collections are skipped.
            if (navigationProperty.Type is not INamedTypeSymbol principalType ||
                principalType.TypeKind != TypeKind.Class ||
                principalType.SpecialType != SpecialType.None ||
                principalType.AllInterfaces.Any(i => i.SpecialType == SpecialType.System_Collections_IEnumerable) ||
                HasAttribute(navigationProperty, "NotMappedAttribute") ||
                principalType.GetAttributes().Any(a => a.AttributeClass?.Name is "NotMappedAttribute" or "OwnedAttribute"))
            {
                continue;
            }

            // A readable fluent HasKey overrides attribute/convention keys. Composite or conflicting
            // fluent keys, multiple [Key] properties and class-level [PrimaryKey] are not supported.
            var principalProperties = new List<IPropertySymbol>();
            for (INamedTypeSymbol? type = principalType; type is not null && type.SpecialType != SpecialType.System_Object; type = type.BaseType)
            {
                principalProperties.AddRange(type.GetMembers().OfType<IPropertySymbol>());
            }

            var keyAttributeProperties = principalProperties.Where(p => HasAttribute(p, "KeyAttribute")).ToList();
            if (keyAttributeProperties.Count > 1 ||
                principalType.GetAttributes().Any(a => a.AttributeClass?.Name == "PrimaryKeyAttribute"))
            {
                continue;
            }

            var principalKeyProperty = keyAttributeProperties.SingleOrDefault() ??
                principalProperties.FirstOrDefault(p =>
                    !p.IsStatic && string.Equals(p.Name, "Id", StringComparison.OrdinalIgnoreCase)) ??
                principalProperties.FirstOrDefault(p =>
                    !p.IsStatic && string.Equals(p.Name, $"{principalType.Name}Id", StringComparison.OrdinalIgnoreCase));
            var configurationMethods = GetModelConfigurationMethods(dbContextSymbol, navigationProperty.ContainingType, principalType).ToList();
            var configuredKeys = configurationMethods
                .SelectMany(method => method.Syntax.DescendantNodes().OfType<InvocationExpressionSyntax>()
                    .Where(invocation => GetInvocationName(invocation)?.Identifier.ValueText == "HasKey")
                    .Select(invocation => (Invocation: invocation, Entity: GetChainEntityType(invocation, method.Symbol, navigationProperty.ContainingType, principalType))))
                .ToList();
            if (configuredKeys.Any(key => key.Entity is null))
            {
                continue;
            }

            var principalKeys = configuredKeys.Where(key => SymbolEqualityComparer.Default.Equals(key.Entity, principalType)).ToList();
            if (principalKeys.Count > 0)
            {
                if (principalKeys.Count != 1)
                {
                    continue;
                }

                string? configuredKeyName = GetSingleForeignKeyName(principalKeys[0].Invocation);
                principalKeyProperty = principalProperties.FirstOrDefault(p =>
                    !p.IsStatic && string.Equals(p.Name, configuredKeyName, StringComparison.Ordinal));
            }

            if (principalKeyProperty is null)
            {
                continue;
            }

            string principalKeyName = principalKeyProperty.Name;
            var foreignKeyProperty = FindForeignKeyProperty(navigationProperty, principalType, principalKeyName, modelProperties);
            // Skip FKs that cannot target the principal primary key (e.g. alternate keys); int? matches int.
            if (foreignKeyProperty is null ||
                !SymbolEqualityComparer.Default.Equals(GetNonNullableType(foreignKeyProperty.Type), GetNonNullableType(principalKeyProperty.Type)) ||
                string.Equals(foreignKeyProperty.Name, modelInfo.PrimaryKeyName, StringComparison.OrdinalIgnoreCase) ||
                navigations.Any(n => string.Equals(n.ForeignKeyPropertyName, foreignKeyProperty.Name, StringComparison.Ordinal)) ||
                HasBlockingRelationshipConfiguration(configurationMethods, principalType, navigationProperty, foreignKeyProperty.Name))
            {
                continue;
            }

            // The generated controller reads the DbSet through _context, so it must be an accessible instance property.
            string? entitySetName = (dbContextSymbol as INamedTypeSymbol)?.GetMembers().OfType<IPropertySymbol>().FirstOrDefault(p =>
                !p.IsStatic &&
                p.GetMethod?.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal &&
                p.Type is INamedTypeSymbol { Name: "DbSet", TypeArguments.Length: 1 } dbSetType &&
                SymbolEqualityComparer.Default.Equals(dbSetType.TypeArguments[0], principalType))?.Name;

            navigations.Add(new ForeignKeyNavigationInfo
            {
                NavigationPropertyName = navigationProperty.Name,
                ForeignKeyPropertyName = foreignKeyProperty.Name,
                PrincipalPrimaryKeyName = principalKeyName,
                DisplayPropertyName = GetDisplayPropertyName(principalProperties, principalKeyName) ?? principalKeyName,
                EntitySetName = string.IsNullOrEmpty(entitySetName)
                    ? $"Set<{principalType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>()"
                    : entitySetName,
                // Optional only when both the FK and the navigation allow null and neither is [Required].
                IsNullable = (foreignKeyProperty.Type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T ||
                    (foreignKeyProperty.Type.IsReferenceType && foreignKeyProperty.Type.NullableAnnotation != NullableAnnotation.NotAnnotated)) &&
                    !HasAttribute(foreignKeyProperty, "RequiredAttribute") &&
                    !HasAttribute(navigationProperty, "RequiredAttribute") &&
                    navigationProperty.NullableAnnotation != NullableAnnotation.NotAnnotated
            });
        }

        return navigations;
    }

    private static IPropertySymbol? FindForeignKeyProperty(
        IPropertySymbol navigationProperty,
        INamedTypeSymbol principalType,
        string principalKeyName,
        List<IPropertySymbol> modelProperties)
    {
        List<string> candidateNames;
        var navigationForeignKeyName = GetForeignKeyAttributeName(navigationProperty);
        if (navigationForeignKeyName is not null)
        {
            // Composite foreign keys ("A,B") are not supported.
            if (navigationForeignKeyName.Contains(','))
            {
                return null;
            }

            candidateNames = [navigationForeignKeyName];
        }
        else
        {
            var attributedProperty = modelProperties.FirstOrDefault(p =>
                string.Equals(GetForeignKeyAttributeName(p), navigationProperty.Name, StringComparison.OrdinalIgnoreCase));
            if (attributedProperty is not null)
            {
                return IsScalarProperty(attributedProperty) && !HasAttribute(attributedProperty, "NotMappedAttribute") ? attributedProperty : null;
            }

            candidateNames =
            [
                $"{navigationProperty.Name}{principalKeyName}",
                $"{navigationProperty.Name}Id",
                $"{principalType.Name}{principalKeyName}",
                $"{principalType.Name}Id"
            ];
        }

        foreach (var candidateName in candidateNames)
        {
            var match = modelProperties.FirstOrDefault(p =>
                !SymbolEqualityComparer.Default.Equals(p, navigationProperty) &&
                string.Equals(p.Name, candidateName, StringComparison.OrdinalIgnoreCase) &&
                IsScalarProperty(p) &&
                !HasAttribute(p, "NotMappedAttribute"));
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }

    /// <summary>
    /// Fail-safe syntax check for fluent configuration this analysis cannot model; true means "no dropdown".
    /// Sources: every method of every DbContext declaration (all partial parts), and the Configure methods of types in
    /// the dependent's assembly that implement IEntityTypeConfiguration&lt;T&gt; for the dependent or principal type.
    /// Each relationship call is matched through its receiver chain: HasOne on the dependent or WithOne on the
    /// principal names the navigation. Unresolved entities or missing relationship calls block; unreadable navigation
    /// names are treated as related. A related HasPrincipalKey or conflicting/unreadable HasForeignKey blocks.
    /// Ignore of the foreign key blocks in any unit; Ignore of the navigation or principal type blocks only on a
    /// matching entity.
    /// Principal HasKey overrides attribute/convention keys only for exactly one call naming an existing single property;
    /// unresolved entities, composite, unreadable, missing or conflicting keys suppress the dropdown.
    /// </summary>
    private static bool HasBlockingRelationshipConfiguration(IEnumerable<(MethodDeclarationSyntax Syntax, IMethodSymbol? Symbol)> methods, INamedTypeSymbol principalType, IPropertySymbol navigationProperty, string foreignKeyPropertyName)
    {
        var dependentType = navigationProperty.ContainingType;
        var ignoredNames = new HashSet<string>(StringComparer.Ordinal) { foreignKeyPropertyName, navigationProperty.Name, principalType.Name };

        foreach (var method in methods)
        {
            SyntaxNode? body = (SyntaxNode?)method.Syntax.Body ?? method.Syntax.ExpressionBody?.Expression;
            if (body is null)
            {
                continue;
            }

            var units = body is BlockSyntax block ? block.Statements.Cast<SyntaxNode>() : new SyntaxNode[] { body };
            foreach (var unit in units)
            {
                foreach (var invocation in unit.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>())
                {
                    SimpleNameSyntax? name = GetInvocationName(invocation);
                    if (name is null || name.Identifier.ValueText is not ("Ignore" or "HasForeignKey" or "HasPrincipalKey"))
                    {
                        continue;
                    }

                    var chainEntity = GetChainEntityType(invocation, method.Symbol, dependentType, principalType);
                    bool matchingEntity = chainEntity is not null;
                    if (name.Identifier.ValueText == "Ignore")
                    {
                        string? ignoredName = GetSingleForeignKeyName(invocation);
                        var ignoredTypes = name is GenericNameSyntax generic
                            ? generic.TypeArgumentList.Arguments.Select(a => a.DescendantNodesAndSelf().OfType<SimpleNameSyntax>().LastOrDefault()?.Identifier.ValueText ?? string.Empty)
                            : Enumerable.Empty<string>();
                        if (string.Equals(ignoredName, foreignKeyPropertyName, StringComparison.Ordinal) ||
                            ignoredTypes.Contains(foreignKeyPropertyName) ||
                            (matchingEntity && (ignoredNames.Contains(ignoredName ?? string.Empty) || ignoredTypes.Any(ignoredNames.Contains))))
                        {
                            return true;
                        }
                        continue;
                    }

                    if (chainEntity is null)
                    {
                        return true;
                    }

                    var chain = GetReceiverChain(invocation).ToList();
                    string relationshipName = SymbolEqualityComparer.Default.Equals(chainEntity, dependentType) ? "HasOne" : "WithOne";
                    var relationship = chain.FirstOrDefault(call => GetInvocationName(call)?.Identifier.ValueText == relationshipName);
                    if (relationship is null)
                    {
                        return true;
                    }

                    string? configuredNavigation = GetSingleForeignKeyName(relationship);
                    if (configuredNavigation is not null &&
                        !string.Equals(configuredNavigation, navigationProperty.Name, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (name.Identifier.ValueText == "HasPrincipalKey" ||
                        !string.Equals(GetSingleForeignKeyName(invocation), foreignKeyPropertyName, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private static SimpleNameSyntax? GetInvocationName(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression switch
        {
            MemberAccessExpressionSyntax member => member.Name,
            SimpleNameSyntax name => name,
            _ => null
        };
    }

    private static IEnumerable<InvocationExpressionSyntax> GetReceiverChain(InvocationExpressionSyntax invocation)
    {
        for (InvocationExpressionSyntax? current = invocation; current is not null;
            current = (current.Expression as MemberAccessExpressionSyntax)?.Expression as InvocationExpressionSyntax)
        {
            yield return current;
        }
    }

    private static INamedTypeSymbol? GetChainEntityType(InvocationExpressionSyntax invocation, IMethodSymbol? methodSymbol, INamedTypeSymbol dependentType, INamedTypeSymbol principalType)
    {
        var chain = GetReceiverChain(invocation).ToList();
        var entityName = chain.Select(GetInvocationName).OfType<GenericNameSyntax>()
            .FirstOrDefault(name => name.Identifier.ValueText == "Entity");
        if (entityName is not null)
        {
            var arguments = entityName.TypeArgumentList.Arguments;
            return arguments.Count == 1 ? MatchEntityType(arguments[0], dependentType, principalType) : null;
        }

        if (chain.Last().Expression is not MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax receiver })
        {
            return null;
        }

        foreach (var lambda in invocation.Ancestors().OfType<LambdaExpressionSyntax>())
        {
            bool matchingParameter = lambda switch
            {
                SimpleLambdaExpressionSyntax simple => simple.Parameter.Identifier.ValueText == receiver.Identifier.ValueText,
                ParenthesizedLambdaExpressionSyntax parenthesized => parenthesized.ParameterList.Parameters.Count == 1 &&
                    parenthesized.ParameterList.Parameters[0].Identifier.ValueText == receiver.Identifier.ValueText,
                _ => false
            };
            if (matchingParameter && lambda.Parent is ArgumentSyntax { Parent.Parent: InvocationExpressionSyntax owner } &&
                GetInvocationName(owner) is GenericNameSyntax { Identifier.ValueText: "Entity", TypeArgumentList.Arguments.Count: 1 } lambdaEntityName)
            {
                return MatchEntityType(lambdaEntityName.TypeArgumentList.Arguments[0], dependentType, principalType);
            }
        }

        var parameter = methodSymbol?.Parameters.FirstOrDefault(p => p.Name == receiver.Identifier.ValueText);
        if (parameter?.Type is INamedTypeSymbol { Name: "EntityTypeBuilder", TypeArguments.Length: 1 } builder)
        {
            return new[] { dependentType, principalType }.FirstOrDefault(type =>
                SymbolEqualityComparer.Default.Equals(type, builder.TypeArguments[0]));
        }

        return null;
    }

    private static INamedTypeSymbol? MatchEntityType(TypeSyntax? syntax, INamedTypeSymbol dependentType, INamedTypeSymbol principalType)
    {
        if (syntax is null)
        {
            return null;
        }

        string name = syntax.ToString();
        return new[] { dependentType, principalType }.FirstOrDefault(type =>
            name == type.Name || name == type.ToDisplayString() ||
            name == type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
    }

    private static IEnumerable<(MethodDeclarationSyntax Syntax, IMethodSymbol? Symbol)> GetModelConfigurationMethods(ISymbol? dbContextSymbol, INamedTypeSymbol dependentType, INamedTypeSymbol principalType)
    {
        if (dbContextSymbol is not null)
        {
            foreach (var method in GetDeclaredMethods(dbContextSymbol))
            {
                yield return method;
            }
        }

        if (dependentType.ContainingAssembly is null)
        {
            yield break;
        }

        foreach (var type in GetAllTypes(dependentType.ContainingAssembly.GlobalNamespace))
        {
            if (type.AllInterfaces.Any(i => i.Name == "IEntityTypeConfiguration" && i.TypeArguments.Length == 1 &&
                (SymbolEqualityComparer.Default.Equals(i.TypeArguments[0], dependentType) ||
                 SymbolEqualityComparer.Default.Equals(i.TypeArguments[0], principalType))))
            {
                foreach (var method in GetDeclaredMethods(type).Where(m => m.Syntax.Identifier.ValueText == "Configure"))
                {
                    yield return method;
                }
            }
        }
    }

    private static IEnumerable<(MethodDeclarationSyntax Syntax, IMethodSymbol? Symbol)> GetDeclaredMethods(ISymbol typeSymbol)
    {
        return typeSymbol.DeclaringSyntaxReferences
            .Select(r => r.GetSyntax())
            .OfType<TypeDeclarationSyntax>()
            .SelectMany(t => t.Members.OfType<MethodDeclarationSyntax>())
            .Select(method => (method, (typeSymbol as INamedTypeSymbol)?.GetMembers(method.Identifier.ValueText)
                .OfType<IMethodSymbol>().FirstOrDefault(symbol =>
                    symbol.DeclaringSyntaxReferences.Any(reference => reference.SyntaxTree == method.SyntaxTree && reference.Span == method.Span))));
    }

    private static IEnumerable<INamedTypeSymbol> GetAllTypes(INamespaceOrTypeSymbol container)
    {
        foreach (var member in container.GetMembers().OfType<INamespaceOrTypeSymbol>())
        {
            if (member is INamedTypeSymbol type)
            {
                yield return type;
            }

            foreach (var nested in GetAllTypes(member))
            {
                yield return nested;
            }
        }
    }

    /// <summary>
    /// Reads a single x => x.Prop or "Prop" argument for key, navigation and Ignore calls; null for anything else.
    /// </summary>
    private static string? GetSingleForeignKeyName(InvocationExpressionSyntax invocation)
    {
        if (invocation.ArgumentList.Arguments.Count != 1)
        {
            return null;
        }

        return invocation.ArgumentList.Arguments[0].Expression switch
        {
            LambdaExpressionSyntax { ExpressionBody: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax } member } => member.Name.Identifier.ValueText,
            LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.StringLiteralExpression) => literal.Token.ValueText,
            _ => null
        };
    }

    private static ITypeSymbol GetNonNullableType(ITypeSymbol type)
    {
        return type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullableType
            ? nullableType.TypeArguments[0]
            : type.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
    }

    private static bool IsScalarProperty(IPropertySymbol propertySymbol)
    {
        return propertySymbol.Type.IsValueType || propertySymbol.Type.SpecialType == SpecialType.System_String;
    }

    private static string? GetForeignKeyAttributeName(IPropertySymbol propertySymbol)
    {
        var attribute = propertySymbol.GetAttributes().FirstOrDefault(a =>
            a.AttributeClass is not null && a.AttributeClass.Name.Equals("ForeignKeyAttribute", StringComparison.Ordinal));
        return attribute?.ConstructorArguments.FirstOrDefault().Value as string;
    }

    /// <summary>
    /// Mirrors the legacy scaffolder: the first non-nullable string property of the principal entity in EF Core
    /// property order (primary key first, then ordinal by name), limited to convention-mapped properties;
    /// null when there is none.
    /// </summary>
    private static string? GetDisplayPropertyName(List<IPropertySymbol> principalProperties, string principalKeyName)
    {
        return principalProperties
            .Where(p => !p.IsStatic &&
                !p.IsIndexer &&
                p.GetMethod?.DeclaredAccessibility == Accessibility.Public &&
                p.SetMethod is not null &&
                !HasAttribute(p, "NotMappedAttribute") &&
                p.Type.SpecialType == SpecialType.System_String &&
                (p.NullableAnnotation == NullableAnnotation.NotAnnotated ||
                 string.Equals(p.Name, principalKeyName, StringComparison.Ordinal) ||
                 HasAttribute(p, "RequiredAttribute")))
            .Select(p => p.Name)
            .OrderBy(name => string.Equals(name, principalKeyName, StringComparison.Ordinal) ? 0 : 1)
            .ThenBy(name => name, StringComparer.Ordinal)
            .FirstOrDefault();
    }

    private static bool HasAttribute(IPropertySymbol propertySymbol, string attributeName)
    {
        return propertySymbol.GetAttributes().Any(a =>
            a.AttributeClass is not null && a.AttributeClass.Name.Equals(attributeName, StringComparison.Ordinal));
    }

    /// <summary>
    /// Check if atleast one property and a primary key property were found for given --model class.
    /// </summary>
    /// <param name="modelInfo">The model information to validate.</param>
    /// <param name="logger">The logger to use for error messages.</param>
    /// <returns>True if the model is valid for CRUD scaffolders; otherwise, false.</returns>
    internal static bool ValidateModelForCrudScaffolders(ModelInfo modelInfo, ILogger logger)
    {
        if (modelInfo is null || string.IsNullOrEmpty(modelInfo.ModelTypeName))
        {
            return false;
        }
        else if (modelInfo.ModelProperties is null || modelInfo.ModelProperties.Count == 0)
        {
            logger.LogError($"No properties found for --model '{modelInfo.ModelTypeName}'");
            return false;
        }
        else if (string.IsNullOrEmpty(modelInfo.PrimaryKeyName))
        {
            logger.LogError($"No primary key found for --model '{modelInfo.ModelTypeName}'");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Gets information about a project, including its path, code service, lowest target framework, and capabilities.
    /// </summary>
    /// <param name="projectPath">The path to the project.</param>
    /// <param name="logger">The logger to use for initialization and diagnostics.</param>
    /// <returns>A <see cref="ProjectInfo"/> object with details about the project.</returns>
    internal static ProjectInfo GetProjectInfo(string projectPath, ILogger logger)
    {
        /* to use MSBuildProjectService, we need to initialize the MsBuildInitializer
         * that is because MSBuildLocator.Register fails if Microsoft.Build assemblies are already pulled in
         * using MSBuildProjectService does that unfortunately so this initialization cannot happen in that helper service
         * unlike ICodeService, also no chance for a wasted op because at this point in the scaffolder,
         * we will definitely to have MSBuild initialized.*/
        new MsBuildInitializer(logger).Initialize();
        CodeService codeService = new(logger, projectPath);
        MSBuildProjectService msBuildProject = new(projectPath);
        List<string> capabilities = msBuildProject.GetProjectCapabilities().ToList();
        ProjectInfo projectInfo = new(projectPath)
        {
            CodeService = codeService,
            Capabilities = capabilities
        };

        return projectInfo;
    }
}
