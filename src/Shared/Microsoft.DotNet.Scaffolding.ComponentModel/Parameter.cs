// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.Scaffolding.ExtensibilityModel
{
    public class Parameter
    {
        public string Name { get; set; }
        public string ShortName { get; set; }
        public bool Required { get; set; }
        public string Description { get; set; }
        public BaseTypes Type { get; set; }
        public object Value { get; set; }
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

    public Dictionary<BaseTypes, Type> TypeDict = new Dictionary<BaseTypes, Type>()
    {
        { BaseTypes.Bool, typeof(bool) },
        { BaseTypes.Int, typeof(int) },
        { BaseTypes.Long, typeof(long) },
        { BaseTypes.Double, typeof(double) },
        { BaseTypes.Decimal, typeof(decimal) },
        { BaseTypes.Char, typeof(char) },
        { BaseTypes.String, typeof(string) },
        { BaseTypes.ListBool, typeof(List<bool>) },
        { BaseTypes.ListLong, typeof(List<long>) },
        { BaseTypes.ListInt, typeof(List<int>) },
        { BaseTypes.ListDouble, typeof(List<double>) },
        { BaseTypes.Li }
    }
}
