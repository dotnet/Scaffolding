// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.ComponentModel;
using Microsoft.DotNet.Scaffolding.Core.Hosting;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Tools.Scaffold.Aspire.Helpers;
using Microsoft.DotNet.Tools.Scaffold.Aspire.ScaffoldSteps;
using Microsoft.DotNet.Tools.Scaffold.Command;

namespace Microsoft.DotNet.Tools.Scaffold.Aspire.Commands;

internal class AspireCommandService(IScaffoldRunnerBuilder builder) : ICommandService
{
    IScaffoldRunnerBuilder _builder = builder;
    List<CommandInfo> _commandInfo = [];

    public List<CommandInfo> CommandInfos => _commandInfo;

    public string CommandId => "dotnet-scaffold-aspire";

    public void AddScaffolderCommands()
    {
        CreateAspireOptions(out var cachingTypeOption,
            out var databaseTypeOption,
            out var storageTypeOption,
            out var appHostProjectOption,
            out var projectOption,
            out var prereleaseOption);

        IScaffoldBuilder caching = _builder.AddScaffolder(ScaffolderCatagory.Aspire, "caching");
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

        _commandInfo.Add(caching.AsCommandInfo());

        IScaffoldBuilder database = _builder.AddScaffolder(ScaffolderCatagory.Aspire, "database");
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
                .WithConnectionStringStep()
                .WithDatabaseCodeModificationSteps();

        _commandInfo.Add(database.AsCommandInfo());

        IScaffoldBuilder storage = _builder.AddScaffolder(ScaffolderCatagory.Aspire, "storage");
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

        _commandInfo.Add(storage.AsCommandInfo());
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
}
