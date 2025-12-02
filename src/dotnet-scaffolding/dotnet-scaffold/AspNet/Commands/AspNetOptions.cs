// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.ComponentModel;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.DotNet.Tools.Scaffold.Command;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Commands;

internal class AspNetOptions
{
    public ScaffolderOption<string> Project { get; }
    public ScaffolderOption<bool> Prerelease { get; }
    public ScaffolderOption<string> FileName { get; }
    public ScaffolderOption<bool> Actions { get; }
    public ScaffolderOption<string> AreaName { get; }
    public ScaffolderOption<string> ModelName { get; }
    public ScaffolderOption<string> EndpointsClass { get; }
    public ScaffolderOption<string> DatabaseProvider { get; }
    public ScaffolderOption<string> DatabaseProviderRequired { get; }
    public ScaffolderOption<string> IdentityDbProviderRequired { get; }
    public ScaffolderOption<string> DataContextClass { get; }
    public ScaffolderOption<string> DataContextClassRequired { get; }
    public ScaffolderOption<bool> OpenApi { get; }
    public ScaffolderOption<string> PageType { get; }
    public ScaffolderOption<string> ControllerName { get; }
    public ScaffolderOption<bool> Views { get; }
    public ScaffolderOption<bool> Overwrite { get; }
    public ScaffolderOption<string> Application { get; }
    public ScaffolderOption<string> TargetFramework { get; }

    private ScaffolderOption<string>? _username = null;
    private ScaffolderOption<string>? _tenantId = null;
    private ScaffolderOption<string>? _applicationId = null;

    public AspNetOptions()
    {
        Project = new ScaffolderOption<string>
        {
            DisplayName = AspnetStrings.Options.Project.DisplayName,
            CliOption = Constants.CliOptions.ProjectCliOption,
            Description = AspnetStrings.Options.Project.Description,
            Required = true,
            PickerType = InteractivePickerType.ProjectPicker
        };

        Prerelease = new ScaffolderOption<bool>
        {
            DisplayName = AspnetStrings.Options.Prerelease.DisplayName,
            CliOption = Constants.CliOptions.PrereleaseCliOption,
            Description = AspnetStrings.Options.Prerelease.Description,
            Required = false,
            PickerType = InteractivePickerType.YesNo
        };

        FileName = new ScaffolderOption<string>
        {
            DisplayName = AspnetStrings.Options.FileName.DisplayName,
            CliOption = Constants.CliOptions.NameOption,
            Description = AspnetStrings.Options.FileName.Description,
            Required = true,
        };

        Actions = new ScaffolderOption<bool>
        {
            DisplayName = AspnetStrings.Options.Actions.DisplayName,
            CliOption = Constants.CliOptions.ActionsOption,
            Description = AspnetStrings.Options.Actions.Description,
            Required = true,
            PickerType = InteractivePickerType.YesNo
        };
        AreaName = new ScaffolderOption<string>
        {
            DisplayName = AspnetStrings.Options.AreaName.DisplayName,
            CliOption = Constants.CliOptions.NameOption,
            Description = AspnetStrings.Options.AreaName.Description,
            Required = true
        };

        ModelName = new ScaffolderOption<string>
        {
            DisplayName = AspnetStrings.Options.ModelName.DisplayName,
            CliOption = Constants.CliOptions.ModelCliOption,
            Description = AspnetStrings.Options.ModelName.Description,
            Required = true,
            PickerType = InteractivePickerType.ClassPicker
        };

        EndpointsClass = new ScaffolderOption<string>
        {
            DisplayName = AspnetStrings.Options.EndpointClassDisplayName,
            CliOption = Constants.CliOptions.EndpointsOption,
            Description = string.Empty,
            Required = true
        };
        DatabaseProvider = new ScaffolderOption<string>
        {
            DisplayName = AspnetStrings.Options.DbProviderDisplayName,
            CliOption = Constants.CliOptions.DbProviderOption,
            Description = string.Empty,
            Required = false,
            PickerType = InteractivePickerType.CustomPicker,
            CustomPickerValues = [.. AspNetDbContextHelper.DbContextTypeDefaults.Keys]
        };

        DatabaseProviderRequired = new ScaffolderOption<string>
        {
            DisplayName = AspnetStrings.Options.DbProviderDisplayName,
            CliOption = Constants.CliOptions.DbProviderOption,
            Description = string.Empty,
            Required = true,
            PickerType = InteractivePickerType.CustomPicker,
            CustomPickerValues = [.. AspNetDbContextHelper.DbContextTypeDefaults.Keys]
        };

        IdentityDbProviderRequired = new ScaffolderOption<string>
        {
            DisplayName = AspnetStrings.Options.DbProviderDisplayName,
            CliOption = Constants.CliOptions.DbProviderOption,
            Description = string.Empty,
            Required = true,
            PickerType = InteractivePickerType.CustomPicker,
            CustomPickerValues = [.. AspNetDbContextHelper.IdentityDbContextTypeDefaults.Keys]
        };

        DataContextClass = new ScaffolderOption<string>
        {
            DisplayName = AspnetStrings.Options.DataContextClassDisplayName,
            CliOption = Constants.CliOptions.DataContextOption,
            Description = string.Empty,
            Required = false
        };
        DataContextClassRequired = new ScaffolderOption<string>
        {
            DisplayName = AspnetStrings.Options.DataContextClassDisplayName,
            CliOption = Constants.CliOptions.DataContextOption,
            Description = string.Empty,
            Required = true
        };

        OpenApi = new ScaffolderOption<bool>
        {
            DisplayName = AspnetStrings.Options.OpenApiDisplayName,
            CliOption = Constants.CliOptions.OpenApiOption,
            Description = string.Empty,
            Required = false,
            PickerType = InteractivePickerType.YesNo
        };

        PageType = new ScaffolderOption<string>
        {
            DisplayName = AspnetStrings.Options.PageType.DisplayName,
            CliOption = Constants.CliOptions.PageTypeOption,
            Description = AspnetStrings.Options.PageType.Description,
            Required = true,
            PickerType = InteractivePickerType.CustomPicker,
            CustomPickerValues = BlazorCrudHelper.CRUDPages
        };

        ControllerName = new ScaffolderOption<string>
        {
            DisplayName = AspnetStrings.Options.ControllerName.DisplayName,
            CliOption = Constants.CliOptions.ControllerNameOption,
            Description = AspnetStrings.Options.ControllerName.Description,
            Required = true
        };

        Views = new ScaffolderOption<bool>
        {
            DisplayName = AspnetStrings.Options.View.DisplayName,
            CliOption = Constants.CliOptions.ViewsOption,
            Description = AspnetStrings.Options.View.Description,
            Required = true,
            PickerType = InteractivePickerType.YesNo
        };

        Overwrite = new ScaffolderOption<bool>
        {
            DisplayName = AspnetStrings.Options.Overwrite.DisplayName,
            CliOption = Constants.CliOptions.OverwriteOption,
            Description = AspnetStrings.Options.Overwrite.Description,
            Required = true,
            PickerType = InteractivePickerType.YesNo
        };

        Application = new ScaffolderOption<string>
        {
            DisplayName = AspnetStrings.Options.Application.DisplayName,
            Description = AspnetStrings.Options.Application.Description,
            Required = true,
            PickerType = InteractivePickerType.ConditionalPicker,
            CustomPickerValues = AspnetStrings.Options.Application.Values
        };

        TargetFramework = new ScaffolderOption<string>
        {
            DisplayName = Scaffolding.Core.Model.TargetFrameworkConstants.TargetFrameworkDisplayName,
            CliOption = Scaffolding.Core.Model.TargetFrameworkConstants.TargetFrameworkCliOption,
            Description = CliStrings.TargetFrameworkDescription,
            Required = false,
            PickerType = InteractivePickerType.CustomPicker,
            CustomPickerValues = Scaffolding.Core.Model.TargetFrameworkConstants.SupportedTargetFrameworks
        };
    }

    public ScaffolderOption<string> Username => _username ??=  new()
    {
        DisplayName = AspnetStrings.Options.Username.DisplayName,
        CliOption = Constants.CliOptions.UsernameOption,
        Description = AspnetStrings.Options.Username.Description,
        Required = true,
        PickerType = InteractivePickerType.DynamicPicker,
    };

    public ScaffolderOption<string> TenantId => _tenantId ??= new()
    {
        DisplayName = AspnetStrings.Options.TenantId.DisplayName,
        CliOption = Constants.CliOptions.TenantIdOption,
        Description = AspnetStrings.Options.TenantId.Description,
        Required = true,
        PickerType = InteractivePickerType.DynamicPicker,
    };

    public ScaffolderOption<string> SelectApplication => _applicationId ??= new()
    {
        DisplayName = AspnetStrings.Options.SelectApplication.DisplayName,
        CliOption = Constants.CliOptions.ApplicationIdOption,
        Description = AspnetStrings.Options.SelectApplication.Description,
        Required = false,
        PickerType = InteractivePickerType.DynamicPicker,
    };
}
