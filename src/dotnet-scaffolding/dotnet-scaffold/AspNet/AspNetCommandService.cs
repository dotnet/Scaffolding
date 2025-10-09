// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.Hosting;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Commands;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;
using Microsoft.DotNet.Tools.Scaffold.Command;

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
            _builder.AddScaffolder(ScaffolderCatagory.AspNet, AspnetStrings.Blazor.Empty)
                .WithDisplayName(AspnetStrings.Blazor.EmptyDisplayName)
                .WithCategory(AspnetStrings.Catagories.Blazor)
                .WithDescription(AspnetStrings.Blazor.EmptyDescription)
                .WithOption(AspNetOptions.Project)
                .WithOption(AspNetOptions.FileName)
                .WithStep<DotnetNewScaffolderStep>(config =>
                {
                    var step = config.Step;
                    var context = config.Context;
                    step.ProjectPath = context.GetOptionResult(AspNetOptions.Project);
                    step.FileName = context.GetOptionResult(AspNetOptions.FileName);
                    step.CommandName = Constants.DotnetCommands.RazorComponentCommandName;
                });

            _builder.AddScaffolder(ScaffolderCatagory.AspNet, AspnetStrings.RazorView.Empty)
                .WithDisplayName(AspnetStrings.RazorView.EmptyDisplayName)
                .WithCategory(AspnetStrings.Catagories.MVC)
                .WithDescription(AspnetStrings.RazorView.EmptyDescription)
                .WithOption(AspNetOptions.Project)
                .WithOption(AspNetOptions.FileName)
                .WithStep<DotnetNewScaffolderStep>(config =>
                {
                    var step = config.Step;
                    var context = config.Context;
                    step.ProjectPath = context.GetOptionResult(AspNetOptions.Project);
                    step.FileName = context.GetOptionResult(AspNetOptions.FileName);
                    step.CommandName = Constants.DotnetCommands.ViewCommandName;
                });

            _builder.AddScaffolder(ScaffolderCatagory.AspNet, AspnetStrings.RazorPage.Empty)
                .WithDisplayName(AspnetStrings.RazorPage.EmptyDisplayName)
                .WithCategory(AspnetStrings.Catagories.RazorPages)
                .WithDescription(AspnetStrings.RazorPage.EmptyDescription)
                .WithOption(AspNetOptions.Project)
                .WithOption(AspNetOptions.FileName)
                .WithStep<DotnetNewScaffolderStep>(config =>
                {
                    var step = config.Step;
                    var context = config.Context;
                    step.ProjectPath = context.GetOptionResult(AspNetOptions.Project);
                    step.NamespaceName = Path.GetFileNameWithoutExtension(step.ProjectPath);
                    step.FileName = context.GetOptionResult(AspNetOptions.FileName);
                    step.CommandName = Constants.DotnetCommands.RazorPageCommandName;
                });

            _builder.AddScaffolder(ScaffolderCatagory.AspNet, AspnetStrings.Api.ApiController)
                .WithDisplayName(AspnetStrings.Api.ApiControllerDisplayName)
                .WithCategory(AspnetStrings.Catagories.API)
                .WithDescription(AspnetStrings.Api.ApiControllerDescription)
                .WithOptions([AspNetOptions.Project, AspNetOptions.FileName, AspNetOptions.Actions])
                .WithStep<EmptyControllerScaffolderStep>(config =>
                {
                    var step = config.Step;
                    var context = config.Context;
                    step.ProjectPath = context.GetOptionResult(AspNetOptions.Project);
                    step.FileName = context.GetOptionResult(AspNetOptions.FileName);
                    step.Actions = context.GetOptionResult(AspNetOptions.Actions);
                    step.CommandName = AspnetStrings.Api.ApiController;
                });

            _builder.AddScaffolder(ScaffolderCatagory.AspNet, AspnetStrings.MVC.Controller)
                .WithDisplayName(AspnetStrings.MVC.DisplayName)
                .WithCategory(AspnetStrings.Catagories.MVC)
                .WithDescription(AspnetStrings.MVC.Description)
                .WithOptions([AspNetOptions.Project, AspNetOptions.FileName, AspNetOptions.Actions])
                .WithStep<EmptyControllerScaffolderStep>(config =>
                {
                    var step = config.Step;
                    var context = config.Context;
                    step.ProjectPath = context.GetOptionResult(AspNetOptions.Project);
                    step.FileName = context.GetOptionResult(AspNetOptions.FileName);
                    step.Actions = context.GetOptionResult(AspNetOptions.Actions);
                    step.CommandName = AspnetStrings.MVC.Controller;
                });

            _builder.AddScaffolder(ScaffolderCatagory.AspNet, AspnetStrings.Api.ApiControllerCrud)
                .WithDisplayName(AspnetStrings.Api.ApiControllerCrudDisplayName)
                .WithCategory(AspnetStrings.Catagories.API)
                .WithDescription(AspnetStrings.Api.ApiControllerCrudDescription)
                .WithOptions([AspNetOptions.Project, AspNetOptions.ModelName, AspNetOptions.ControllerName, AspNetOptions.DataContextClassRequired, AspNetOptions.DatabaseProviderRequired, AspNetOptions.Prerelease])
                .WithStep<ValidateEfControllerStep>(config =>
                {
                    var step = config.Step;
                    var context = config.Context;
                    step.Project = context.GetOptionResult(AspNetOptions.Project);
                    step.Model = context.GetOptionResult(AspNetOptions.ModelName);
                    step.DataContext = context.GetOptionResult(AspNetOptions.DataContextClassRequired);
                    step.DatabaseProvider = context.GetOptionResult(AspNetOptions.DatabaseProviderRequired);
                    step.Prerelease = context.GetOptionResult(AspNetOptions.Prerelease);
                    step.ControllerType = AspnetStrings.Catagories.API;
                    step.ControllerName = context.GetOptionResult(AspNetOptions.ControllerName);
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
                .WithOptions([AspNetOptions.Project, AspNetOptions.ModelName, AspNetOptions.ControllerName, AspNetOptions.Views, AspNetOptions.DataContextClassRequired, AspNetOptions.DatabaseProviderRequired, AspNetOptions.Prerelease])
                .WithStep<ValidateEfControllerStep>(config =>
                {
                    var step = config.Step;
                    var context = config.Context;
                    step.Project = context.GetOptionResult(AspNetOptions.Project);
                    step.Model = context.GetOptionResult(AspNetOptions.ModelName);
                    step.DataContext = context.GetOptionResult(AspNetOptions.DataContextClassRequired);
                    step.DatabaseProvider = context.GetOptionResult(AspNetOptions.DatabaseProviderRequired);
                    step.Prerelease = context.GetOptionResult(AspNetOptions.Prerelease);
                    step.ControllerType = AspnetStrings.Catagories.MVC;
                    step.ControllerName = context.GetOptionResult(AspNetOptions.ControllerName);
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
                .WithOptions([AspNetOptions.Project, AspNetOptions.ModelName, AspNetOptions.DataContextClassRequired, AspNetOptions.DatabaseProviderRequired, AspNetOptions.PageType, AspNetOptions.Prerelease])
                .WithStep<ValidateBlazorCrudStep>(config =>
                {
                    var step = config.Step;
                    var context = config.Context;
                    step.Project = context.GetOptionResult(AspNetOptions.Project);
                    step.Model = context.GetOptionResult(AspNetOptions.ModelName);
                    step.DataContext = context.GetOptionResult(AspNetOptions.DataContextClassRequired);
                    step.DatabaseProvider = context.GetOptionResult(AspNetOptions.DatabaseProviderRequired);
                    step.Prerelease = context.GetOptionResult(AspNetOptions.Prerelease);
                    step.Page = context.GetOptionResult(AspNetOptions.PageType);
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
                .WithOptions([AspNetOptions.Project, AspNetOptions.ModelName, AspNetOptions.DataContextClassRequired, AspNetOptions.DatabaseProviderRequired, AspNetOptions.PageType, AspNetOptions.Prerelease])
                .WithStep<ValidateRazorPagesStep>(config =>
                {
                    var step = config.Step;
                    var context = config.Context;
                    step.Project = context.GetOptionResult(AspNetOptions.Project);
                    step.Model = context.GetOptionResult(AspNetOptions.ModelName);
                    step.DataContext = context.GetOptionResult(AspNetOptions.DataContextClassRequired);
                    step.DatabaseProvider = context.GetOptionResult(AspNetOptions.DatabaseProviderRequired);
                    step.Prerelease = context.GetOptionResult(AspNetOptions.Prerelease);
                    step.Page = context.GetOptionResult(AspNetOptions.PageType);
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
                .WithOptions([AspNetOptions.Project, AspNetOptions.ModelName, AspNetOptions.PageType])
                .WithStep<ValidateViewsStep>(config =>
                {
                    var step = config.Step;
                    var context = config.Context;
                    step.Project = context.GetOptionResult(AspNetOptions.Project);
                    step.Model = context.GetOptionResult(AspNetOptions.ModelName);
                    step.Page = context.GetOptionResult(AspNetOptions.PageType);
                })
                .WithViewsTextTemplatingStep()
                .WithViewsAddFileStep();

            _builder.AddScaffolder(ScaffolderCatagory.AspNet, AspnetStrings.Api.MinimalApi)
                .WithDisplayName(AspnetStrings.Api.MinimalApiDisplayName)
                .WithCategory(AspnetStrings.Catagories.API)
                .WithDescription(AspnetStrings.Api.MinimalApiDescription)
                .WithOptions([AspNetOptions.Project, AspNetOptions.ModelName, AspNetOptions.EndpointsClass, AspNetOptions.OpenApi, AspNetOptions.DataContextClass, AspNetOptions.DatabaseProvider, AspNetOptions.Prerelease])
                .WithStep<ValidateMinimalApiStep>(config =>
                {
                    var step = config.Step;
                    var context = config.Context;
                    step.Project = context.GetOptionResult(AspNetOptions.Project);
                    step.Model = context.GetOptionResult(AspNetOptions.ModelName);
                    step.DataContext = context.GetOptionResult(AspNetOptions.DataContextClass);
                    step.DatabaseProvider = context.GetOptionResult(AspNetOptions.DatabaseProvider);
                    step.Prerelease = context.GetOptionResult(AspNetOptions.Prerelease);
                    step.OpenApi = context.GetOptionResult(AspNetOptions.OpenApi);
                    step.Endpoints = context.GetOptionResult(AspNetOptions.EndpointsClass);
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
                .WithOptions([AspNetOptions.Project, AspNetOptions.AreaName])
                .WithStep<AreaScaffolderStep>(config =>
                {
                    var step = config.Step;
                    var context = config.Context;
                    step.Project = context.GetOptionResult(AspNetOptions.Project);
                    step.Name = context.GetOptionResult(AspNetOptions.AreaName);
                });

            _builder.AddScaffolder(ScaffolderCatagory.AspNet, AspnetStrings.Blazor.Identity)
                .WithDisplayName(AspnetStrings.Blazor.IdentityDisplayName)
                .WithCategory(AspnetStrings.Catagories.Blazor)
                .WithCategory(AspnetStrings.Catagories.Identity)
                .WithDescription(AspnetStrings.Blazor.IdentityDescription)
                .WithOptions([AspNetOptions.Project, AspNetOptions.DataContextClassRequired, AspNetOptions.IdentityDbProviderRequired, AspNetOptions.Overwrite, AspNetOptions.Prerelease])
                .WithStep<ValidateIdentityStep>(config =>
                {
                    var step = config.Step;
                    var context = config.Context;
                    step.Project = context.GetOptionResult(AspNetOptions.Project);
                    step.DataContext = context.GetOptionResult(AspNetOptions.DataContextClassRequired);
                    step.DatabaseProvider = context.GetOptionResult(AspNetOptions.IdentityDbProviderRequired);
                    step.Prerelease = context.GetOptionResult(AspNetOptions.Prerelease);
                    step.Overwrite = context.GetOptionResult(AspNetOptions.Overwrite);
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
                .WithOptions([AspNetOptions.Project, AspNetOptions.DataContextClassRequired, AspNetOptions.IdentityDbProviderRequired, AspNetOptions.Overwrite, AspNetOptions.Prerelease])
                .WithStep<ValidateIdentityStep>(config =>
                {
                    var step = config.Step;
                    var context = config.Context;
                    step.Project = context.GetOptionResult(AspNetOptions.Project);
                    step.DataContext = context.GetOptionResult(AspNetOptions.DataContextClassRequired);
                    step.DatabaseProvider = context.GetOptionResult(AspNetOptions.IdentityDbProviderRequired);
                    step.Prerelease = context.GetOptionResult(AspNetOptions.Prerelease);
                    step.Overwrite = context.GetOptionResult(AspNetOptions.Overwrite);
                })
                .WithIdentityAddPackagesStep()
                .WithIdentityDbContextStep()
                .WithAspNetConnectionStringStep()
                .WithIdentityTextTemplatingStep()
                .WithIdentityCodeChangeStep();

            AspNetOptions options = new();

            if (options.AreAzCliCommandsSuccessful())
            {
                _builder.AddScaffolder(ScaffolderCatagory.AspNet, AspnetStrings.EntraId.Name)
                    .WithDisplayName(AspnetStrings.EntraId.DisplayName)
                    .WithCategory(AspnetStrings.Catagories.EntraId)
                    .WithDescription(AspnetStrings.EntraId.Description)
                    .WithOptions([options.Username, AspNetOptions.Project, options.TenantId, AspNetOptions.Application, options.SelectApplication])
                    .WithStep<ValidateEntraIdStep>(config =>
                    {
                        var step = config.Step;
                        var context = config.Context;
                        step.Username = context.GetOptionResult(options.Username);
                        step.Project = context.GetOptionResult(AspNetOptions.Project);
                        step.TenantId = context.GetOptionResult(options.TenantId);
                        step.Application = context.GetOptionResult(AspNetOptions.Application);
                        step.SelectApplication = context.GetOptionResult(options.SelectApplication);
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
    }
}
