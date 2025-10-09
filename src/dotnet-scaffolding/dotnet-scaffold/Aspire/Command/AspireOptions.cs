// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.ComponentModel;

namespace Microsoft.DotNet.Tools.Scaffold.Aspire.Command;

internal static class AspireOptions
{
    public static ScaffolderOption<string> CachingType => new()
    {
        DisplayName = AspireCliStrings.CachingTypeOption,
        CliOption = AspireCliStrings.TypeCliOption,
        Description = AspireCliStrings.CachingTypeDescription,
        Required = true,
        PickerType = InteractivePickerType.CustomPicker,
        CustomPickerValues = AspireCliStrings.CachingTypeCustomValues
    };

    public static ScaffolderOption<string> DatabaseType => new()
    {
        DisplayName = AspireCliStrings.Database.DatabaseTypeOption,
        CliOption = AspireCliStrings.TypeCliOption,
        Description = AspireCliStrings.Database.DatabaseTypeDescription,
        Required = true,
        PickerType = InteractivePickerType.CustomPicker,
        CustomPickerValues = AspireCliStrings.Database.DatabaseTypeCustomValues
    };

    public static ScaffolderOption<string> StorageType => new()
    {
        DisplayName = AspireCliStrings.StorageTypeOption,
        CliOption = AspireCliStrings.TypeCliOption,
        Description = AspireCliStrings.StorageTypeDescription,
        Required = true,
        PickerType = InteractivePickerType.CustomPicker,
        CustomPickerValues = AspireCliStrings.StorageTypeCustomValues
    };

    public static ScaffolderOption<string> AppHostProject => new()
    {
        DisplayName = AspireCliStrings.AppHostProjectOption,
        CliOption = AspireCliStrings.AppHostCliOption,
        Description = AspireCliStrings.AppHostProjectDescription,
        Required = true,
        PickerType = InteractivePickerType.ProjectPicker
    };

    public static ScaffolderOption<string> Project => new()
    {
        DisplayName = AspireCliStrings.ProjectOption,
        CliOption = AspireCliStrings.WorkerProjectCliOption,
        Description = AspireCliStrings.ProjectOptionDescription,
        Required = true,
        PickerType = InteractivePickerType.ProjectPicker
    };

    public static ScaffolderOption<bool> Prerelease => new()
    {
        DisplayName = AspireCliStrings.PrereleaseOption,
        CliOption = AspireCliStrings.PrereleaseCliOption,
        Description = AspireCliStrings.PrereleaseDescription,
        Required = false,
        PickerType = InteractivePickerType.YesNo
    };
}
