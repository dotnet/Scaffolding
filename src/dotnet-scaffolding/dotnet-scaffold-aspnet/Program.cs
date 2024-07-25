using System.Linq;
using Microsoft.DotNet.Scaffolding.ComponentModel;
using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.Hosting;
using Microsoft.DotNet.Scaffolding.Helpers.Services;
using Microsoft.DotNet.Scaffolding.Helpers.Services.Environment;
using Microsoft.DotNet.Scaffolding.Helpers.Steps;
using Microsoft.DotNet.Scaffolding.TextTemplating;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Commands.Blazor.BlazorCrud;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet;
public static class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateScaffoldBuilder();
        ConfigureServices(builder.Services);
        CreateOptions(
            out var projectOption, out var prereleaseOption, out var fileNameOption, out var actionsOption,
            out var areaNameOption, out var modelNameOption, out var endpointsClassOption, out var databaseProviderOption,
            out var databaseProviderRequiredOption, out var dataContextClassOption, out var dataContextClassRequiredOption,
            out var openApiOption, out var pageTypeOption);

        builder.AddScaffolder("blazor-empty")
            .WithDisplayName("Razor Component - Empty")
            .WithCategory("Blazor")
            .WithDescription("Add an empty razor component to a given project")
            .WithOption(projectOption)
            .WithOption(fileNameOption)
            .WithStep<DotnetNewScaffolderStep>(config =>
            {
                var step = config.Step;
                var context = config.Context;
                step.Project = context.GetOptionResult(projectOption);
                step.Name = context.GetOptionResult(fileNameOption);
                step.CommandName = "razorcomponent";
            });

        builder.AddScaffolder("razorview-empty")
            .WithDisplayName("Razor View - Empty")
            .WithCategory("MVC")
            .WithDescription("Add an empty razor view to a given project")
            .WithOption(projectOption)
            .WithOption(fileNameOption)
            .WithStep<DotnetNewScaffolderStep>(config =>
            {
                var step = config.Step;
                var context = config.Context;
                step.Project = context.GetOptionResult(projectOption);
                step.Name = context.GetOptionResult(fileNameOption);
                step.CommandName = "view";
            });

        builder.AddScaffolder("razorpage-empty")
            .WithDisplayName("Razor Page - Empty")
            .WithCategory("Razor Pages")
            .WithDescription("Add an empty razor page to a given project")
            .WithOption(projectOption)
            .WithOption(fileNameOption)
            .WithStep<DotnetNewScaffolderStep>(config =>
            {
                var step = config.Step;
                var context = config.Context;
                step.Project = context.GetOptionResult(projectOption);
                step.Name = context.GetOptionResult(fileNameOption);
                step.CommandName = "page";
            });

        builder.AddScaffolder("apicontroller-empty")
            .WithDisplayName("API Controller - Empty")
            .WithCategory("API")
            .WithDescription("Add an empty API Controller to a given project")
            .WithOptions([projectOption, fileNameOption, actionsOption])
            .WithStep<EmptyControllerScaffolderStep>(config =>
            {
                var step = config.Step;
                var context = config.Context;
                step.Project = context.GetOptionResult(projectOption);
                step.Name = context.GetOptionResult(fileNameOption);
                step.Actions = context.GetOptionResult(actionsOption);
                step.CommandName = "apicontroller";
            });

        builder.AddScaffolder("mvccontroller-empty")
            .WithDisplayName("MVC Controller - Empty")
            .WithCategory("MVC")
            .WithDescription("Add an empty MVC Controller to a given project")
            .WithOptions([projectOption, fileNameOption, actionsOption])
            .WithStep<EmptyControllerScaffolderStep>(config =>
            {
                var step = config.Step;
                var context = config.Context;
                step.Project = context.GetOptionResult(projectOption);
                step.Name = context.GetOptionResult(fileNameOption);
                step.Actions = context.GetOptionResult(actionsOption);
                step.CommandName = "mvccontroller";
            });

        builder.AddScaffolder("blazor-crud")
            .WithDisplayName("Razor Components w/ EF (CRUD)")
            .WithCategory("Blazor")
            .WithDescription("Generates Razor Components using Entity Framework for Create, Delete, Details, Edit and List operations for the given model")
            .WithOptions([projectOption, modelNameOption, dataContextClassRequiredOption, databaseProviderRequiredOption, pageTypeOption, prereleaseOption])
            .WithStep<BlazorCrudScaffolderStep>(config =>
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
            .WithDbContextStep()
            .WithConnectionStringStep()
            .WithBlazorCrudTextTemplatingStep();

        builder.AddScaffolder("minimalapi")
            .WithDisplayName("Minimal API")
            .WithCategory("API")
            .WithDescription("Generates an endpoints file (with CRUD API endpoints) given a model and optional DbContext.")
            .WithOptions([projectOption, modelNameOption, endpointsClassOption, openApiOption, dataContextClassOption, databaseProviderOption, prereleaseOption])
            .WithStep<MinimalApiScaffolderStep>(config =>
            {
                var step = config.Step;
                var context = config.Context;
                step.Project = context.GetOptionResult(projectOption);
                step.Model = context.GetOptionResult(modelNameOption);
                step.Endpoints = context.GetOptionResult(endpointsClassOption);
                step.OpenApi = context.GetOptionResult(openApiOption);
                step.DataContext = context.GetOptionResult(dataContextClassOption);
                step.DatabaseProvider = context.GetOptionResult(databaseProviderOption);
                step.Prerelease = context.GetOptionResult(prereleaseOption);
            })
            .WithDbContextStep()
            .WithConnectionStringStep()
            .WithMinimalApiTextTemplatingStep();

        builder.AddScaffolder("area")
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

        var runner = builder.Build();
        runner.RunAsync(args).Wait();
    }

    //TODO separate adding transient steps from singleton services.
    static void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient<AddConnectionStringStep>();
        services.AddTransient<TextTemplatingStep>();
        services.AddTransient<AreaScaffolderStep>();
        services.AddTransient<DotnetNewScaffolderStep>();
        services.AddTransient<EmptyControllerScaffolderStep>();
        services.AddTransient<MinimalApiScaffolderStep>();
        services.AddTransient<BlazorCrudScaffolderStep>();
        services.AddSingleton<IFileSystem, FileSystem>();
        services.AddSingleton<IEnvironmentService, EnvironmentService>();
        services.AddSingleton<IDotNetToolService, DotNetToolService>();
    }


    static void CreateOptions(
        out ScaffolderOption<string> projectOption,
        out ScaffolderOption<bool> prereleaseOption,
        out ScaffolderOption<string> fileNameOption,
        out ScaffolderOption<bool> actionsOption,
        out ScaffolderOption<string> areaNameOption,
        out ScaffolderOption<string> modelNameOption,
        out ScaffolderOption<string> endpointsClassOption,
        out ScaffolderOption<string> databaseProviderOption,
        out ScaffolderOption<string> databaseProviderRequiredOption,
        out ScaffolderOption<string> dataContextClassOption,
        out ScaffolderOption<string> dataContextClassRequiredOption,
        out ScaffolderOption<bool> openApiOption,
        out ScaffolderOption<string> pageTypeOption)
    {
        projectOption = new ScaffolderOption<string>
        {
            DisplayName = ".NET project file",
            CliOption = "--project",
            Description = ".NET project to be used for scaffolding (.csproj file)",
            Required = true,
            PickerType = InteractivePickerType.ProjectPicker
        };

        prereleaseOption = new ScaffolderOption<bool>
        {
            DisplayName = "Include Prerelease packages?",
            CliOption = "--prerelease",
            Description = "Include prerelease package versions when installing latest Aspire components",
            Required = false,
            PickerType = InteractivePickerType.YesNo
        };

        fileNameOption = new ScaffolderOption<string>
        {
            DisplayName = "File name",
            CliOption = "--name",
            Description = "File name for new file being created with 'dotnet new'",
            Required = true,
        };

        actionsOption = new ScaffolderOption<bool>
        {
            DisplayName = "Read/Write Actions?",
            CliOption = "--actions",
            Description = "Create controller with read/write actions?",
            Required = true,
            PickerType = InteractivePickerType.YesNo
        };

        areaNameOption = new ScaffolderOption<string>
        {
            DisplayName = "Area Name",
            CliOption = "--name",
            Description = "Name for the area being created",
            Required = true
        };

        modelNameOption = new ScaffolderOption<string>
        {
            DisplayName = "Model Name",
            CliOption = "--model",
            Description = "Name for the model class to be used for scaffolding",
            Required = true,
            PickerType = InteractivePickerType.ClassPicker
        };

        endpointsClassOption = new ScaffolderOption<string>
        {
            DisplayName = "Endpoints File Name",
            CliOption = "--endpoints",
            Description = "",
            Required = true
        };

        dataContextClassOption = new ScaffolderOption<string>
        {
            DisplayName = "Data Context Class",
            CliOption = "--dataContext",
            Description = "",
            Required = false
        };

        dataContextClassRequiredOption = new ScaffolderOption<string>
        {
            DisplayName = "Data Context Class",
            CliOption = "--dataContext",
            Description = "",
            Required = true
        };

        openApiOption = new ScaffolderOption<bool>
        {
            DisplayName = "Open API Enabled",
            CliOption = "--open",
            Description = "",
            Required = false,
            PickerType = InteractivePickerType.YesNo
        };

        databaseProviderOption = new ScaffolderOption<string>
        {
            DisplayName = "Database Provider",
            CliOption = "--dbProvider",
            Description = "",
            Required = false,
            PickerType = InteractivePickerType.CustomPicker,
            CustomPickerValues = AspNetDbContextHelper.DbContextTypeDefaults.Keys.ToArray()
        };

        databaseProviderRequiredOption = new ScaffolderOption<string>
        {
            DisplayName = "Database Provider",
            CliOption = "--dbProvider",
            Description = "",
            Required = true,
            PickerType = InteractivePickerType.CustomPicker,
            CustomPickerValues = AspNetDbContextHelper.DbContextTypeDefaults.Keys.ToArray()
        };

        pageTypeOption = new ScaffolderOption<string>
        {
            DisplayName = "Page Type",
            CliOption = "--page",
            Description = "The CRUD page(s) to scaffold",
            Required = true,
            PickerType = InteractivePickerType.CustomPicker,
            CustomPickerValues = BlazorCrudHelper.CRUDPages
        };
    }
}

