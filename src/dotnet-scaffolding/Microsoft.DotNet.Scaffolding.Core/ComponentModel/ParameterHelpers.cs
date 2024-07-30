// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Microsoft.DotNet.Scaffolding.Core.ComponentModel;

internal static class ParameterHelpers
{
    public static bool CheckType(CliTypes cliType, string value)
    {
        var expectedType = Parameter.GetType(cliType);
        if (CanConvertToType(value, expectedType))
        {
            return true;
        }

        return false;
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
