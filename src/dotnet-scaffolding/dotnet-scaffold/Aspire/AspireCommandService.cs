// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Core.Builder;
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

    public Type[] GetScaffoldSteps()
    {
        return
        [
            typeof(AddAspireCodeChangeStep),
            typeof(AddAspireConnectionStringStep),
            typeof(ValidateOptionsStep),
            typeof(WrappedAddPackagesStep)
        ];
    }

    public void AddScaffolderCommands()
    {
        var cachingType = AspireOptions.CachingType;

        IScaffoldBuilder caching = _builder.AddScaffolder(ScaffolderCatagory.Aspire, AspireCliStrings.CachingTitle);
        caching.WithCategory(AspireCliStrings.AspireCategory)
               .WithDescription(AspireCliStrings.CachingDescription)
               .WithExample(AspireCliStrings.CachingExample1, AspireCliStrings.CachingExample1Description)
               .WithExample(AspireCliStrings.CachingExample2, AspireCliStrings.CachingExample2Description)
              .WithOption(AspireOptions.CachingType)
              .WithOption(AspireOptions.AppHostProject)
              .WithOption(AspireOptions.Project)
              .WithOption(AspireOptions.Prerelease)
              .WithStep<ValidateOptionsStep>(config =>
              {
                config.Step.ValidateMethod = ValidationHelper.ValidateCachingSettings;
              })
              .WithCachingAddPackageSteps()
              .WithCachingCodeModificationSteps();

        IScaffoldBuilder database = _builder.AddScaffolder(ScaffolderCatagory.Aspire, AspireCliStrings.Database.DatabaseTitle);
        database.WithCategory(AspireCliStrings.AspireCategory)
                .WithDescription(AspireCliStrings.Database.DatabaseDescription)
                .WithExample(AspireCliStrings.Database.DatabaseExample1, AspireCliStrings.Database.DatabaseExample1Description)
                .WithExample(AspireCliStrings.Database.DatabaseExample2, AspireCliStrings.Database.DatabaseExample2Description)
                .WithOption(AspireOptions.DatabaseType)
                .WithOption(AspireOptions.AppHostProject)
                .WithOption(AspireOptions.Project)
                .WithOption(AspireOptions.Prerelease)
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
               .WithExample(AspireCliStrings.StorageExample1, AspireCliStrings.StorageExample1Description)
               .WithExample(AspireCliStrings.StorageExample2, AspireCliStrings.StorageExample2Description)
               .WithOption(AspireOptions.StorageType)
               .WithOption(AspireOptions.AppHostProject)
               .WithOption(AspireOptions.Project)
               .WithOption(AspireOptions.Prerelease)
               .WithStep<ValidateOptionsStep>(config =>
               {
                   config.Step.ValidateMethod = ValidationHelper.ValidateStorageSettings;
               })
               .WithStorageAddPackageSteps()
               .WithStorageCodeModificationSteps();
    }
}
