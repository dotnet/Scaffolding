using Microsoft.DotNet.Scaffolding.ComponentModel;
using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.Hosting;
using Microsoft.DotNet.Scaffolding.Helpers.Services;
using Microsoft.DotNet.Scaffolding.Helpers.Services.Environment;
using Microsoft.DotNet.Scaffolding.Helpers.Steps;
using Microsoft.DotNet.Scaffolding.TextTemplating;
using Microsoft.DotNet.Tools.Scaffold.Aspire;
using Microsoft.DotNet.Tools.Scaffold.Aspire.CommandHelpers;
using Microsoft.DotNet.Tools.Scaffold.Aspire.Commands;
using Microsoft.DotNet.Tools.Scaffold.Aspire.ScaffoldSteps;
using Microsoft.Extensions.DependencyInjection;

var builder = Host.CreateScaffoldBuilder();

ConfigureServices(builder.Services);

CreateOptions(out var cachingTypeOption, out var databaseTypeOption, out var storageTypeOption,
              out var appHostProjectOption, out var projectOption, out var prereleaseOption);

var caching = builder.AddScaffolder("caching");
caching.WithCategory("Aspire")
       .WithDescription("Modified Aspire project to make it caching ready!")
       .WithOption(cachingTypeOption)
       .WithOption(appHostProjectOption)
       .WithOption(projectOption)
       .WithOption(prereleaseOption)
       .WithStep<ValidateContextStep>(config =>
       {
           config.Step.ValidateMethod = ValidationHelper.ValidateCachingSettings;
       })
       .WithCachingAddPackageSteps()
       .WithStep<PlaceholderCachingStep>(config =>
       {
           var step = config.Step;
           var context = config.Context;

           step.Type = context.GetOptionResult(cachingTypeOption);
           step.AppHostProject = context.GetOptionResult(appHostProjectOption);
           step.Project = context.GetOptionResult(projectOption);
           step.Prerelease = context.GetOptionResult(prereleaseOption);
       });

var database = builder.AddScaffolder("database");
database.WithCategory("Aspire")
        .WithDescription("Modifies Aspire project to make it database ready!")
        .WithOption(databaseTypeOption)
        .WithOption(appHostProjectOption)
        .WithOption(projectOption)
        .WithOption(prereleaseOption)
        .WithStep<ValidateContextStep>(config =>
        {
            config.Step.ValidateMethod = ValidationHelper.ValidateDatabaseSettings;
        })
        .WithDatabaseAddPackageSteps()
        .WithStep<PlaceholderDatabaseStep>(config =>
        {
            var step = config.Step;
            var context = config.Context;

            step.Type = context.GetOptionResult(databaseTypeOption);
            step.AppHostProject = context.GetOptionResult(appHostProjectOption);
            step.Project = context.GetOptionResult(projectOption);
            step.Prerelease = context.GetOptionResult(prereleaseOption);
        })
        .WithDbContextStep()
        .WithConnectionStringStep();

var storage = builder.AddScaffolder("storage");
storage.WithCategory("Aspire")
       .WithDescription("Modifies Aspire project to make it storage ready!")
       .WithOption(storageTypeOption)
       .WithOption(appHostProjectOption)
       .WithOption(projectOption)
       .WithOption(prereleaseOption)
       .WithStep<ValidateContextStep>(config =>
       {
           config.Step.ValidateMethod = ValidationHelper.ValidateStorageSettings;
       })
       .WithDatabaseAddPackageSteps()
       .WithStep<PlaceholderStorageStep>(config =>
       {
           var step = config.Step;
           var context = config.Context;

           step.Type = context.GetOptionResult(storageTypeOption);
           step.AppHostProject = context.GetOptionResult(appHostProjectOption);
           step.Project = context.GetOptionResult(projectOption);
           step.Prerelease = context.GetOptionResult(prereleaseOption);
       });

var runner = builder.Build();

runner.RunAsync(args).Wait();

//TODO separate adding transient steps from singleton services.
static void ConfigureServices(IServiceCollection services)
{
    services.AddTransient<AddPackagesStep>();
    services.AddTransient<TextTemplatingStep>();
    services.AddTransient<AddConnectionStringStep>();
    services.AddTransient<PlaceholderCachingStep>();
    services.AddTransient<PlaceholderDatabaseStep>();
    services.AddTransient<PlaceholderStorageStep>();
    services.AddSingleton<IFileSystem, FileSystem>();
    services.AddSingleton<IEnvironmentService, EnvironmentService>();
    services.AddSingleton<IDotNetToolService, DotNetToolService>();
}

static void CreateOptions(out ScaffolderOption<string> cachingTypeOption, out ScaffolderOption<string> databaseTypeOption, out ScaffolderOption<string> storageTypeOption,
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
