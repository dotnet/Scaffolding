// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Common;

/// <summary>
/// Represents information about a model class, including its properties, namespace, type, and primary key details.
/// </summary>
internal class ModelInfo
{
    /// <summary>
    /// Gets or sets the list of model properties.
    /// </summary>
    public List<IPropertySymbol>? ModelProperties { get; set; }
    /// <summary>
    /// Gets or sets the namespace of the model class.
    /// </summary>
    public string? ModelNamespace { get; set; }
    /// <summary>
    /// Gets or sets the full name of the model class (namespace + type name).
    /// </summary>
    public string? ModelFullName { get; set; }
    /// <summary>
    /// Gets or sets the type name of the model class.
    /// </summary>
    public string ModelTypeName { get; set; } = default!;
    /// <summary>
    /// Gets the capitalized type name of the model class.
    /// </summary>
    public string ModelTypeNameCapitalized => !string.IsNullOrEmpty(ModelTypeName) ? char.ToUpper(ModelTypeName[0]) + ModelTypeName.Substring(1) : ModelTypeName;
    //TODO pluralize correctly
    /// <summary>
    /// Gets the pluralized type name of the model class (simple pluralization).
    /// </summary>
    public string ModelTypePluralName => $"{ModelTypeName}s";
    /// <summary>
    /// Gets the variable name for the model class (lowercase).
    /// </summary>
    public string ModelVariable => ModelTypeName.ToLowerInvariant();
    /// <summary>
    /// Gets or sets the primary key property name.
    /// </summary>
    public string? PrimaryKeyName { get; set; }
    /// <summary>
    /// Gets or sets the short type name of the primary key.
    /// </summary>
    public string? PrimaryKeyShortTypeName { get; set; }
    /// <summary>
    /// Gets or sets the full type name of the primary key.
    /// </summary>
    public string? PrimaryKeyTypeName { get; set; }
    /// <summary>
    /// Gets or sets the reference navigations that have a scalar foreign key property on the model.
    /// </summary>
    public List<ForeignKeyNavigationInfo> ForeignKeyNavigations { get; set; } = [];
}

/// <summary>
/// Describes a reference navigation on a model that is paired with a single scalar foreign key property.
/// </summary>
internal class ForeignKeyNavigationInfo
{
    /// <summary>
    /// Gets the name of the reference navigation property (e.g. "Country").
    /// </summary>
    public required string NavigationPropertyName { get; init; }
    /// <summary>
    /// Gets the name of the scalar foreign key property (e.g. "CountryId").
    /// </summary>
    public required string ForeignKeyPropertyName { get; init; }
    /// <summary>
    /// Gets the primary key property name of the principal entity.
    /// </summary>
    public required string PrincipalPrimaryKeyName { get; init; }
    /// <summary>
    /// Gets the principal entity property used as the display text of a select list.
    /// </summary>
    public required string DisplayPropertyName { get; init; }
    /// <summary>
    /// Gets the DbContext member expression for the principal entity set (e.g. "Countries" or "Set&lt;global::Models.Country&gt;()").
    /// </summary>
    public required string EntitySetName { get; init; }
    /// <summary>
    /// Gets whether the relationship is optional, so the view adds an empty option: the foreign key is Nullable&lt;T&gt; or a
    /// reference type not annotated as non-nullable, neither the foreign key nor the navigation has [Required], and the
    /// navigation is not annotated as non-nullable.
    /// </summary>
    public required bool IsNullable { get; init; }
}
