// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.Hosting;
using Microsoft.DotNet.Scaffolding.Core.Model;
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
                typeof(NuGetVersionService),
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
            AspNetOptions options = new();

            _builder.AddScaffolder(ScaffolderCatagory.AspNet, AspnetStrings.Blazor.Empty)
                .WithDisplayName(AspnetStrings.Blazor.EmptyDisplayName)
                .WithCategory(AspnetStrings.Catagories.Blazor)
                .WithDescription(AspnetStrings.Blazor.EmptyDescription)
                .WithExample(AspnetStrings.Blazor.EmptyExample, AspnetStrings.Blazor.EmptyExampleDescription)
                .WithOption(options.Project)
                .WithOption(options.FileName)
                .WithStep<DotnetNewScaffolderStep>(config =>
                {
                    var step = config.Step;
                    var context = config.Context;
                    step.ProjectPath = context.GetOptionResult(options.Project);
                    step.FileName = context.GetOptionResult(options.FileName);
                    step.CommandName = Constants.DotnetCommands.RazorComponentCommandName;
                });

            _builder.AddScaffolder(ScaffolderCatagory.AspNet, AspnetStrings.RazorView.Empty)
                .WithDisplayName(AspnetStrings.RazorView.EmptyDisplayName)
                .WithCategory(AspnetStrings.Catagories.MVC)
                .WithDescription(AspnetStrings.RazorView.EmptyDescription)
                .WithExample(AspnetStrings.RazorView.EmptyExample, AspnetStrings.RazorView.EmptyExampleDescription)
                .WithOption(options.Project)
                .WithOption(options.FileName)
                .WithStep<DotnetNewScaffolderStep>(config =>
                {
                    var step = config.Step;
                    var context = config.Context;
                    step.ProjectPath = context.GetOptionResult(options.Project);
                    step.FileName = context.GetOptionResult(options.FileName);
                    step.CommandName = Constants.DotnetCommands.ViewCommandName;
                });

            _builder.AddScaffolder(ScaffolderCatagory.AspNet, AspnetStrings.RazorPage.Empty)
                .WithDisplayName(AspnetStrings.RazorPage.EmptyDisplayName)
                .WithCategory(AspnetStrings.Catagories.RazorPages)
                .WithDescription(AspnetStrings.RazorPage.EmptyDescription)
                .WithExample(AspnetStrings.RazorPage.EmptyExample, AspnetStrings.RazorPage.EmptyExampleDescription)
                .WithOption(options.Project)
                .WithOption(options.FileName)
                .WithStep<DotnetNewScaffolderStep>(config =>
                {
                    var step = config.Step;
                    var context = config.Context;
                    step.ProjectPath = context.GetOptionResult(options.Project);
                    step.NamespaceName = Path.GetFileNameWithoutExtension(step.ProjectPath);
                    step.FileName = context.GetOptionResult(options.FileName);
                    step.CommandName = Constants.DotnetCommands.RazorPageCommandName;
                });

            _builder.AddScaffolder(ScaffolderCatagory.AspNet, AspnetStrings.Api.ApiController)
                .WithDisplayName(AspnetStrings.Api.ApiControllerDisplayName)
                .WithCategory(AspnetStrings.Catagories.API)
                .WithDescription(AspnetStrings.Api.ApiControllerDescription)
                .WithExample(AspnetStrings.Api.ApiControllerExample1, AspnetStrings.Api.ApiControllerExample1Description)
                .WithExample(AspnetStrings.Api.ApiControllerExample2, AspnetStrings.Api.ApiControllerExample2Description)
                .WithOptions([options.Project, options.FileName, options.Actions])
                .WithStep<EmptyControllerScaffolderStep>(config =>
                {
                    var step = config.Step;
                    var context = config.Context;
                    step.ProjectPath = context.GetOptionResult(options.Project);
                    step.FileName = context.GetOptionResult(options.FileName);
                    step.Actions = context.GetOptionResult(options.Actions);
                    step.CommandName = AspnetStrings.Api.ApiController;
                });

            _builder.AddScaffolder(ScaffolderCatagory.AspNet, AspnetStrings.MVC.Controller)
                .WithDisplayName(AspnetStrings.MVC.DisplayName)
                .WithCategory(AspnetStrings.Catagories.MVC)
                .WithDescription(AspnetStrings.MVC.Description)
                .WithExample(AspnetStrings.MVC.ControllerExample1, AspnetStrings.MVC.ControllerExample1Description)
                .WithExample(AspnetStrings.MVC.ControllerExample2, AspnetStrings.MVC.ControllerExample2Description)
                .WithOptions([options.Project, options.FileName, options.Actions])
                .WithStep<EmptyControllerScaffolderStep>(config =>
                {
                    var step = config.Step;
                    var context = config.Context;
                    step.ProjectPath = context.GetOptionResult(options.Project);
                    step.FileName = context.GetOptionResult(options.FileName);
                    step.Actions = context.GetOptionResult(options.Actions);
                    step.CommandName = AspnetStrings.MVC.Controller;
                });

            _builder.AddScaffolder(ScaffolderCatagory.AspNet, AspnetStrings.Api.ApiControllerCrud)
                .WithDisplayName(AspnetStrings.Api.ApiControllerCrudDisplayName)
                .WithCategory(AspnetStrings.Catagories.API)
                .WithDescription(AspnetStrings.Api.ApiControllerCrudDescription)
                .WithExample(AspnetStrings.Api.ApiControllerCrudExample1, AspnetStrings.Api.ApiControllerCrudExample1Description)
                .WithExample(AspnetStrings.Api.ApiControllerCrudExample2, AspnetStrings.Api.ApiControllerCrudExample2Description)
                .WithOptions([options.Project, options.ModelName, options.ControllerName, options.DataContextClassRequired, options.DatabaseProviderRequired, options.Prerelease])
                .WithStep<ValidateEfControllerStep>(config =>
                {
                    var step = config.Step;
                    var context = config.Context;
                    step.Project = context.GetOptionResult(options.Project);
                    step.Model = context.GetOptionResult(options.ModelName);
                    step.DataContext = context.GetOptionResult(options.DataContextClassRequired);
                    step.DatabaseProvider = context.GetOptionResult(options.DatabaseProviderRequired);
                    step.Prerelease = context.GetOptionResult(options.Prerelease);
                    step.ControllerType = AspnetStrings.Catagories.API;
                    step.ControllerName = context.GetOptionResult(options.ControllerName);
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
                .WithExample(AspnetStrings.MVC.ControllerCrudExample1, AspnetStrings.MVC.ControllerCrudExample1Description)
                .WithExample(AspnetStrings.MVC.ControllerCrudExample2, AspnetStrings.MVC.ControllerCrudExample2Description)
                .WithOptions([options.Project, options.ModelName, options.ControllerName, options.Views, options.DataContextClassRequired, options.DatabaseProviderRequired, options.Prerelease])
                .WithStep<ValidateEfControllerStep>(config =>
                {
                    var step = config.Step;
                    var context = config.Context;
                    step.Project = context.GetOptionResult(options.Project);
                    step.Model = context.GetOptionResult(options.ModelName);
                    step.DataContext = context.GetOptionResult(options.DataContextClassRequired);
                    step.DatabaseProvider = context.GetOptionResult(options.DatabaseProviderRequired);
                    step.Prerelease = context.GetOptionResult(options.Prerelease);
                    step.ControllerType = AspnetStrings.Catagories.MVC;
                    step.ControllerName = context.GetOptionResult(options.ControllerName);
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
                .WithExample(AspnetStrings.Blazor.CrudExample1, AspnetStrings.Blazor.CrudExample1Description)
                .WithExample(AspnetStrings.Blazor.CrudExample2, AspnetStrings.Blazor.CrudExample2Description)
                .WithOptions([options.Project, options.ModelName, options.DataContextClassRequired, options.DatabaseProviderRequired, options.PageType, options.Prerelease])
                .WithStep<ValidateBlazorCrudStep>(config =>
                {
                    var step = config.Step;
                    var context = config.Context;
                    step.Project = context.GetOptionResult(options.Project);
                    step.Model = context.GetOptionResult(options.ModelName);
                    step.DataContext = context.GetOptionResult(options.DataContextClassRequired);
                    step.DatabaseProvider = context.GetOptionResult(options.DatabaseProviderRequired);
                    step.Prerelease = context.GetOptionResult(options.Prerelease);
                    step.Page = context.GetOptionResult(options.PageType);
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
                .WithExample(AspnetStrings.RazorPage.CrudExample1, AspnetStrings.RazorPage.CrudExample1Description)
                .WithExample(AspnetStrings.RazorPage.CrudExample2, AspnetStrings.RazorPage.CrudExample2Description)
                .WithOptions([options.Project, options.ModelName, options.DataContextClassRequired, options.DatabaseProviderRequired, options.PageType, options.Prerelease])
                .WithStep<ValidateRazorPagesStep>(config =>
                {
                    var step = config.Step;
                    var context = config.Context;
                    step.Project = context.GetOptionResult(options.Project);
                    step.Model = context.GetOptionResult(options.ModelName);
                    step.DataContext = context.GetOptionResult(options.DataContextClassRequired);
                    step.DatabaseProvider = context.GetOptionResult(options.DatabaseProviderRequired);
                    step.Prerelease = context.GetOptionResult(options.Prerelease);
                    step.Page = context.GetOptionResult(options.PageType);
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
                .WithExample(AspnetStrings.RazorView.ViewsExample1, AspnetStrings.RazorView.ViewsExample1Description)
                .WithExample(AspnetStrings.RazorView.ViewsExample2, AspnetStrings.RazorView.ViewsExample2Description)
                .WithOptions([options.Project, options.ModelName, options.PageType])
                .WithStep<ValidateViewsStep>(config =>
                {
                    var step = config.Step;
                    var context = config.Context;
                    step.Project = context.GetOptionResult(options.Project);
                    step.Model = context.GetOptionResult(options.ModelName);
                    step.Page = context.GetOptionResult(options.PageType);
                })
                .WithViewsTextTemplatingStep()
                .WithViewsAddFileStep();

            _builder.AddScaffolder(ScaffolderCatagory.AspNet, AspnetStrings.Api.MinimalApi)
                .WithDisplayName(AspnetStrings.Api.MinimalApiDisplayName)
                .WithCategory(AspnetStrings.Catagories.API)
                .WithDescription(AspnetStrings.Api.MinimalApiDescription)
                .WithExample(AspnetStrings.Api.MinimalApiExample1, AspnetStrings.Api.MinimalApiExample1Description)
                .WithExample(AspnetStrings.Api.MinimalApiExample2, AspnetStrings.Api.MinimalApiExample2Description)
                .WithOptions([options.Project, options.ModelName, options.EndpointsClass, options.OpenApi, options.TypedResults, options.DataContextClass, options.DatabaseProvider, options.Prerelease])
                .WithStep<ValidateMinimalApiStep>(config =>
                {
                    var step = config.Step;
                    var context = config.Context;
                    step.Project = context.GetOptionResult(options.Project);
                    step.Model = context.GetOptionResult(options.ModelName);
                    step.DataContext = context.GetOptionResult(options.DataContextClass);
                    step.DatabaseProvider = context.GetOptionResult(options.DatabaseProvider);
                    step.Prerelease = context.GetOptionResult(options.Prerelease);
                    step.OpenApi = context.GetOptionResult(options.OpenApi);
                    step.TypedResults = context.GetOptionResult(options.TypedResults);
                    step.Endpoints = context.GetOptionResult(options.EndpointsClass);
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
                .WithExample(AspnetStrings.Area.AreaExample, AspnetStrings.Area.AreaExampleDescription)
                .WithOptions([options.Project, options.AreaName])
                .WithStep<AreaScaffolderStep>(config =>
                {
                    var step = config.Step;
                    var context = config.Context;
                    step.Project = context.GetOptionResult(options.Project);
                    step.Name = context.GetOptionResult(options.AreaName);
                });

            _builder.AddScaffolder(ScaffolderCatagory.AspNet, AspnetStrings.Blazor.Identity)
                .WithDisplayName(AspnetStrings.Blazor.IdentityDisplayName)
                .WithCategory(AspnetStrings.Catagories.Blazor)
                .WithCategory(AspnetStrings.Catagories.Identity)
                .WithDescription(AspnetStrings.Blazor.IdentityDescription)
                .WithExample(AspnetStrings.Blazor.IdentityExample, AspnetStrings.Blazor.IdentityExampleDescription)
                .WithOptions([options.Project, options.DataContextClassRequired, options.IdentityDbProviderRequired, options.Overwrite, options.Prerelease])
                .WithStep<ValidateIdentityStep>(config =>
                {
                    var step = config.Step;
                    var context = config.Context;
                    step.Project = context.GetOptionResult(options.Project);
                    step.DataContext = context.GetOptionResult(options.DataContextClassRequired);
                    step.DatabaseProvider = context.GetOptionResult(options.IdentityDbProviderRequired);
                    step.Prerelease = context.GetOptionResult(options.Prerelease);
                    step.Overwrite = context.GetOptionResult(options.Overwrite);
                    step.BlazorScenario = true;
                })
                .WithBlazorIdentityAddPackagesStep()
                .WithIdentityDbContextStep()
                .WithAspNetConnectionStringStep()
                .WithBlazorIdentityTextTemplatingStep()
                .WithBlazorIdentityStaticFilesStep()
                .WithBlazorIdentityCodeChangeStep();

            _builder.AddScaffolder(ScaffolderCatagory.AspNet, AspnetStrings.Identity.Name)
                .WithDisplayName(AspnetStrings.Identity.DisplayName)
                .WithCategory(AspnetStrings.Catagories.Identity)
                .WithDescription(AspnetStrings.Identity.Description)
                .WithExample(AspnetStrings.Identity.IdentityExample1, AspnetStrings.Identity.IdentityExample1Description)
                .WithExample(AspnetStrings.Identity.IdentityExample2, AspnetStrings.Identity.IdentityExample2Description)
                .WithOptions([options.Project, options.DataContextClassRequired, options.IdentityDbProviderRequired, options.Overwrite, options.Prerelease])
                .WithStep<ValidateIdentityStep>(config =>
                {
                    var step = config.Step;
                    var context = config.Context;
                    step.Project = context.GetOptionResult(options.Project);
                    step.DataContext = context.GetOptionResult(options.DataContextClassRequired);
                    step.DatabaseProvider = context.GetOptionResult(options.IdentityDbProviderRequired);
                    step.Prerelease = context.GetOptionResult(options.Prerelease);
                    step.Overwrite = context.GetOptionResult(options.Overwrite);
                })
                .WithIdentityAddPackagesStep()
                .WithIdentityDbContextStep()
                .WithAspNetConnectionStringStep()
                .WithIdentityTextTemplatingStep()
                .WithIdentityCodeChangeStep();

            _builder.AddScaffolder(ScaffolderCatagory.AspNet, AspnetStrings.EntraId.Name)
                    .WithDisplayName(AspnetStrings.EntraId.DisplayName)
                    .WithCategory(AspnetStrings.Catagories.EntraId)
                    .WithDescription(AspnetStrings.EntraId.Description)
                    .WithExample(AspnetStrings.EntraId.EntraIdExample, AspnetStrings.EntraId.EntraIdExampleDescription)
                    .WithOptions([options.Username, options.Project, options.TenantId, options.Application, options.SelectApplication])
                    .WithStep<ValidateEntraIdStep>(config =>
                    {
                        var step = config.Step;
                        var context = config.Context;
                        step.Username = context.GetOptionResult(options.Username);
                        step.Project = context.GetOptionResult(options.Project);
                        step.TenantId = context.GetOptionResult(options.TenantId);
                        step.Application = context.GetOptionResult(options.Application);
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
