// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.CommandLine.Parsing;
using Microsoft.DotNet.Scaffolding.Core.ComponentModel;

namespace Microsoft.DotNet.Scaffolding.Core.Builder;

/// <summary>
/// Generic scaffolder option for CLI and interactive UI, supporting a specific type.
/// </summary>
public class ScaffolderOption<T> : ScaffolderOption
{
    private Option<T>? _cliOption;

    /// <summary>
    /// Converts the option to a CLI option of type T.
    /// </summary>
    internal override Option ToCliOption()
    {
        if (_cliOption is null)
        {
            _cliOption = new Option<T>(FixedName)
            {
                Description = Description ?? DisplayName,
                Required = Required
            };

            // Add accepted values hint for custom picker options
            if (CustomPickerValues is not null && CustomPickerValues.Any())
            {
                _cliOption.AcceptOnlyFromAmong([.. CustomPickerValues]);
            }
        }
        return _cliOption;
    }

    /// <summary>
    /// Gets the parsed value from a ParseResult.
    /// </summary>
    internal override object? GetValue(ParseResult parseResult)
    {
        var option = (Option<T>)ToCliOption();
        return parseResult.GetValue(option);
    }

    /// <summary>
    /// Converts the option to a parameter for interactive UI.
    /// </summary>
    internal override Parameter ToParameter()
    {
        return new Parameter()
        {
            Name = FixedName,
            DisplayName = DisplayName,
            Required = Required,
            Description = Description,
            Type = Parameter.GetCliType<T>(),
            PickerType = PickerType,
            CustomPickerValues = CustomPickerValues
        };
    }

    /// <summary>
    /// Gets the normalized CLI option name.
    /// </summary>
    private string FixedName => Parameter.GetParameterName(CliOption, DisplayName);
}
