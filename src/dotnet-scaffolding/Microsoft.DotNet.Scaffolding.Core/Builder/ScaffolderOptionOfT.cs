// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using Microsoft.DotNet.Scaffolding.Core.ComponentModel;

namespace Microsoft.DotNet.Scaffolding.Core.Builder;

public class ScaffolderOption<T> : ScaffolderOption
{
    private Option<T>? _cliOption;

    internal override Option ToCliOption()
    {
        _cliOption ??= new Option<T>(FixedName);

        return _cliOption;
    }

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

    private string FixedName => CliOption ?? $"--{DisplayName.ToLowerInvariant().Replace(" ", "-")}";
}
