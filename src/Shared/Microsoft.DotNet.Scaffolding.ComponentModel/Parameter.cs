// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.Scaffolding.ComponentModel
{
    public class Parameter
    {
        public string Name { get; set; } = default!;
        public string DisplayName { get; set; } = default!;
        public bool Required { get; set; } = false;
        public string? Description { get; set; }
        public BaseTypes Type { get; set; }
        public InteractivePickerType? PickerType { get; set; }

        internal static Dictionary<BaseTypes, Type> TypeDict = new()
        {
            { BaseTypes.Bool, typeof(bool) },
            { BaseTypes.Int, typeof(int) },
            { BaseTypes.Long, typeof(long) },
            { BaseTypes.Double, typeof(double) },
            { BaseTypes.Decimal, typeof(decimal) },
            { BaseTypes.Char, typeof(char) },
            { BaseTypes.String, typeof(string) }
        };

        public static Type GetParameterType(BaseTypes baseType)
        {
            return TypeDict[baseType];
        }

        public Type GetParameterType()
        {
            return TypeDict[Type];
        }
    }

    public static class ParameterHelpers
    {
        public static bool CheckType(BaseTypes baseType, List<string> values)
        {
            var expectedType = Parameter.TypeDict[baseType];
            if (values.Count == 1 && expectedType.IsGenericType)
            {
                // Handle singular value when a list type is expected
                return false;
            }

            if (values.Count > 1 && !expectedType.IsGenericType)
            {
                // Handle list value when a singular type is expected
                return false;
            }

            if (values.Any(v => !CanConvertToType(v, expectedType)))
            {
                // Check if any value cannot be converted to the expected type
                return false;
            }

            return true;
        }

        private static bool CanConvertToType(string value, Type type)
        {
            try
            {
                var converter = System.ComponentModel.TypeDescriptor.GetConverter(type);
                return converter.IsValid(value);
            }
            catch
            {
                return false;
            }
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
        DbProviderPicker
    }
}
