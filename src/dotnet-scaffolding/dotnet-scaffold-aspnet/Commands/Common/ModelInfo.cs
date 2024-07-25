// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Commands.Common;

internal class ModelInfo
{
    //Model class info
    public List<IPropertySymbol>? ModelProperties { get; set; }
    public string? ModelNamespace { get; set; }
    public string ModelTypeName { get; set; } = default!;
    public string ModelTypePluralName => $"{ModelTypeName}s";
    public string ModelVariable => ModelTypeName.ToLowerInvariant();
    public string? PrimaryKeyName { get; set; }
    public string? PrimaryKeyShortTypeName { get; set; }
    public string? PrimaryKeyTypeName { get; set; }
}
