// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.ComponentModel;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Commands;

internal class AspNetOptions
{
    public static  ScaffolderOption<string> Project => new()
    {
        DisplayName = AspnetStrings.Options.Project.DisplayName,
        CliOption = Constants.CliOptions.ProjectCliOption,
        Description = AspnetStrings.Options.Project.Description,
        Required = true,
        PickerType = InteractivePickerType.ProjectPicker
    };

    public static ScaffolderOption<bool> Prerelease => new()
    {
        DisplayName = AspnetStrings.Options.Prerelease.DisplayName,
        CliOption = Constants.CliOptions.PrereleaseCliOption,
        Description = AspnetStrings.Options.Prerelease.Description,
        Required = false,
        PickerType = InteractivePickerType.YesNo
    };

    public static ScaffolderOption<string> FileName => new()
    {
        DisplayName = AspnetStrings.Options.FileName.DisplayName,
        CliOption = Constants.CliOptions.NameOption,
        Description = AspnetStrings.Options.FileName.Description,
        Required = true,
    };

    public static ScaffolderOption<bool> Actions => new()
    {
        DisplayName = AspnetStrings.Options.Actions.DisplayName,
        CliOption = Constants.CliOptions.ActionsOption,
        Description = AspnetStrings.Options.Actions.Description,
        Required = true,
        PickerType = InteractivePickerType.YesNo
    };

    public static ScaffolderOption<string> AreaName => new()
    {
        DisplayName = AspnetStrings.Options.AreaName.DisplayName,
        CliOption = Constants.CliOptions.NameOption,
        Description = AspnetStrings.Options.AreaName.Description,
        Required = true
    };

    public static ScaffolderOption<string> ModelName => new()
    {
        DisplayName = AspnetStrings.Options.ModelName.DisplayName,
        CliOption = Constants.CliOptions.ModelCliOption,
        Description = AspnetStrings.Options.ModelName.Description,
        Required = true,
        PickerType = InteractivePickerType.ClassPicker
    };

    public static ScaffolderOption<string> EndpointsClass => new()
    {
        DisplayName = AspnetStrings.Options.EndpointClassDisplayName,
        CliOption = Constants.CliOptions.EndpointsOption,
        Description = string.Empty,
        Required = true
    };

    public static ScaffolderOption<string> DatabaseProvider => new()
    {
        DisplayName = AspnetStrings.Options.DbProviderDisplayName,
        CliOption = Constants.CliOptions.DbProviderOption,
        Description = string.Empty,
        Required = false,
        PickerType = InteractivePickerType.CustomPicker,
        CustomPickerValues = [.. AspNetDbContextHelper.DbContextTypeDefaults.Keys]
    };

    public static ScaffolderOption<string> DatabaseProviderRequired => new()
    {
        DisplayName = AspnetStrings.Options.DbProviderDisplayName,
        CliOption = Constants.CliOptions.DbProviderOption,
        Description = string.Empty,
        Required = true,
        PickerType = InteractivePickerType.CustomPicker,
        CustomPickerValues = [.. AspNetDbContextHelper.DbContextTypeDefaults.Keys]
    };

    public static ScaffolderOption<string> IdentityDbProviderRequired => new()
    {
        DisplayName = AspnetStrings.Options.DbProviderDisplayName,
        CliOption = Constants.CliOptions.DbProviderOption,
        Description = string.Empty,
        Required = true,
        PickerType = InteractivePickerType.CustomPicker,
        CustomPickerValues = [.. AspNetDbContextHelper.IdentityDbContextTypeDefaults.Keys]
    };

    public static ScaffolderOption<string> DataContextClass => new()
    {
        DisplayName = AspnetStrings.Options.DataContextClassDisplayName,
        CliOption = Constants.CliOptions.DataContextOption,
        Description = string.Empty,
        Required = false
    };

    public static ScaffolderOption<string> DataContextClassRequired => new()
    {
        DisplayName = AspnetStrings.Options.DataContextClassDisplayName,
        CliOption = Constants.CliOptions.DataContextOption,
        Description = string.Empty,
        Required = true
    };

    public static ScaffolderOption<bool> OpenApi => new()
    {
        DisplayName = AspnetStrings.Options.OpenApiDisplayName,
        CliOption = Constants.CliOptions.OpenApiOption,
        Description = string.Empty,
        Required = false,
        PickerType = InteractivePickerType.YesNo
    };

    public static ScaffolderOption<string> PageType => new()
    {
        DisplayName = AspnetStrings.Options.PageType.DisplayName,
        CliOption = Constants.CliOptions.PageTypeOption,
        Description = AspnetStrings.Options.PageType.Description,
        Required = true,
        PickerType = InteractivePickerType.CustomPicker,
        CustomPickerValues = BlazorCrudHelper.CRUDPages
    };

    public static ScaffolderOption<string> ControllerName => new()
    {
        DisplayName = AspnetStrings.Options.ControllerName.DisplayName,
        CliOption = Constants.CliOptions.ControllerNameOption,
        Description = AspnetStrings.Options.ControllerName.Description,
        Required = true
    };

    public static ScaffolderOption<bool> Views => new()
    {
        DisplayName = AspnetStrings.Options.View.DisplayName,
        CliOption = Constants.CliOptions.ViewsOption,
        Description = AspnetStrings.Options.View.Description,
        Required = true,
        PickerType = InteractivePickerType.YesNo
    };

    public static ScaffolderOption<bool> Overwrite => new()
    {
        DisplayName = AspnetStrings.Options.Overwrite.DisplayName,
        CliOption = Constants.CliOptions.OverwriteOption,
        Description = AspnetStrings.Options.Overwrite.Description,
        Required = true,
        PickerType = InteractivePickerType.YesNo
    };

    private readonly bool _areAzCliCommandsSuccessful;
    private readonly List<string> _usernames = [];
    private readonly List<string> _tenants = [];
    private readonly List<string> _appIds = [];
    private ScaffolderOption<string>? _username = null;
    private ScaffolderOption<string>? _tenantId = null;
    private ScaffolderOption<string>? _applicationId = null;

    public AspNetOptions()
    {
        _areAzCliCommandsSuccessful = AzCliHelper.GetAzureInformation(out List<string> usernames, out List<string> tenants, out List<string> appIds);
        _usernames = usernames;
        _tenants = tenants;
        _appIds = appIds;
    }

    public bool AreAzCliCommandsSuccessful() => _areAzCliCommandsSuccessful;

    public ScaffolderOption<string> Username => _username ??=  new()
    {
        DisplayName = AspnetStrings.Options.Username.DisplayName,
        CliOption = Constants.CliOptions.UsernameOption,
        Description = AspnetStrings.Options.Username.Description,
        Required = true,
        PickerType = InteractivePickerType.CustomPicker,
        CustomPickerValues = _usernames
    };
    

    public ScaffolderOption<string> TenantId => _tenantId ??= new()
    {
        DisplayName = AspnetStrings.Options.TenantId.DisplayName,
        CliOption = Constants.CliOptions.TenantIdOption,
        Description = AspnetStrings.Options.TenantId.Description,
        Required = true,
        PickerType = InteractivePickerType.CustomPicker,
        CustomPickerValues = _tenants
    };

    public static ScaffolderOption<string> Application => new()
    {
        DisplayName = AspnetStrings.Options.Application.DisplayName,
        Description = AspnetStrings.Options.Application.Description,
        Required = true,
        PickerType = InteractivePickerType.ConditionalPicker,
        CustomPickerValues = AspnetStrings.Options.Application.Values
    };

    public ScaffolderOption<string> SelectApplication => _applicationId ??= new()
    {
        DisplayName = AspnetStrings.Options.SelectApplication.DisplayName,
        CliOption = Constants.CliOptions.ApplicationIdOption,
        Description = AspnetStrings.Options.SelectApplication.Description,
        Required = false,
        PickerType = InteractivePickerType.CustomPicker,
        CustomPickerValues = _appIds
    };
}
