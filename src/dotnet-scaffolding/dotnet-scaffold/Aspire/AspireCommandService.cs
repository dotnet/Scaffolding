// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.ComponentModel;
using Microsoft.DotNet.Scaffolding.Core.Hosting;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Tools.Scaffold.Aspire.Command;
using Microsoft.DotNet.Tools.Scaffold.Aspire.Helpers;
using Microsoft.DotNet.Tools.Scaffold.Aspire.ScaffoldSteps;
using Microsoft.DotNet.Tools.Scaffold.Command;

namespace Microsoft.DotNet.Tools.Scaffold.Aspire;

internal class AspireCommandService(IScaffoldRunnerBuilder builder) : ICommandService
{
    IScaffoldRunnerBuilder _builder = builder;

    public void AddScaffolderCommands()
    {
        CreateAspireOptions(out var cachingTypeOption,
            out var databaseTypeOption,
            out var storageTypeOption,
            out var appHostProjectOption,
            out var projectOption,
            out var prereleaseOption);

        IScaffoldBuilder caching = _builder.AddScaffolder(ScaffolderCatagory.Aspire, AspireCliStrings.CachingTitle);
        caching.WithCategory(AspireCliStrings.AspireCategory)
               .WithDescription(AspireCliStrings.CachingDescription)
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

        IScaffoldBuilder database = _builder.AddScaffolder(ScaffolderCatagory.Aspire, AspireCliStrings.Database.DatabaseTitle);
        database.WithCategory(AspireCliStrings.AspireCategory)
                .WithDescription(AspireCliStrings.Database.DatabaseDescription)
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

        IScaffoldBuilder storage = _builder.AddScaffolder(ScaffolderCatagory.Aspire, AspireCliStrings.StorageTitle);
        storage.WithCategory(AspireCliStrings.AspireCategory)
               .WithDescription(AspireCliStrings.StorageDescription)
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
    }

    private static void CreateAspireOptions(out ScaffolderOption<string> cachingTypeOption,
        out ScaffolderOption<string> databaseTypeOption,
        out ScaffolderOption<string> storageTypeOption,
        out ScaffolderOption<string> appHostProjectOption,
        out ScaffolderOption<string> projectOption,
        out ScaffolderOption<bool> prereleaseOption)
    {
        cachingTypeOption = new ScaffolderOption<string>
        {
            DisplayName = AspireCliStrings.CachingTypeOption,
            CliOption = AspireCliStrings.TypeCliOption,
            Description = AspireCliStrings.CachingTypeDescription,
            Required = true,
            PickerType = InteractivePickerType.CustomPicker,
            CustomPickerValues = AspireCliStrings.CachingTypeCustomValues
        };

        databaseTypeOption = new ScaffolderOption<string>
        {
            DisplayName = AspireCliStrings.Database.DatabaseTypeOption,
            CliOption = AspireCliStrings.TypeCliOption,
            Description = AspireCliStrings.Database.DatabaseTypeDescription,
            Required = true,
            PickerType = InteractivePickerType.CustomPicker,
            CustomPickerValues = AspireCliStrings.Database.DatabaseTypeCustomValues
        };

        storageTypeOption = new ScaffolderOption<string>
        {
            DisplayName = AspireCliStrings.StorageTypeOption,
            CliOption = AspireCliStrings.TypeCliOption,
            Description = AspireCliStrings.StorageTypeDescription,
            Required = true,
            PickerType = InteractivePickerType.CustomPicker,
            CustomPickerValues = AspireCliStrings.StorageTypeCustomValues
        };

        appHostProjectOption = new ScaffolderOption<string>
        {
            DisplayName = AspireCliStrings.AppHostProjectOption,
            CliOption = AspireCliStrings.AppHostCliOption,
            Description = AspireCliStrings.AppHostProjectDescription,
            Required = true,
            PickerType = InteractivePickerType.ProjectPicker
        };

        projectOption = new ScaffolderOption<string>
        {
            DisplayName = AspireCliStrings.ProjectOption,
            CliOption = AspireCliStrings.WorkerProjectCliOption,
            Description = AspireCliStrings.ProjectOptionDescription,
            Required = true,
            PickerType = InteractivePickerType.ProjectPicker
        };

        prereleaseOption = new ScaffolderOption<bool>
        {
            DisplayName = AspireCliStrings.PrereleaseOption,
            CliOption = AspireCliStrings.PrereleaseCliOption,
            Description = AspireCliStrings.PrereleaseDescription,
            Required = false,
            PickerType = InteractivePickerType.YesNo
        };
    }
}
