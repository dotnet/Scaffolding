// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.ComponentModel;
using Microsoft.DotNet.Scaffolding.Core.Hosting;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Commands;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.DotNet.Tools.Scaffold.Command;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;
using Microsoft.DotNet.Scaffolding.Core.Steps;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet
{
    internal class AspNetCommandService(IScaffoldRunnerBuilder builder) : ICommandService
    {
        IScaffoldRunnerBuilder _builder = builder;

        public Type[] GetScaffoldSteps()
        {
            return
            [
                typeof(AddClientSecretStep),
                typeof(AddAspNetConnectionStringStep),
                typeof(AddFileStep),
                typeof(AreaScaffolderStep),
                typeof(DetectBlazorWasmStep),
                typeof(DotnetNewScaffolderStep),
                typeof(EmptyControllerScaffolderStep),
                typeof(RegisterAppStep),
                typeof(UpdateAppAuthorizationStep),
                typeof(UpdateAppSettingsStep),
                typeof(ValidateBlazorCrudStep),
                typeof(ValidateEfControllerStep),
                typeof(ValidateEntraIdStep),
                typeof(ValidateIdentityStep),
                typeof(ValidateMinimalApiStep),
                typeof(ValidateRazorPagesStep),
                typeof(ValidateViewsStep),
                typeof(WrappedAddPackagesStep),
                typeof(WrappedCodeModificationStep),
                typeof(WrappedTextTemplatingStep)
            ];
        }

        public void AddScaffolderCommands()
        {
            CreateOptions(
               out var projectOption, out var prereleaseOption, out var fileNameOption, out var actionsOption,
               out var areaNameOption, out var modelNameOption, out var endpointsClassOption, out var databaseProviderOption,
               out var databaseProviderRequiredOption, out var identityDbProviderRequiredOption, out var dataContextClassOption, out var dataContextClassRequiredOption,
               out var openApiOption, out var pageTypeOption, out var controllerNameOption, out var viewsOption, out var overwriteOption,
               out var usernameOption, out var tenantIdOption, out var applicationOption, out var selectApplicationOption, out bool areAzCliCommandsSuccessful);

            _builder.AddScaffolder(ScaffolderCatagory.AspNet, AspnetStrings.Blazor.Empty)
                .WithDisplayName(AspnetStrings.Blazor.EmptyDisplayName)
                .WithCategory(AspnetStrings.Catagories.Blazor)
                .WithDescription(AspnetStrings.Blazor.EmptyDescription)
                .WithOption(projectOption)
                .WithOption(fileNameOption)
                .WithStep<DotnetNewScaffolderStep>(config =>
                {
                    var step = config.Step;
                    var context = config.Context;
                    step.ProjectPath = context.GetOptionResult(projectOption);
                    step.FileName = context.GetOptionResult(fileNameOption);
                    step.CommandName = Constants.DotnetCommands.RazorComponentCommandName;
                });

            _builder.AddScaffolder(ScaffolderCatagory.AspNet, AspnetStrings.RazorView.Empty)
                .WithDisplayName(AspnetStrings.RazorView.EmptyDisplayName)
                .WithCategory(AspnetStrings.Catagories.MVC)
                .WithDescription(AspnetStrings.RazorView.EmptyDescription)
                .WithOption(projectOption)
                .WithOption(fileNameOption)
                .WithStep<DotnetNewScaffolderStep>(config =>
                {
                    var step = config.Step;
                    var context = config.Context;
                    step.ProjectPath = context.GetOptionResult(projectOption);
                    step.FileName = context.GetOptionResult(fileNameOption);
                    step.CommandName = Constants.DotnetCommands.ViewCommandName;
                });

            _builder.AddScaffolder(ScaffolderCatagory.AspNet, AspnetStrings.RazorPage.Empty)
                .WithDisplayName(AspnetStrings.RazorPage.EmptyDisplayName)
                .WithCategory(AspnetStrings.Catagories.RazorPages)
                .WithDescription(AspnetStrings.RazorPage.EmptyDescription)
                .WithOption(projectOption)
                .WithOption(fileNameOption)
                .WithStep<DotnetNewScaffolderStep>(config =>
                {
                    var step = config.Step;
                    var context = config.Context;
                    step.ProjectPath = context.GetOptionResult(projectOption);
                    step.NamespaceName = Path.GetFileNameWithoutExtension(step.ProjectPath);
                    step.FileName = context.GetOptionResult(fileNameOption);
                    step.CommandName = Constants.DotnetCommands.RazorPageCommandName;
                });

            _builder.AddScaffolder(ScaffolderCatagory.AspNet, AspnetStrings.Api.ApiController)
                .WithDisplayName(AspnetStrings.Api.ApiControllerDisplayName)
                .WithCategory(AspnetStrings.Catagories.API)
                .WithDescription(AspnetStrings.Api.ApiControllerDescription)
                .WithOptions([projectOption, fileNameOption, actionsOption])
                .WithStep<EmptyControllerScaffolderStep>(config =>
                {
                    var step = config.Step;
                    var context = config.Context;
                    step.ProjectPath = context.GetOptionResult(projectOption);
                    step.FileName = context.GetOptionResult(fileNameOption);
                    step.Actions = context.GetOptionResult(actionsOption);
                    step.CommandName = AspnetStrings.Api.ApiController;
                });

            _builder.AddScaffolder(ScaffolderCatagory.AspNet, AspnetStrings.MVC.Controller)
                .WithDisplayName(AspnetStrings.MVC.DisplayName)
                .WithCategory(AspnetStrings.Catagories.MVC)
                .WithDescription(AspnetStrings.MVC.Description)
                .WithOptions([projectOption, fileNameOption, actionsOption])
                .WithStep<EmptyControllerScaffolderStep>(config =>
                {
                    var step = config.Step;
                    var context = config.Context;
                    step.ProjectPath = context.GetOptionResult(projectOption);
                    step.FileName = context.GetOptionResult(fileNameOption);
                    step.Actions = context.GetOptionResult(actionsOption);
                    step.CommandName = AspnetStrings.MVC.Controller;
                });

            _builder.AddScaffolder(ScaffolderCatagory.AspNet, AspnetStrings.Api.ApiControllerCrud)
                .WithDisplayName(AspnetStrings.Api.ApiControllerCrudDisplayName)
                .WithCategory(AspnetStrings.Catagories.API)
                .WithDescription(AspnetStrings.Api.ApiControllerCrudDescription)
                .WithOptions([projectOption, modelNameOption, controllerNameOption, dataContextClassRequiredOption, databaseProviderRequiredOption, prereleaseOption])
                .WithStep<ValidateEfControllerStep>(config =>
                {
                    var step = config.Step;
                    var context = config.Context;
                    step.Project = context.GetOptionResult(projectOption);
                    step.Model = context.GetOptionResult(modelNameOption);
                    step.DataContext = context.GetOptionResult(dataContextClassRequiredOption);
                    step.DatabaseProvider = context.GetOptionResult(databaseProviderRequiredOption);
                    step.Prerelease = context.GetOptionResult(prereleaseOption);
                    step.ControllerType = AspnetStrings.Catagories.API;
                    step.ControllerName = context.GetOptionResult(controllerNameOption);
                })
                .WithEfControllerAddPackagesStep()
                .WithDbContextStep()
                .WithAspNetConnectionStringStep()
                .WithEfControllerTextTemplatingStep()
                .WithEfControllerCodeChangeStep();

            _builder.AddScaffolder(ScaffolderCatagory.AspNet, AspnetStrings.MVC.ControllerCrud)
                .WithDisplayName(AspnetStrings.MVC.CrudDisplayName)
                .WithCategory(AspnetStrings.Catagories.MVC)
                .WithDescription(AspnetStrings.MVC.CrudDescription)
                .WithOptions([projectOption, modelNameOption, controllerNameOption, viewsOption, dataContextClassRequiredOption, databaseProviderRequiredOption, prereleaseOption])
                .WithStep<ValidateEfControllerStep>(config =>
                {
                    var step = config.Step;
                    var context = config.Context;
                    step.Project = context.GetOptionResult(projectOption);
                    step.Model = context.GetOptionResult(modelNameOption);
                    step.DataContext = context.GetOptionResult(dataContextClassRequiredOption);
                    step.DatabaseProvider = context.GetOptionResult(databaseProviderRequiredOption);
                    step.Prerelease = context.GetOptionResult(prereleaseOption);
                    step.ControllerType = AspnetStrings.Catagories.MVC;
                    step.ControllerName = context.GetOptionResult(controllerNameOption);
                })
                .WithEfControllerAddPackagesStep()
                .WithDbContextStep()
                .WithAspNetConnectionStringStep()
                .WithEfControllerTextTemplatingStep()
                .WithEfControllerCodeChangeStep()
                .WithMvcViewsStep();

            _builder.AddScaffolder(ScaffolderCatagory.AspNet, AspnetStrings.Blazor.Crud)
                .WithDisplayName(AspnetStrings.Blazor.CrudDisplayName)
                .WithCategory(AspnetStrings.Catagories.Blazor)
                .WithDescription(AspnetStrings.Blazor.CrudDescription)
                .WithOptions([projectOption, modelNameOption, dataContextClassRequiredOption, databaseProviderRequiredOption, pageTypeOption, prereleaseOption])
                .WithStep<ValidateBlazorCrudStep>(config =>
                {
                    var step = config.Step;
                    var context = config.Context;
                    step.Project = context.GetOptionResult(projectOption);
                    step.Model = context.GetOptionResult(modelNameOption);
                    step.DataContext = context.GetOptionResult(dataContextClassRequiredOption);
                    step.DatabaseProvider = context.GetOptionResult(databaseProviderRequiredOption);
                    step.Prerelease = context.GetOptionResult(prereleaseOption);
                    step.Page = context.GetOptionResult(pageTypeOption);
                })
                .WithBlazorCrudAddPackagesStep()
                .WithDbContextStep()
                .WithAspNetConnectionStringStep()
                .WithBlazorCrudTextTemplatingStep()
                .WithBlazorCrudCodeChangeStep();

            _builder.AddScaffolder(ScaffolderCatagory.AspNet, AspnetStrings.RazorPage.Crud)
                .WithDisplayName(AspnetStrings.RazorPage.CrudDisplayName)
                .WithCategory(AspnetStrings.Catagories.RazorPages)
                .WithDescription(AspnetStrings.RazorPage.CrudDescription)
                .WithOptions([projectOption, modelNameOption, dataContextClassRequiredOption, databaseProviderRequiredOption, pageTypeOption, prereleaseOption])
                .WithStep<ValidateRazorPagesStep>(config =>
                {
                    var step = config.Step;
                    var context = config.Context;
                    step.Project = context.GetOptionResult(projectOption);
                    step.Model = context.GetOptionResult(modelNameOption);
                    step.DataContext = context.GetOptionResult(dataContextClassRequiredOption);
                    step.DatabaseProvider = context.GetOptionResult(databaseProviderRequiredOption);
                    step.Prerelease = context.GetOptionResult(prereleaseOption);
                    step.Page = context.GetOptionResult(pageTypeOption);
                })
                .WithRazorPagesAddPackagesStep()
                .WithDbContextStep()
                .WithAspNetConnectionStringStep()
                .WithRazorPagesTextTemplatingStep()
                .WithRazorPagesCodeChangeStep();

            _builder.AddScaffolder(ScaffolderCatagory.AspNet, AspnetStrings.RazorView.Views)
                .WithDisplayName(AspnetStrings.RazorView.ViewsDisplayName)
                .WithCategory(AspnetStrings.Catagories.MVC)
                .WithDescription(AspnetStrings.RazorView.ViewsDescription)
                .WithOptions([projectOption, modelNameOption, pageTypeOption])
                .WithStep<ValidateViewsStep>(config =>
                {
                    var step = config.Step;
                    var context = config.Context;
                    step.Project = context.GetOptionResult(projectOption);
                    step.Model = context.GetOptionResult(modelNameOption);
                    step.Page = context.GetOptionResult(pageTypeOption);
                })
                .WithViewsTextTemplatingStep()
                .WithViewsAddFileStep();

            _builder.AddScaffolder(ScaffolderCatagory.AspNet, AspnetStrings.Api.MinimalApi)
                .WithDisplayName(AspnetStrings.Api.MinimalApiDisplayName)
                .WithCategory(AspnetStrings.Catagories.API)
                .WithDescription(AspnetStrings.Api.MinimalApiDescription)
                .WithOptions([projectOption, modelNameOption, endpointsClassOption, openApiOption, dataContextClassOption, databaseProviderOption, prereleaseOption])
                .WithStep<ValidateMinimalApiStep>(config =>
                {
                    var step = config.Step;
                    var context = config.Context;
                    step.Project = context.GetOptionResult(projectOption);
                    step.Model = context.GetOptionResult(modelNameOption);
                    step.DataContext = context.GetOptionResult(dataContextClassOption);
                    step.DatabaseProvider = context.GetOptionResult(databaseProviderOption);
                    step.Prerelease = context.GetOptionResult(prereleaseOption);
                    step.OpenApi = context.GetOptionResult(openApiOption);
                    step.Endpoints = context.GetOptionResult(endpointsClassOption);
                })
                .WithMinimalApiAddPackagesStep()
                .WithDbContextStep()
                .WithAspNetConnectionStringStep()
                .WithMinimalApiTextTemplatingStep()
                .WithMinimalApiCodeChangeStep();

            _builder.AddScaffolder(ScaffolderCatagory.AspNet, AspnetStrings.Area.Name)
                .WithDisplayName(AspnetStrings.Area.DisplayName)
                .WithCategory(AspnetStrings.Catagories.MVC)
                .WithDescription(AspnetStrings.Area.Description)
                .WithOptions([projectOption, areaNameOption])
                .WithStep<AreaScaffolderStep>(config =>
                {
                    var step = config.Step;
                    var context = config.Context;
                    step.Project = context.GetOptionResult(projectOption);
                    step.Name = context.GetOptionResult(areaNameOption);
                });

            _builder.AddScaffolder(ScaffolderCatagory.AspNet, AspnetStrings.Blazor.Identity)
                .WithDisplayName(AspnetStrings.Blazor.IdentityDisplayName)
                .WithCategory(AspnetStrings.Catagories.Blazor)
                .WithCategory(AspnetStrings.Catagories.Identity)
                .WithDescription(AspnetStrings.Blazor.IdentityDescription)
                .WithOptions([projectOption, dataContextClassRequiredOption, identityDbProviderRequiredOption, overwriteOption, prereleaseOption])
                .WithStep<ValidateIdentityStep>(config =>
                {
                    var step = config.Step;
                    var context = config.Context;
                    step.Project = context.GetOptionResult(projectOption);
                    step.DataContext = context.GetOptionResult(dataContextClassRequiredOption);
                    step.DatabaseProvider = context.GetOptionResult(identityDbProviderRequiredOption);
                    step.Prerelease = context.GetOptionResult(prereleaseOption);
                    step.Overwrite = context.GetOptionResult(overwriteOption);
                    step.BlazorScenario = true;
                })
                .WithBlazorIdentityAddPackagesStep()
                .WithIdentityDbContextStep()
                .WithAspNetConnectionStringStep()
                .WithBlazorIdentityTextTemplatingStep()
                .WithBlazorIdentityCodeChangeStep();

            _builder.AddScaffolder(ScaffolderCatagory.AspNet, AspnetStrings.Identity.Name)
                .WithDisplayName(AspnetStrings.Identity.DisplayName)
                .WithCategory(AspnetStrings.Catagories.Identity)
                .WithDescription(AspnetStrings.Identity.Description)
                .WithOptions([projectOption, dataContextClassRequiredOption, identityDbProviderRequiredOption, overwriteOption, prereleaseOption])
                .WithStep<ValidateIdentityStep>(config =>
                {
                    var step = config.Step;
                    var context = config.Context;
                    step.Project = context.GetOptionResult(projectOption);
                    step.DataContext = context.GetOptionResult(dataContextClassRequiredOption);
                    step.DatabaseProvider = context.GetOptionResult(identityDbProviderRequiredOption);
                    step.Prerelease = context.GetOptionResult(prereleaseOption);
                    step.Overwrite = context.GetOptionResult(overwriteOption);
                })
                .WithIdentityAddPackagesStep()
                .WithIdentityDbContextStep()
                .WithAspNetConnectionStringStep()
                .WithIdentityTextTemplatingStep()
                .WithIdentityCodeChangeStep();

            if (areAzCliCommandsSuccessful)
            {
                _builder.AddScaffolder(ScaffolderCatagory.AspNet, AspnetStrings.EntraId.Name)
                    .WithDisplayName(AspnetStrings.EntraId.DisplayName)
                    .WithCategory(AspnetStrings.Catagories.EntraId)
                    .WithDescription(AspnetStrings.EntraId.Description)
                    .WithOptions([usernameOption, projectOption, tenantIdOption, applicationOption, selectApplicationOption])
                    .WithStep<ValidateEntraIdStep>(config =>
                    {
                        var step = config.Step;
                        var context = config.Context;
                        step.Username = context.GetOptionResult(usernameOption);
                        step.Project = context.GetOptionResult(projectOption);
                        step.TenantId = context.GetOptionResult(tenantIdOption);
                        step.Application = context.GetOptionResult(applicationOption);
                        step.SelectApplication = context.GetOptionResult(selectApplicationOption);
                    })
                    .WithRegisterAppStep()
                    .WithAddClientSecretStep()
                    .WithDetectBlazorWasmStep()
                    .WithUpdateAppSettingsStep()
                    .WithUpdateAppAuthorizationStep()
                    .WithEntraAddPackagesStep()
                    .WithEntraBlazorWasmAddPackagesStep()
                    .WithEntraIdCodeChangeStep()
                    .WithEntraIdBlazorWasmCodeChangeStep()
                    .WithEntraIdTextTemplatingStep();
            }
        }

        private static void CreateOptions(
           out ScaffolderOption<string> projectOption,
           out ScaffolderOption<bool> prereleaseOption,
           out ScaffolderOption<string> fileNameOption,
           out ScaffolderOption<bool> actionsOption,
           out ScaffolderOption<string> areaNameOption,
           out ScaffolderOption<string> modelNameOption,
           out ScaffolderOption<string> endpointsClassOption,
           out ScaffolderOption<string> databaseProviderOption,
           out ScaffolderOption<string> databaseProviderRequiredOption,
           out ScaffolderOption<string> identityDbProviderRequiredOption,
           out ScaffolderOption<string> dataContextClassOption,
           out ScaffolderOption<string> dataContextClassRequiredOption,
           out ScaffolderOption<bool> openApiOption,
           out ScaffolderOption<string> pageTypeOption,
           out ScaffolderOption<string> controllerNameOption,
           out ScaffolderOption<bool> viewsOption,
           out ScaffolderOption<bool> overwriteOption,
           out ScaffolderOption<string> usernameOption,
           out ScaffolderOption<string> tenantIdOption,
           out ScaffolderOption<string> applicationOption,
           out ScaffolderOption<string> selectApplicationOption,
           out bool areAzCliCommandsSuccessful)
        {
            areAzCliCommandsSuccessful = AzCliHelper.GetAzureInformation(out List<string> usernames, out List<string> tenants, out List<string> appIds);

            projectOption = new ScaffolderOption<string>
            {
                DisplayName = AspnetStrings.Options.Project.DisplayName,
                CliOption = Constants.CliOptions.ProjectCliOption,
                Description = AspnetStrings.Options.Project.Description,
                Required = true,
                PickerType = InteractivePickerType.ProjectPicker
            };

            prereleaseOption = new ScaffolderOption<bool>
            {
                DisplayName = AspnetStrings.Options.Prerelease.DisplayName,
                CliOption = Constants.CliOptions.PrereleaseCliOption,
                Description = AspnetStrings.Options.Prerelease.Description,
                Required = false,
                PickerType = InteractivePickerType.YesNo
            };

            fileNameOption = new ScaffolderOption<string>
            {
                DisplayName = AspnetStrings.Options.FileName.DisplayName,
                CliOption = Constants.CliOptions.NameOption,
                Description = AspnetStrings.Options.FileName.Description,
                Required = true,
            };

            actionsOption = new ScaffolderOption<bool>
            {
                DisplayName = AspnetStrings.Options.Actions.DisplayName,
                CliOption = Constants.CliOptions.ActionsOption,
                Description = AspnetStrings.Options.Actions.Description,
                Required = true,
                PickerType = InteractivePickerType.YesNo
            };

            controllerNameOption = new ScaffolderOption<string>
            {
                DisplayName = AspnetStrings.Options.ControllerName.DisplayName,
                CliOption = Constants.CliOptions.ControllerNameOption,
                Description = AspnetStrings.Options.ControllerName.Description,
                Required = true
            };

            areaNameOption = new ScaffolderOption<string>
            {
                DisplayName = AspnetStrings.Options.AreaName.DisplayName,
                CliOption = Constants.CliOptions.NameOption,
                Description = AspnetStrings.Options.AreaName.Description,
                Required = true
            };

            modelNameOption = new ScaffolderOption<string>
            {
                DisplayName = AspnetStrings.Options.ModelName.DisplayName,
                CliOption = Constants.CliOptions.ModelCliOption,
                Description = AspnetStrings.Options.ModelName.Description,
                Required = true,
                PickerType = InteractivePickerType.ClassPicker
            };

            endpointsClassOption = new ScaffolderOption<string>
            {
                DisplayName = AspnetStrings.Options.EndpointClassDisplayName,
                CliOption = Constants.CliOptions.EndpointsOption,
                Description = string.Empty,
                Required = true
            };

            dataContextClassOption = new ScaffolderOption<string>
            {
                DisplayName = AspnetStrings.Options.DataContextClassDisplayName,
                CliOption = Constants.CliOptions.DataContextOption,
                Description = string.Empty,
                Required = false
            };

            dataContextClassRequiredOption = new ScaffolderOption<string>
            {
                DisplayName = AspnetStrings.Options.DataContextClassDisplayName,
                CliOption = Constants.CliOptions.DataContextOption,
                Description = string.Empty,
                Required = true
            };

            openApiOption = new ScaffolderOption<bool>
            {
                DisplayName = AspnetStrings.Options.OpenApiDisplayName,
                CliOption = Constants.CliOptions.OpenApiOption,
                Description = string.Empty,
                Required = false,
                PickerType = InteractivePickerType.YesNo
            };

            databaseProviderOption = new ScaffolderOption<string>
            {
                DisplayName = AspnetStrings.Options.DbProviderDisplayName,
                CliOption = Constants.CliOptions.DbProviderOption,
                Description = string.Empty,
                Required = false,
                PickerType = InteractivePickerType.CustomPicker,
                CustomPickerValues = [.. AspNetDbContextHelper.DbContextTypeDefaults.Keys]
            };

            databaseProviderRequiredOption = new ScaffolderOption<string>
            {
                DisplayName = AspnetStrings.Options.DbProviderDisplayName,
                CliOption = Constants.CliOptions.DbProviderOption,
                Description = string.Empty,
                Required = true,
                PickerType = InteractivePickerType.CustomPicker,
                CustomPickerValues = [.. AspNetDbContextHelper.DbContextTypeDefaults.Keys]
            };

            identityDbProviderRequiredOption = new ScaffolderOption<string>
            {
                DisplayName = AspnetStrings.Options.DbProviderDisplayName,
                CliOption = Constants.CliOptions.DbProviderOption,
                Description = string.Empty,
                Required = true,
                PickerType = InteractivePickerType.CustomPicker,
                CustomPickerValues = [.. AspNetDbContextHelper.IdentityDbContextTypeDefaults.Keys]
            };

            pageTypeOption = new ScaffolderOption<string>
            {
                DisplayName = AspnetStrings.Options.PageType.DisplayName,
                CliOption = Constants.CliOptions.PageTypeOption,
                Description = AspnetStrings.Options.PageType.Description,
                Required = true,
                PickerType = InteractivePickerType.CustomPicker,
                CustomPickerValues = BlazorCrudHelper.CRUDPages
            };

            viewsOption = new ScaffolderOption<bool>
            {
                DisplayName = AspnetStrings.Options.View.DisplayName,
                CliOption = Constants.CliOptions.ViewsOption,
                Description = AspnetStrings.Options.View.Description,
                Required = true,
                PickerType = InteractivePickerType.YesNo
            };

            overwriteOption = new ScaffolderOption<bool>
            {
                DisplayName = AspnetStrings.Options.Overwrite.DisplayName,
                CliOption = Constants.CliOptions.OverwriteOption,
                Description = AspnetStrings.Options.Overwrite.Description,
                Required = true,
                PickerType = InteractivePickerType.YesNo
            };

            usernameOption = new ScaffolderOption<string>
            {
                DisplayName = AspnetStrings.Options.Username.DisplayName,
                CliOption = Constants.CliOptions.UsernameOption,
                Description = AspnetStrings.Options.Username.Description,
                Required = true,
                PickerType = InteractivePickerType.CustomPicker,
                CustomPickerValues = usernames
            };

            tenantIdOption = new ScaffolderOption<string>
            {
                DisplayName = AspnetStrings.Options.TenantId.DisplayName,
                CliOption = Constants.CliOptions.TenantIdOption,
                Description = AspnetStrings.Options.TenantId.Description,
                Required = true,
                PickerType = InteractivePickerType.CustomPicker,
                CustomPickerValues = tenants
            };

            applicationOption = new ScaffolderOption<string>
            {
                DisplayName = AspnetStrings.Options.Application.DisplayName,
                Description = AspnetStrings.Options.Application.Description,
                Required = true,
                PickerType = InteractivePickerType.ConditionalPicker,
                CustomPickerValues = AspnetStrings.Options.Application.Values
            };

            selectApplicationOption = new ScaffolderOption<string>
            {
                DisplayName = AspnetStrings.Options.SelectApplication.DisplayName,
                CliOption = Constants.CliOptions.ApplicationIdOption,
                Description = AspnetStrings.Options.SelectApplication.Description,
                Required = false,
                PickerType = InteractivePickerType.CustomPicker,
                CustomPickerValues = appIds
            };
        }
    }
}
