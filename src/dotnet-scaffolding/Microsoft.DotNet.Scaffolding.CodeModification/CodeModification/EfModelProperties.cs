// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.Scaffolding.CodeModification;

public class EfModelProperties
{
    public required List<IPropertySymbol> AllModelProperties { get; init; }
    public required List<string> PrimaryKeyName { get; init; }
    public required Dictionary<string, string> PrimaryKeyShortTypeName { get; init; }
    public required Dictionary<string, string> PrimaryKeyTypeName { get; init; }
}
