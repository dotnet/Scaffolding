// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Collections.ObjectModel;

namespace Microsoft.DotNet.Scaffolding.ComponentModel;

public class Parameter
{
    public required string Name { get; init; }
    public required string DisplayName { get; init; }
    public required bool Required { get; set; } = false;
    public string? Description { get; set; }
    public required BaseTypes Type { get; set; } = BaseTypes.String;
    public InteractivePickerType? PickerType { get; set; }
    public List<string>? CustomPickerValues { get; set; }

    internal static readonly ReadOnlyDictionary<BaseTypes, Type> TypeDict = new(
        new Dictionary<BaseTypes, Type>
        {
            { BaseTypes.Bool, typeof(bool) },
            { BaseTypes.Int, typeof(int) },
            { BaseTypes.Long, typeof(long) },
            { BaseTypes.Double, typeof(double) },
            { BaseTypes.Decimal, typeof(decimal) },
            { BaseTypes.Char, typeof(char) },
            { BaseTypes.String, typeof(string) }
        });

    public static Type GetParameterType(BaseTypes baseType)
    {
        return TypeDict[baseType];
    }

    public Type GetParameterType()
    {
        return TypeDict[Type];
    }
}

//will add List types for all these soon!
public enum BaseTypes
{
    Bool,
    Int,
    Long,
    Double,
    Decimal,
    Char,
    String
}

public enum InteractivePickerType
{
    ClassPicker,
    FilePicker,
    DbProviderPicker,
    ProjectPicker,
    CustomPicker,
    YesNo
}
