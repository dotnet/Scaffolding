// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.Scaffolding.ExtensibilityModel
{
    public class Parameter
    {
        public string? Name { get; set; }
        public string? ShortName { get; set; }
        public bool Required { get; set; }
        public string? Description { get; set; }
        public BaseTypes Type { get; set; }
        public object? Value { get; set; }
    }

    public enum BaseTypes
    {
        Bool,
        Int,
        Long,
        Double,
        Decimal,
        Char,
        String,
        ListBool,
        ListInt,
        ListLong,
        ListDouble,
        ListDecimal,
        ListChar,
        ListString
    }
}
