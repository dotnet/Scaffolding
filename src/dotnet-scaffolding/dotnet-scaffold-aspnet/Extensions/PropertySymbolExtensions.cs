// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Extensions;

internal static class PropertySymbolExtensions
{
    public static bool HasRequiredAttribute(this IPropertySymbol propertySymbol)
    {
        return propertySymbol.GetAttributes().FirstOrDefault(
            x => x.AttributeClass is not null &&
            x.AttributeClass.Name.Equals(nameof(RequiredAttribute), StringComparison.OrdinalIgnoreCase)) is not null;
    }
}
