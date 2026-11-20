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

            // A principal entity must have a discoverable primary key, resolved with EF Core precedence:
            // a single [Key], then "Id", then "<Type>Id", regardless of declaration order. Composite keys
            // ([Key] on several properties or a class-level [PrimaryKey]) are not supported.
            var principalProperties = EfDbContextHelpers.GetModelProperties(principalType);
            if (principalProperties is null)
            {
                continue;
            }

            var keyAttributeProperties = principalProperties.AllModelProperties.Where(p => HasAttribute(p, "KeyAttribute")).ToList();
            if (keyAttributeProperties.Count > 1 ||
                principalType.GetAttributes().Any(a => a.AttributeClass?.Name == "PrimaryKeyAttribute"))
            {
                continue;
            }

            var principalKeyProperty = keyAttributeProperties.SingleOrDefault() ??
                principalProperties.AllModelProperties.FirstOrDefault(p =>
                    !p.IsStatic && string.Equals(p.Name, "Id", StringComparison.OrdinalIgnoreCase)) ??
                principalProperties.AllModelProperties.FirstOrDefault(p =>
                    !p.IsStatic && string.Equals(p.Name, $"{principalType.Name}Id", StringComparison.OrdinalIgnoreCase));
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
                HasPrincipalKeyConfiguration(dbContextSymbol, modelInfo.ModelTypeName, principalType, navigationProperty) ||
                HasConflictingForeignKeyConfiguration(dbContextSymbol, modelInfo.ModelTypeName, principalType, navigationProperty, foreignKeyProperty.Name))
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
                DisplayPropertyName = GetDisplayPropertyName(principalProperties.AllModelProperties, principalKeyName) ?? principalKeyName,
                EntitySetName = string.IsNullOrEmpty(entitySetName)
                    ? $"Set<{principalType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>()"
                    : entitySetName,
                IsNullable = (foreignKeyProperty.Type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T ||
                    foreignKeyProperty.Type.NullableAnnotation == NullableAnnotation.Annotated) &&
                    !HasAttribute(foreignKeyProperty, "RequiredAttribute")
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
    /// Simple syntax check for an alternate principal key: true when a single fluent statement in the DbContext's
    /// OnModelCreating calls HasPrincipalKey and names both ends of this relationship (the dependent type or the
    /// principal's collection navigation, and the principal type or the dependent's navigation).
    /// Configuration split across statements or placed in IEntityTypeConfiguration classes is not detected.
    /// </summary>
    private static bool HasPrincipalKeyConfiguration(ISymbol? dbContextSymbol, string dependentTypeName, INamedTypeSymbol principalType, IPropertySymbol navigationProperty)
    {
        return GetRelationshipInvocations(dbContextSymbol, "HasPrincipalKey", dependentTypeName, principalType, navigationProperty).Any();
    }

    /// <summary>
    /// Same syntax scan as <see cref="HasPrincipalKeyConfiguration"/> for HasForeignKey: true when a matching statement
    /// configures a foreign key that is not exactly the single property <paramref name="foreignKeyPropertyName"/>
    /// (a different member, several members, or an argument that cannot be read such as a variable).
    /// </summary>
    private static bool HasConflictingForeignKeyConfiguration(ISymbol? dbContextSymbol, string dependentTypeName, INamedTypeSymbol principalType, IPropertySymbol navigationProperty, string foreignKeyPropertyName)
    {
        return GetRelationshipInvocations(dbContextSymbol, "HasForeignKey", dependentTypeName, principalType, navigationProperty)
            .Any(invocation => !string.Equals(GetSingleForeignKeyName(invocation), foreignKeyPropertyName, StringComparison.Ordinal));
    }

    /// <summary>
    /// Reads the property name from HasForeignKey(x => x.Prop) or HasForeignKey("Prop"); null for anything else.
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

    /// <summary>
    /// Returns calls to <paramref name="methodName"/> in the DbContext's OnModelCreating whose enclosing statement names
    /// both ends of this relationship (the dependent type or the principal's collection navigation, and the principal
    /// type or the dependent's navigation).
    /// </summary>
    private static IEnumerable<InvocationExpressionSyntax> GetRelationshipInvocations(ISymbol? dbContextSymbol, string methodName, string dependentTypeName, INamedTypeSymbol principalType, IPropertySymbol navigationProperty)
    {
        if (dbContextSymbol is null)
        {
            yield break;
        }

        var principalNames = new HashSet<string>(StringComparer.Ordinal) { principalType.Name, navigationProperty.Name };
        var dependentNames = new HashSet<string>(StringComparer.Ordinal) { dependentTypeName };
        dependentNames.UnionWith(principalType.GetMembers().OfType<IPropertySymbol>()
            .Where(p => p.Type is INamedTypeSymbol { IsGenericType: true } type && type.TypeArguments.Any(t => t.Name == dependentTypeName))
            .Select(p => p.Name));

        var invocations = dbContextSymbol.DeclaringSyntaxReferences
            .SelectMany(r => r.GetSyntax().DescendantNodes().OfType<MethodDeclarationSyntax>())
            .Where(m => m.Identifier.ValueText == "OnModelCreating")
            .SelectMany(m => m.DescendantNodes().OfType<InvocationExpressionSyntax>())
            .Where(i => i.Expression is MemberAccessExpressionSyntax member && member.Name.Identifier.ValueText == methodName);

        foreach (var invocation in invocations)
        {
            SyntaxNode statement = (SyntaxNode?)invocation.FirstAncestorOrSelf<StatementSyntax>() ??
                invocation.AncestorsAndSelf().OfType<ExpressionSyntax>().Last();
            var identifiers = statement.DescendantTokens()
                .Where(t => t.IsKind(SyntaxKind.IdentifierToken))
                .Select(t => t.ValueText)
                .ToHashSet(StringComparer.Ordinal);
            if (identifiers.Overlaps(principalNames) && identifiers.Overlaps(dependentNames))
            {
                yield return invocation;
            }
        }
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
