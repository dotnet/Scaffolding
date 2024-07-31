// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.Scaffolding.CodeModification;

public class EfModelProperties
{
    public required List<IPropertySymbol> AllModelProperties { get; init; }
    public required string PrimaryKeyName { get; init; }
    public required string PrimaryKeyShortTypeName { get; init; }
    public required string PrimaryKeyTypeName { get; init; }
}
