// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Collections.ObjectModel;

namespace Microsoft.DotNet.Scaffolding.Core.ComponentModel;

internal class Parameter
{
    public required string Name { get; init; }
    public required string DisplayName { get; init; }
    public required bool Required { get; set; } = false;
    public string? Description { get; set; }
    public required CliTypes Type { get; set; } = CliTypes.String;
    public InteractivePickerType PickerType { get; set; } = InteractivePickerType.None;
    public IEnumerable<string>? CustomPickerValues { get; set; }

    private static readonly ReadOnlyDictionary<CliTypes, Type> s_baseTypeToTypeMapping = new(
        new Dictionary<CliTypes, Type>
        {
            { CliTypes.Bool, typeof(bool) },
            { CliTypes.Int, typeof(int) },
            { CliTypes.Long, typeof(long) },
            { CliTypes.Double, typeof(double) },
            { CliTypes.Decimal, typeof(decimal) },
            { CliTypes.Char, typeof(char) },
            { CliTypes.String, typeof(string) }
        });

    private static readonly ReadOnlyDictionary<Type, CliTypes> s_typeToBaseTypeMapping = new(s_baseTypeToTypeMapping.ToDictionary(x => x.Value, x => x.Key));

    public static Type GetType(CliTypes baseType)
    {
        return s_baseTypeToTypeMapping[baseType];
    }

    public static CliTypes GetCliType(Type type)
    {
        return s_typeToBaseTypeMapping[type];
    }

    public static CliTypes GetCliType<T>()
        => GetCliType(typeof(T));
}
