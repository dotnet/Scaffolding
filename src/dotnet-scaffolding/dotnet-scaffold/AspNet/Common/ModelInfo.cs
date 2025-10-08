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
}
