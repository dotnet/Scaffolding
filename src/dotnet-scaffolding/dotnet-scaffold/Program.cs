// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.CommandLine;
using Microsoft.DotNet.Scaffolding.CodeModification;
using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.ComponentModel;
using Microsoft.DotNet.Scaffolding.Core.Hosting;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.Aspire.Helpers;
using Microsoft.DotNet.Tools.Scaffold.Aspire.ScaffoldSteps;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.DotNet.Tools.Scaffold.Command;
using Microsoft.DotNet.Tools.Scaffold.Interactive.AppBuilder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;

IScaffoldRunnerBuilder builder = Host.CreateScaffoldBuilder();

ConfigureServices(builder.Services);
ConfigureSteps(builder.Services);

System.Diagnostics.Debugger.Launch();

ScaffolderOption<bool> nonInteractiveScaffoldOption = GetNonInteractiveOption();
builder.AddOption(nonInteractiveScaffoldOption);
Option nonInteractiveOption = nonInteractiveScaffoldOption.ToCliOption();

//Begin aspire migrated

CreateOptionsForAspire(out var cachingTypeOption, out var databaseTypeOption, out var storageTypeOption,
              out var appHostProjectOption, out var projectOption, out var prereleaseOption);

var caching = builder.AddScaffolder(ScaffolderCatagory.Aspire, "caching");
caching.WithCategory("Aspire")
       .WithDescription("Modified Aspire project to make it caching ready.")
       .WithOption(cachingTypeOption)
       .WithOption(appHostProjectOption)
       .WithOption(projectOption)
       .WithOption(prereleaseOption)
       .WithStep<ValidateOptionsStep>(config =>
       {
           config.Step.ValidateMethod = ValidationHelper.ValidateCachingSettings;
       })
       .WithCachingAddPackageSteps()
       .WithCachingCodeModificationSteps();

var database = builder.AddScaffolder(ScaffolderCatagory.Aspire, "database");
database.WithCategory("Aspire")
        .WithDescription("Modifies Aspire project to make it database ready.")
        .WithOption(databaseTypeOption)
        .WithOption(appHostProjectOption)
        .WithOption(projectOption)
        .WithOption(prereleaseOption)
        .WithStep<ValidateOptionsStep>(config =>
        {
            config.Step.ValidateMethod = ValidationHelper.ValidateDatabaseSettings;
        })
        .WithDatabaseAddPackageSteps()
        .WithDbContextStep()
        .WithAspireConnectionStringStep()
        .WithDatabaseCodeModificationSteps();

var storage = builder.AddScaffolder(ScaffolderCatagory.Aspire, "storage");
storage.WithCategory("Aspire")
       .WithDescription("Modifies Aspire project to make it storage ready.")
       .WithOption(storageTypeOption)
       .WithOption(appHostProjectOption)
       .WithOption(projectOption)
       .WithOption(prereleaseOption)
       .WithStep<ValidateOptionsStep>(config =>
       {
           config.Step.ValidateMethod = ValidationHelper.ValidateStorageSettings;
       })
       .WithStorageAddPackageSteps()
       .WithStorageCodeModificationSteps();

//end aspire migrated
//begin aspnet migrated
CreateOptionsForAspNet(
    out var aspNetProjectOption,
    out var aspNetPrereleaseOption,
    out var fileNameOption,
    out var actionsOption,
    out var areaNameOption,
    out var modelNameOption,
    out var endpointsClassOption,
    out var databaseProviderOption,
    out var databaseProviderRequiredOption,
    out var identityDbProviderRequiredOption,
    out var dataContextClassOption,
    out var dataContextClassRequiredOption,
    out var openApiOption,
    out var pageTypeOption,
    out var controllerNameOption,
    out var viewsOption,
    out var overwriteOption,
    out var usernameOption,
    out var tenantIdOption,
    out var applicationOption,
    out var selectApplicationOption,
    out bool areAzCliCommandsSuccessful
    );

builder.AddScaffolder(ScaffolderCatagory.AspNet, "blazor-empty")
            .WithDisplayName("Razor Component")
            .WithCategory("Blazor")
            .WithDescription("Add an empty razor component to a given project")
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
builder.AddScaffolder(ScaffolderCatagory.AspNet, "razorview-empty")
            .WithDisplayName("Razor View - Empty")
            .WithCategory("MVC")
            .WithDescription("Add an empty razor view to a given project")
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
builder.AddScaffolder(ScaffolderCatagory.AspNet, "razorpage-empty")
           .WithDisplayName("Razor Page - Empty")
           .WithCategory("Razor Pages")
           .WithDescription("Add an empty razor page to a given project")
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

builder.AddScaffolder(ScaffolderCatagory.AspNet, "apicontroller")
    .WithDisplayName("API Controller")
    .WithCategory("API")
    .WithDescription("Add an empty API Controller to a given project")
    .WithOptions([projectOption, fileNameOption, actionsOption])
    .WithStep<EmptyControllerScaffolderStep>(config =>
    {
        var step = config.Step;
        var context = config.Context;
        step.ProjectPath = context.GetOptionResult(projectOption);
        step.FileName = context.GetOptionResult(fileNameOption);
        step.Actions = context.GetOptionResult(actionsOption);
        step.CommandName = "apicontroller";
    });

builder.AddScaffolder(ScaffolderCatagory.AspNet, "mvccontroller")
    .WithDisplayName("MVC Controller")
    .WithCategory("MVC")
    .WithDescription("Add an empty MVC Controller to a given project")
    .WithOptions([projectOption, fileNameOption, actionsOption])
    .WithStep<EmptyControllerScaffolderStep>(config =>
    {
        var step = config.Step;
        var context = config.Context;
        step.ProjectPath = context.GetOptionResult(projectOption);
        step.FileName = context.GetOptionResult(fileNameOption);
        step.Actions = context.GetOptionResult(actionsOption);
        step.CommandName = "mvccontroller";
    });

builder.AddScaffolder(ScaffolderCatagory.AspNet, "apicontroller-crud")
    .WithDisplayName("API Controller with actions, using Entity Framework (CRUD)")
    .WithCategory("API")
    .WithDescription("Create an API controller with REST actions to create, read, update, delete, and list entities")
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
        step.ControllerType = "API";
        step.ControllerName = context.GetOptionResult(controllerNameOption);
    })
    .WithEfControllerAddPackagesStep()
    .WithDbContextStep()
    .WithAspNetConnectionStringStep()
    .WithEfControllerTextTemplatingStep()
    .WithEfControllerCodeChangeStep();

builder.AddScaffolder(ScaffolderCatagory.AspNet, "mvccontroller-crud")
    .WithDisplayName("MVC Controller with views, using Entity Framework (CRUD)")
    .WithCategory("MVC")
    .WithDescription("Create a MVC controller with read/write actions and views using Entity Framework")
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
        step.ControllerType = "MVC";
        step.ControllerName = context.GetOptionResult(controllerNameOption);
    })
    .WithEfControllerAddPackagesStep()
    .WithDbContextStep()
    .WithAspNetConnectionStringStep()
    .WithEfControllerTextTemplatingStep()
    .WithEfControllerCodeChangeStep()
    .WithMvcViewsStep();

builder.AddScaffolder(ScaffolderCatagory.AspNet, "blazor-crud")
    .WithDisplayName("Razor Components with EntityFrameworkCore (CRUD)")
    .WithCategory("Blazor")
    .WithDescription("Generates Razor Components using Entity Framework for Create, Delete, Details, Edit and List operations for the given model")
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

builder.AddScaffolder(ScaffolderCatagory.AspNet, "razorpages-crud")
    .WithDisplayName("Razor Pages with Entity Framework (CRUD)")
    .WithCategory("Razor Pages")
    .WithDescription("Generates Razor pages using Entity Framework for Create, Delete, Details, Edit and List operations for the given model")
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

builder.AddScaffolder(ScaffolderCatagory.AspNet, "views")
    .WithDisplayName("Razor Views")
    .WithCategory("MVC")
    .WithDescription("Generates Razor views for Create, Delete, Details, Edit and List operations for the given model")
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

builder.AddScaffolder(ScaffolderCatagory.AspNet, "minimalapi")
    .WithDisplayName("Minimal API")
    .WithCategory("API")
    .WithDescription("Generates an endpoints file (with CRUD API endpoints) given a model and optional DbContext.")
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

builder.AddScaffolder(ScaffolderCatagory.AspNet, "area")
    .WithDisplayName("Area")
    .WithCategory("MVC")
    .WithDescription("Creates a MVC Area folder structure.")
    .WithOptions([projectOption, areaNameOption])
    .WithStep<AreaScaffolderStep>(config =>
    {
        var step = config.Step;
        var context = config.Context;
        step.Project = context.GetOptionResult(projectOption);
        step.Name = context.GetOptionResult(areaNameOption);
    });

builder.AddScaffolder(ScaffolderCatagory.AspNet, "blazor-identity")
            .WithDisplayName("Blazor Identity")
            .WithCategory("Blazor")
            .WithCategory("Identity")
            .WithDescription("Add blazor identity to a project.")
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

builder.AddScaffolder(ScaffolderCatagory.AspNet, "identity")
    .WithDisplayName("ASP.NET Core Identity")
    .WithCategory("Identity")
    .WithDescription("Add ASP.NET Core identity to a project.")
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
    builder.AddScaffolder(ScaffolderCatagory.AspNet, "entra-id")
        .WithDisplayName("Entra ID")
        .WithCategory("Entra ID")
        .WithDescription("Add Entra auth")
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

IScaffoldRunner runner = builder.Build();

// add handler for routing the interactive tool through the Spectre.Console experience, all others
// are routed through System.CommandLine experience
builder.AddHandler(async (context) =>
{
    var nonInteractive = context.ParseResult.GetValue(nonInteractiveOption);
    if (nonInteractive is true)
    {
        context.Console.WriteLine("Non-Interactive mode is not yet implemented. Use \"dotnet scaffold\" for the interactive experience.");
        context.ExitCode = 1;
    }
    else
    {
        var builder = new ScaffoldCommandAppBuilder(context.ParseResult.Tokens.Select(t => t.Value).ToArray());
        var app = builder.Build();
        await app.RunAsync();
    }
});

var telemetryWrapper = builder.ServiceProvider?.GetRequiredService<IFirstPartyToolTelemetryWrapper>();
telemetryWrapper?.ConfigureFirstTimeTelemetry();
await runner.RunAsync(args);
telemetryWrapper?.Flush();

static void ConfigureServices(IServiceCollection services)
{
    services.AddSingleton<IFileSystem, FileSystem>();
    services.AddSingleton<IEnvironmentService, EnvironmentService>();
    services.AddTelemetry("dotnetScaffoldAspire"); //figure out the telemetry here
    services.AddSingleton<IFirstPartyToolTelemetryWrapper, FirstPartyToolTelemetryWrapper>();
}

static void ConfigureSteps(IServiceCollection services)
{
    var executingAssembly = System.Reflection.Assembly.GetExecutingAssembly();
    var scaffoldStepTypes = executingAssembly.GetTypes().Where(x => x.IsSubclassOf(typeof(ScaffoldStep)));
    foreach (var type in scaffoldStepTypes)
    {
        services.AddTransient(type);
    }

    services.AddTransient<CodeModificationStep>();
    services.AddTransient<AddAspireCodeChangeStep>();
    services.AddTransient<ValidateOptionsStep>();
    services.AddTransient<AddPackagesStep>();
    services.AddTransient<AspNetWrappedAddPackagesStep>();
    services.AddTransient<AspireWrappedAddPackagesStep>();
    services.AddTransient<Microsoft.DotNet.Scaffolding.TextTemplating.TextTemplatingStep>();
    services.AddTransient<Microsoft.DotNet.Scaffolding.Core.Steps.AddConnectionStringStep>();
}

static void CreateOptionsForAspire(out ScaffolderOption<string> cachingTypeOption, out ScaffolderOption<string> databaseTypeOption, out ScaffolderOption<string> storageTypeOption,
                          out ScaffolderOption<string> appHostProjectOption, out ScaffolderOption<string> projectOption, out ScaffolderOption<bool> prereleaseOption)
{
    cachingTypeOption = new ScaffolderOption<string>
    {
        DisplayName = "Caching type",
        CliOption = AspireCommandHelpers.TypeCliOption,
        Description = "Types of caching",
        Required = true,
        PickerType = InteractivePickerType.CustomPicker,
        CustomPickerValues = AspireCommandHelpers.CachingTypeCustomValues
    };

    databaseTypeOption = new ScaffolderOption<string>
    {
        DisplayName = "Database type",
        CliOption = AspireCommandHelpers.TypeCliOption,
        Description = "Types of database",
        Required = true,
        PickerType = InteractivePickerType.CustomPicker,
        CustomPickerValues = AspireCommandHelpers.DatabaseTypeCustomValues
    };

    storageTypeOption = new ScaffolderOption<string>
    {
        DisplayName = "Storage type",
        CliOption = AspireCommandHelpers.TypeCliOption,
        Description = "Types of storage",
        Required = true,
        PickerType = InteractivePickerType.CustomPicker,
        CustomPickerValues = AspireCommandHelpers.StorageTypeCustomValues
    };

    appHostProjectOption = new ScaffolderOption<string>
    {
        DisplayName = "Aspire App host project file",
        CliOption = AspireCommandHelpers.AppHostCliOption,
        Description = "Aspire App host project for the scaffolding",
        Required = true,
        PickerType = InteractivePickerType.ProjectPicker
    };

    projectOption = new ScaffolderOption<string>
    {
        DisplayName = "Web or worker project file",
        CliOption = AspireCommandHelpers.WorkerProjectCliOption,
        Description = "Web or worker project associated with the Aspire App host",
        Required = true,
        PickerType = InteractivePickerType.ProjectPicker
    };

    prereleaseOption = new ScaffolderOption<bool>
    {
        DisplayName = "Include Prerelease packages?",
        CliOption = AspireCommandHelpers.PrereleaseCliOption,
        Description = "Include prerelease package versions when installing latest Aspire components",
        Required = false,
        PickerType = InteractivePickerType.YesNo
    };
}

//end aspire migration

static ScaffolderOption<bool> GetNonInteractiveOption()
{
    return new ScaffolderOption<bool>
    {
        DisplayName = "Run scaffolder in non-Interactive mode",
        CliOption = CliOptions.NonInteractiveCliOption,
        Required = false
    };
}

static void CreateOptionsForAspNet(
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
        out bool areAzCliCommandsSuccessful
        )
{
    areAzCliCommandsSuccessful = AzCliHelper.GetAzureInformation(out List<string> usernames, out List<string> tenants, out List<string> appIds);

    projectOption = new ScaffolderOption<string>
    {
        DisplayName = ".NET project file",
        CliOption = Constants.CliOptions.ProjectCliOption,
        Description = ".NET project to be used for scaffolding (.csproj file)",
        Required = true,
        PickerType = InteractivePickerType.ProjectPicker
    };

    prereleaseOption = new ScaffolderOption<bool>
    {
        DisplayName = "Include Prerelease packages?",
        CliOption = Constants.CliOptions.PrereleaseCliOption,
        Description = "Include prerelease package versions when installing latest Aspire components",
        Required = false,
        PickerType = InteractivePickerType.YesNo
    };

    fileNameOption = new ScaffolderOption<string>
    {
        DisplayName = "File name",
        CliOption = Constants.CliOptions.NameOption,
        Description = "File name for new file being created with 'dotnet new'",
        Required = true,
    };

    actionsOption = new ScaffolderOption<bool>
    {
        DisplayName = "Read/Write Actions?",
        CliOption = Constants.CliOptions.ActionsOption,
        Description = "Create controller with read/write actions?",
        Required = true,
        PickerType = InteractivePickerType.YesNo
    };

    controllerNameOption = new ScaffolderOption<string>
    {
        DisplayName = "Controller Name",
        CliOption = Constants.CliOptions.ControllerNameOption,
        Description = "Name for the controller being created",
        Required = true
    };

    areaNameOption = new ScaffolderOption<string>
    {
        DisplayName = "Area Name",
        CliOption = Constants.CliOptions.NameOption,
        Description = "Name for the area being created",
        Required = true
    };

    modelNameOption = new ScaffolderOption<string>
    {
        DisplayName = "Model Name",
        CliOption = Constants.CliOptions.ModelCliOption,
        Description = "Name for the model class to be used for scaffolding",
        Required = true,
        PickerType = InteractivePickerType.ClassPicker
    };

    endpointsClassOption = new ScaffolderOption<string>
    {
        DisplayName = "Endpoints File Name",
        CliOption = Constants.CliOptions.EndpointsOption,
        Description = "",
        Required = true
    };

    dataContextClassOption = new ScaffolderOption<string>
    {
        DisplayName = "Data Context Class",
        CliOption = Constants.CliOptions.DataContextOption,
        Description = "",
        Required = false
    };

    dataContextClassRequiredOption = new ScaffolderOption<string>
    {
        DisplayName = "Data Context Class",
        CliOption = Constants.CliOptions.DataContextOption,
        Description = "",
        Required = true
    };

    openApiOption = new ScaffolderOption<bool>
    {
        DisplayName = "Open API Enabled",
        CliOption = Constants.CliOptions.OpenApiOption,
        Description = "",
        Required = false,
        PickerType = InteractivePickerType.YesNo
    };

    databaseProviderOption = new ScaffolderOption<string>
    {
        DisplayName = "Database Provider",
        CliOption = Constants.CliOptions.DbProviderOption,
        Description = "",
        Required = false,
        PickerType = InteractivePickerType.CustomPicker,
        CustomPickerValues = AspNetDbContextHelper.DbContextTypeDefaults.Keys.ToArray()
    };

    databaseProviderRequiredOption = new ScaffolderOption<string>
    {
        DisplayName = "Database Provider",
        CliOption = Constants.CliOptions.DbProviderOption,
        Description = "",
        Required = true,
        PickerType = InteractivePickerType.CustomPicker,
        CustomPickerValues = AspNetDbContextHelper.DbContextTypeDefaults.Keys.ToArray()
    };

    identityDbProviderRequiredOption = new ScaffolderOption<string>
    {
        DisplayName = "Database Provider",
        CliOption = Constants.CliOptions.DbProviderOption,
        Description = "",
        Required = true,
        PickerType = InteractivePickerType.CustomPicker,
        CustomPickerValues = AspNetDbContextHelper.IdentityDbContextTypeDefaults.Keys.ToArray()
    };

    pageTypeOption = new ScaffolderOption<string>
    {
        DisplayName = "Page Type",
        CliOption = Constants.CliOptions.PageTypeOption,
        Description = "The CRUD page(s) to scaffold",
        Required = true,
        PickerType = InteractivePickerType.CustomPicker,
        CustomPickerValues = BlazorCrudHelper.CRUDPages
    };

    viewsOption = new ScaffolderOption<bool>
    {
        DisplayName = "With Views?",
        CliOption = Constants.CliOptions.ViewsOption,
        Description = "Add CRUD razor views (.cshtml)",
        Required = true,
        PickerType = InteractivePickerType.YesNo
    };

    overwriteOption = new ScaffolderOption<bool>
    {
        DisplayName = "Overwrite existing files?",
        CliOption = Constants.CliOptions.OverwriteOption,
        Description = "Option to enable overwriting existing files",
        Required = true,
        PickerType = InteractivePickerType.YesNo
    };

    usernameOption = new ScaffolderOption<string>
    {
        DisplayName = "Select username",
        CliOption = Constants.CliOptions.UsernameOption,
        Description = "User name for the identity user",
        Required = true,
        PickerType = InteractivePickerType.CustomPicker,
        CustomPickerValues = usernames
    };

    tenantIdOption = new ScaffolderOption<string>
    {
        DisplayName = "Tenant Id",
        CliOption = Constants.CliOptions.TenantIdOption,
        Description = "Tenant Id for the identity user",
        Required = true,
        PickerType = InteractivePickerType.CustomPicker,
        CustomPickerValues = tenants
    };

    applicationOption = new ScaffolderOption<string>
    {
        DisplayName = "Create or Select Application",
        Description = "Create or select existing application",
        Required = true,
        PickerType = InteractivePickerType.ConditionalPicker,
        CustomPickerValues = new[] { "Select an existing Azure application object", "Create a new Azure application object" }
    };

    selectApplicationOption = new ScaffolderOption<string>
    {
        DisplayName = "Select Application",
        CliOption = Constants.CliOptions.ApplicationIdOption,
        Description = "Select existing application",
        Required = false,
        PickerType = InteractivePickerType.CustomPicker,
        CustomPickerValues = appIds
    };
}
