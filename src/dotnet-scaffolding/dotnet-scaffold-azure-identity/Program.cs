using Microsoft.DotNet.Scaffolding.CodeModification;
using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.ComponentModel;
using Microsoft.DotNet.Scaffolding.Core.Hosting;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Scaffolding.TextTemplating;
using Microsoft.DotNet.Tools.Scaffold.Azure.Identity.Helpers;
using Microsoft.DotNet.Tools.Scaffold.Azure.Identity.ScaffoldSteps;
using Microsoft.Extensions.DependencyInjection;

var builder = Host.CreateScaffoldBuilder();
ConfigureServices(builder.Services);
ConfigureSteps(builder.Services);

CreateOptions(out var blazorWebAppProjectOption, out var blazorWebClientAppProjectOption, out var prereleaseOption);

var caching = builder.AddScaffolder("entra");
caching.WithCategory("Azure")
       .WithDescription("Modified Aspire project to make it caching ready.")
       .WithOption(blazorWebAppProjectOption)
       .WithOption(blazorWebClientAppProjectOption)
       .WithOption(prereleaseOption);
       //.WithStep<ValidateOptionsStep>(config =>
       //{
       //    config.Step.ValidateMethod = ValidationHelper.ValidateCachingSettings;
       //})
       //.WithCachingAddPackageSteps()
       //.WithCachingCodeModificationSteps();

var runner = builder.Build();
var telemetryWrapper = builder.ServiceProvider?.GetRequiredService<IFirstPartyToolTelemetryWrapper>();
telemetryWrapper?.ConfigureFirstTimeTelemetry();
runner.RunAsync(args).Wait();
telemetryWrapper?.Flush();

static void ConfigureServices(IServiceCollection services)
{
    services.AddSingleton<IFileSystem, FileSystem>();
    services.AddSingleton<IEnvironmentService, EnvironmentService>();
    services.AddTelemetry("dotnetScaffoldAzureIdentity");
    services.AddSingleton<IFirstPartyToolTelemetryWrapper, FirstPartyToolTelemetryWrapper>();
}

static void ConfigureSteps(IServiceCollection services)
{
    services.AddTransient<CodeModificationStep>();
    services.AddTransient<WrappedCodeModificationStep>();
    //services.AddTransient<ValidateOptionsStep>();
    services.AddTransient<AddPackagesStep>();
    services.AddTransient<WrappedAddPackagesStep>();
    services.AddTransient<TextTemplatingStep>();
}

static void CreateOptions(out ScaffolderOption<string> blazorWebAppProjectOption, out ScaffolderOption<string> blazorWebClientAppProjectOption, out ScaffolderOption<bool> prereleaseOption)
{
    blazorWebAppProjectOption = new ScaffolderOption<string>
    {
        DisplayName = "Blazr web app project file",
        CliOption = CommandHelpers.BlazorWebProjectCliOption,
        Description = "Aspire App host project for the scaffolding",
        Required = true,
        PickerType = InteractivePickerType.ProjectPicker
    };

    blazorWebClientAppProjectOption = new ScaffolderOption<string>
    {
        DisplayName = "Blazr web client app project file",
        CliOption = CommandHelpers.BlazorWebClientProjectCliOption,
        Description = "Aspire App host project for the scaffolding",
        Required = false,
        PickerType = InteractivePickerType.ProjectPicker
    };

    prereleaseOption = new ScaffolderOption<bool>
    {
        DisplayName = "Include Prerelease packages?",
        CliOption = CommandHelpers.PrereleaseCliOption,
        Description = "Include prerelease package versions when installing latest Aspire components",
        Required = false,
        PickerType = InteractivePickerType.YesNo
    };
}
