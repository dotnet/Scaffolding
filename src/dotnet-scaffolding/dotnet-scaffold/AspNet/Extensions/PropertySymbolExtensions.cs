// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Extensions;

/// <summary>
/// Provides extension methods for <see cref="IPropertySymbol"/> to check for data annotations.
/// </summary>
internal static class PropertySymbolExtensions
{
    /// <summary>
    /// Determines whether the property symbol has a <see cref="RequiredAttribute"/>.
    /// </summary>
    /// <param name="propertySymbol">The property symbol to check.</param>
    /// <returns>True if the property has the Required attribute; otherwise, false.</returns>
    public static bool HasRequiredAttribute(this IPropertySymbol propertySymbol)
    {
        return propertySymbol.GetAttributes().Any(
            x => x.AttributeClass is not null &&
            x.AttributeClass.Name.Equals(nameof(RequiredAttribute), StringComparison.OrdinalIgnoreCase));
    }
}
