// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.Scaffolding.Core.ComponentModel;

/// <summary>
/// Provides helper methods for working with command parameters and their types.
/// </summary>
internal static class ParameterHelpers
{
    /// <summary>
    /// Checks if the provided string value can be converted to the expected type for the given CLI type.
    /// </summary>
    /// <param name="cliType">The CLI type to check against.</param>
    /// <param name="value">The string value to validate.</param>
    /// <returns>True if the value can be converted to the expected type; otherwise, false.</returns>
    public static bool CheckType(CliTypes cliType, string value)
    {
        var expectedType = Parameter.GetType(cliType);
        if (CanConvertToType(value, expectedType))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Determines whether the specified string value can be converted to the given .NET type.
    /// </summary>
    /// <param name="value">The string value to check.</param>
    /// <param name="type">The .NET type to convert to.</param>
    /// <returns>True if the value is valid for the type; otherwise, false.</returns>
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

    public static bool IsTargetFrameworkOption(Parameter parameter)
    {
        return string.Equals(parameter.DisplayName, Model.TargetFrameworkConstants.TargetFrameworkDisplayName, StringComparison.Ordinal);
    }
}
