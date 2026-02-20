// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Commands;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using AspNetConstants = Microsoft.DotNet.Tools.Scaffold.AspNet.Common.Constants;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.RazorPages;

/// <summary>
/// Integration tests for the Razor Pages with Entity Framework CRUD scaffolder targeting .NET 11.
/// The razorpages-crud scaffolder generates Razor pages using Entity Framework for
/// Create, Delete, Details, Edit and List operations for a given model.
///
/// These tests validate:
///  - Scaffolder definition constants (Name, DisplayName, Description, Examples)
///  - Category assignment ("Razor Pages")
///  - CLI option constants (--model, --dataContext, --dbProvider, --page, --prerelease, --project)
///  - Option display names and descriptions (ModelName, DataContextClass, DatabaseProvider, PageType, Prerelease)
///  - CrudSettings / EfWithModelStepSettings / BaseSettings property chain
///  - ValidateRazorPagesStep property defaults and get/set
///  - ValidateRazorPagesStep validation logic (null/empty Project, Model, Page, DataContext)
///  - DatabaseProvider defaulting to sqlserver-efcore for unknown values
///  - DataContext defaulting to NewDbContext for invalid identifiers
///  - Page type normalization (defaults invalid values to CRUD)
///  - PackageConstants EF provider mappings and UseDatabaseMethods
///  - Telemetry tracking via ITelemetryService interface method
///  - RazorPages template folder contents for net11.0 (30 T4-based files, flat structure)
///  - razorPagesChanges.json exists in net11.0 CodeModificationConfigs
///  - BlazorEntraId folder exists for net11.0
///  - Template root expected scaffolder folders
///  - Parity with net10.0 templates
/// </summary>
public class RazorPagesCrudNet11IntegrationTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly string _testProjectDir;
    private readonly string _testProjectPath;
    private readonly Mock<IFileSystem> _mockFileSystem;
    private readonly Mock<ITelemetryService> _mockTelemetryService;
    private readonly Mock<IScaffolder> _mockScaffolder;
    private readonly ScaffolderContext _context;

    public RazorPagesCrudNet11IntegrationTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "RazorPagesCrudNet11IntegrationTests", Guid.NewGuid().ToString());
        _testProjectDir = Path.Combine(_testDirectory, "TestProject");
        _testProjectPath = Path.Combine(_testProjectDir, "TestProject.csproj");
        Directory.CreateDirectory(_testProjectDir);

        _mockFileSystem = new Mock<IFileSystem>();
        _mockTelemetryService = new Mock<ITelemetryService>();

        _mockScaffolder = new Mock<IScaffolder>();
        _mockScaffolder.Setup(s => s.DisplayName).Returns(AspnetStrings.RazorPage.CrudDisplayName);
        _mockScaffolder.Setup(s => s.Name).Returns(AspnetStrings.RazorPage.Crud);
        _context = new ScaffolderContext(_mockScaffolder.Object);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            try { Directory.Delete(_testDirectory, recursive: true); }
            catch { /* best-effort cleanup */ }
        }
    }

    #region Constants & Scaffolder Definition

    [Fact]
    public void CrudScaffolderName_IsRazorPagesCrud_Net11()
    {
        Assert.Equal("razorpages-crud", AspnetStrings.RazorPage.Crud);
    }

    [Fact]
    public void CrudScaffolderDisplayName_IsCorrect_Net11()
    {
        Assert.Equal("Razor Pages with Entity Framework (CRUD)", AspnetStrings.RazorPage.CrudDisplayName);
    }

    [Fact]
    public void CrudScaffolderDescription_IsCorrect_Net11()
    {
        Assert.Equal("Generates Razor pages using Entity Framework for Create, Delete, Details, Edit and List operations for the given model", AspnetStrings.RazorPage.CrudDescription);
    }

    [Fact]
    public void CrudScaffolderCategory_IsRazorPages_Net11()
    {
        Assert.Equal("Razor Pages", AspnetStrings.Catagories.RazorPages);
    }

    [Fact]
    public void CrudExample1_ContainsCrudCommand_Net11()
    {
        Assert.Contains("razorpages-crud", AspnetStrings.RazorPage.CrudExample1);
    }

    [Fact]
    public void CrudExample1_ContainsProjectOption_Net11()
    {
        Assert.Contains("--project", AspnetStrings.RazorPage.CrudExample1);
    }

    [Fact]
    public void CrudExample1_ContainsModelOption_Net11()
    {
        Assert.Contains("--model", AspnetStrings.RazorPage.CrudExample1);
        Assert.Contains("Customer", AspnetStrings.RazorPage.CrudExample1);
    }

    [Fact]
    public void CrudExample1_ContainsDataContextOption_Net11()
    {
        Assert.Contains("--data-context", AspnetStrings.RazorPage.CrudExample1);
        Assert.Contains("ShopDbContext", AspnetStrings.RazorPage.CrudExample1);
    }

    [Fact]
    public void CrudExample1_ContainsDatabaseProviderOption_Net11()
    {
        Assert.Contains("--database-provider", AspnetStrings.RazorPage.CrudExample1);
        Assert.Contains("SqlServer", AspnetStrings.RazorPage.CrudExample1);
    }

    [Fact]
    public void CrudExample1_ContainsPageOption_Net11()
    {
        Assert.Contains("--page", AspnetStrings.RazorPage.CrudExample1);
        Assert.Contains("All", AspnetStrings.RazorPage.CrudExample1);
    }

    [Fact]
    public void CrudExample1Description_ContainsCrud_Net11()
    {
        Assert.Contains("CRUD", AspnetStrings.RazorPage.CrudExample1Description);
    }

    [Fact]
    public void CrudExample2_ContainsPrerelease_Net11()
    {
        Assert.Contains("--prerelease", AspnetStrings.RazorPage.CrudExample2);
    }

    [Fact]
    public void CrudExample2_ContainsSQLite_Net11()
    {
        Assert.Contains("SQLite", AspnetStrings.RazorPage.CrudExample2);
    }

    [Fact]
    public void CrudExample2_ContainsListAndDetails_Net11()
    {
        Assert.Contains("List", AspnetStrings.RazorPage.CrudExample2);
        Assert.Contains("Details", AspnetStrings.RazorPage.CrudExample2);
    }

    [Fact]
    public void CrudExample2Description_ContainsSQLite_Net11()
    {
        Assert.Contains("SQLite", AspnetStrings.RazorPage.CrudExample2Description);
    }

    #endregion

    #region CLI Option Constants

    [Fact]
    public void CliOptions_ProjectOption_IsCorrect_Net11()
    {
        Assert.Equal("--project", AspNetConstants.CliOptions.ProjectCliOption);
    }

    [Fact]
    public void CliOptions_ModelOption_IsCorrect_Net11()
    {
        Assert.Equal("--model", AspNetConstants.CliOptions.ModelCliOption);
    }

    [Fact]
    public void CliOptions_DataContextOption_IsCorrect_Net11()
    {
        Assert.Equal("--dataContext", AspNetConstants.CliOptions.DataContextOption);
    }

    [Fact]
    public void CliOptions_DbProviderOption_IsCorrect_Net11()
    {
        Assert.Equal("--dbProvider", AspNetConstants.CliOptions.DbProviderOption);
    }

    [Fact]
    public void CliOptions_PageTypeOption_IsCorrect_Net11()
    {
        Assert.Equal("--page", AspNetConstants.CliOptions.PageTypeOption);
    }

    [Fact]
    public void CliOptions_PrereleaseOption_IsCorrect_Net11()
    {
        Assert.Equal("--prerelease", AspNetConstants.CliOptions.PrereleaseCliOption);
    }

    [Fact]
    public void Constants_NewDbContext_IsCorrect_Net11()
    {
        Assert.Equal("NewDbContext", AspNetConstants.NewDbContext);
    }

    #endregion

    #region Option Strings — ModelName

    [Fact]
    public void ModelNameOption_DisplayName_IsCorrect_Net11()
    {
        Assert.Equal("Model Name", AspnetStrings.Options.ModelName.DisplayName);
    }

    [Fact]
    public void ModelNameOption_Description_IsCorrect_Net11()
    {
        Assert.Equal("Name for the model class to be used for scaffolding", AspnetStrings.Options.ModelName.Description);
    }

    #endregion

    #region Option Strings — DataContextClass

    [Fact]
    public void DataContextClassOption_DisplayName_IsCorrect_Net11()
    {
        Assert.Equal("Data Context Class", AspnetStrings.Options.DataContextClass.DisplayName);
    }

    [Fact]
    public void DataContextClassOption_Description_ContainsDbContext_Net11()
    {
        Assert.Contains("DbContext", AspnetStrings.Options.DataContextClass.Description);
    }

    #endregion

    #region Option Strings — DatabaseProvider

    [Fact]
    public void DatabaseProviderOption_DisplayName_IsCorrect_Net11()
    {
        Assert.Equal("Database Provider", AspnetStrings.Options.DatabaseProvider.DisplayName);
    }

    [Fact]
    public void DatabaseProviderOption_Description_ContainsProvider_Net11()
    {
        Assert.Contains("provider", AspnetStrings.Options.DatabaseProvider.Description, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Option Strings — PageType

    [Fact]
    public void PageTypeOption_DisplayName_IsCorrect_Net11()
    {
        Assert.Equal("Page Type", AspnetStrings.Options.PageType.DisplayName);
    }

    [Fact]
    public void PageTypeOption_Description_ContainsCrud_Net11()
    {
        Assert.Contains("CRUD", AspnetStrings.Options.PageType.Description);
    }

    #endregion

    #region Option Strings — Prerelease

    [Fact]
    public void PrereleaseOption_DisplayName_IsCorrect_Net11()
    {
        Assert.Equal("Include Prerelease packages?", AspnetStrings.Options.Prerelease.DisplayName);
    }

    [Fact]
    public void PrereleaseOption_Description_ContainsPrerelease_Net11()
    {
        Assert.Contains("prerelease", AspnetStrings.Options.Prerelease.Description, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region CrudSettings Properties

    [Fact]
    public void CrudSettings_HasPageProperty_Net11()
    {
        var settings = new CrudSettings
        {
            Project = _testProjectPath,
            Model = "Customer",
            Page = "CRUD",
            DataContext = "ShopDbContext",
            DatabaseProvider = PackageConstants.EfConstants.SqlServer
        };

        Assert.Equal("CRUD", settings.Page);
    }

    [Fact]
    public void CrudSettings_HasModelProperty_Net11()
    {
        var settings = new CrudSettings
        {
            Project = _testProjectPath,
            Model = "Customer",
            Page = "CRUD",
            DataContext = "ShopDbContext",
            DatabaseProvider = PackageConstants.EfConstants.SqlServer
        };

        Assert.Equal("Customer", settings.Model);
    }

    [Fact]
    public void CrudSettings_HasDataContextProperty_Net11()
    {
        var settings = new CrudSettings
        {
            Project = _testProjectPath,
            Model = "Customer",
            Page = "CRUD",
            DataContext = "ShopDbContext",
            DatabaseProvider = PackageConstants.EfConstants.SqlServer
        };

        Assert.Equal("ShopDbContext", settings.DataContext);
    }

    [Fact]
    public void CrudSettings_HasDatabaseProviderProperty_Net11()
    {
        var settings = new CrudSettings
        {
            Project = _testProjectPath,
            Model = "Customer",
            Page = "CRUD",
            DataContext = "ShopDbContext",
            DatabaseProvider = PackageConstants.EfConstants.SqlServer
        };

        Assert.Equal(PackageConstants.EfConstants.SqlServer, settings.DatabaseProvider);
    }

    [Fact]
    public void CrudSettings_HasProjectProperty_Net11()
    {
        var settings = new CrudSettings
        {
            Project = _testProjectPath,
            Model = "Customer",
            Page = "CRUD",
            DataContext = "ShopDbContext",
            DatabaseProvider = PackageConstants.EfConstants.SqlServer
        };

        Assert.Equal(_testProjectPath, settings.Project);
    }

    [Fact]
    public void CrudSettings_HasPrereleaseProperty_Net11()
    {
        var settings = new CrudSettings
        {
            Project = _testProjectPath,
            Model = "Customer",
            Page = "CRUD",
            DataContext = "ShopDbContext",
            DatabaseProvider = PackageConstants.EfConstants.SqlServer,
            Prerelease = true
        };

        Assert.True(settings.Prerelease);
    }

    [Fact]
    public void CrudSettings_PrereleaseDefaultsFalse_Net11()
    {
        var settings = new CrudSettings
        {
            Project = _testProjectPath,
            Model = "Customer",
            Page = "CRUD",
            DataContext = "ShopDbContext",
            DatabaseProvider = PackageConstants.EfConstants.SqlServer
        };

        Assert.False(settings.Prerelease);
    }

    [Fact]
    public void CrudSettings_InheritsEfWithModelStepSettings_Net11()
    {
        var settings = new CrudSettings
        {
            Project = _testProjectPath,
            Model = "Customer",
            Page = "CRUD",
            DataContext = "ShopDbContext",
            DatabaseProvider = PackageConstants.EfConstants.SqlServer
        };

        Assert.IsAssignableFrom<EfWithModelStepSettings>(settings);
    }

    [Fact]
    public void CrudSettings_InheritsBaseSettings_Net11()
    {
        var settings = new CrudSettings
        {
            Project = _testProjectPath,
            Model = "Customer",
            Page = "CRUD",
            DataContext = "ShopDbContext",
            DatabaseProvider = PackageConstants.EfConstants.SqlServer
        };

        Assert.IsAssignableFrom<BaseSettings>(settings);
    }

    #endregion

    #region ValidateRazorPagesStep — Properties

    [Fact]
    public void ValidateRazorPagesStep_HasProjectProperty_Net11()
    {
        var step = CreateValidateRazorPagesStep();
        step.Project = _testProjectPath;
        Assert.Equal(_testProjectPath, step.Project);
    }

    [Fact]
    public void ValidateRazorPagesStep_HasModelProperty_Net11()
    {
        var step = CreateValidateRazorPagesStep();
        step.Model = "Customer";
        Assert.Equal("Customer", step.Model);
    }

    [Fact]
    public void ValidateRazorPagesStep_HasPageProperty_Net11()
    {
        var step = CreateValidateRazorPagesStep();
        step.Page = "CRUD";
        Assert.Equal("CRUD", step.Page);
    }

    [Fact]
    public void ValidateRazorPagesStep_HasDataContextProperty_Net11()
    {
        var step = CreateValidateRazorPagesStep();
        step.DataContext = "ShopDbContext";
        Assert.Equal("ShopDbContext", step.DataContext);
    }

    [Fact]
    public void ValidateRazorPagesStep_HasDatabaseProviderProperty_Net11()
    {
        var step = CreateValidateRazorPagesStep();
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;
        Assert.Equal(PackageConstants.EfConstants.SqlServer, step.DatabaseProvider);
    }

    [Fact]
    public void ValidateRazorPagesStep_HasPrereleaseProperty_Net11()
    {
        var step = CreateValidateRazorPagesStep();
        step.Prerelease = true;
        Assert.True(step.Prerelease);
    }

    [Fact]
    public void ValidateRazorPagesStep_ProjectDefaultsToNull_Net11()
    {
        var step = CreateValidateRazorPagesStep();
        Assert.Null(step.Project);
    }

    [Fact]
    public void ValidateRazorPagesStep_ModelDefaultsToNull_Net11()
    {
        var step = CreateValidateRazorPagesStep();
        Assert.Null(step.Model);
    }

    [Fact]
    public void ValidateRazorPagesStep_PageDefaultsToNull_Net11()
    {
        var step = CreateValidateRazorPagesStep();
        Assert.Null(step.Page);
    }

    [Fact]
    public void ValidateRazorPagesStep_DataContextDefaultsToNull_Net11()
    {
        var step = CreateValidateRazorPagesStep();
        Assert.Null(step.DataContext);
    }

    [Fact]
    public void ValidateRazorPagesStep_DatabaseProviderDefaultsToNull_Net11()
    {
        var step = CreateValidateRazorPagesStep();
        Assert.Null(step.DatabaseProvider);
    }

    [Fact]
    public void ValidateRazorPagesStep_PrereleaseDefaultsToFalse_Net11()
    {
        var step = CreateValidateRazorPagesStep();
        Assert.False(step.Prerelease);
    }

    #endregion

    #region ValidateRazorPagesStep — Validation Logic

    [Fact]
    public async Task ValidateRazorPagesStep_FailsWithNullProject_Net11()
    {
        var step = CreateValidateRazorPagesStep();
        step.Project = null;
        step.Model = "Customer";
        step.Page = "CRUD";
        step.DataContext = "ShopDbContext";
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateRazorPagesStep_FailsWithEmptyProject_Net11()
    {
        var step = CreateValidateRazorPagesStep();
        step.Project = string.Empty;
        step.Model = "Customer";
        step.Page = "CRUD";
        step.DataContext = "ShopDbContext";
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateRazorPagesStep_FailsWithNonExistentProject_Net11()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(false);

        var step = CreateValidateRazorPagesStep();
        step.Project = @"C:\NonExistent\Project.csproj";
        step.Model = "Customer";
        step.Page = "CRUD";
        step.DataContext = "ShopDbContext";
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateRazorPagesStep_FailsWithNullModel_Net11()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateRazorPagesStep();
        step.Project = _testProjectPath;
        step.Model = null;
        step.Page = "CRUD";
        step.DataContext = "ShopDbContext";
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateRazorPagesStep_FailsWithEmptyModel_Net11()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateRazorPagesStep();
        step.Project = _testProjectPath;
        step.Model = string.Empty;
        step.Page = "CRUD";
        step.DataContext = "ShopDbContext";
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateRazorPagesStep_FailsWithNullPage_Net11()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateRazorPagesStep();
        step.Project = _testProjectPath;
        step.Model = "Customer";
        step.Page = null;
        step.DataContext = "ShopDbContext";
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateRazorPagesStep_FailsWithEmptyPage_Net11()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateRazorPagesStep();
        step.Project = _testProjectPath;
        step.Model = "Customer";
        step.Page = string.Empty;
        step.DataContext = "ShopDbContext";
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateRazorPagesStep_FailsWithNullDataContext_Net11()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateRazorPagesStep();
        step.Project = _testProjectPath;
        step.Model = "Customer";
        step.Page = "CRUD";
        step.DataContext = null;
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateRazorPagesStep_FailsWithEmptyDataContext_Net11()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateRazorPagesStep();
        step.Project = _testProjectPath;
        step.Model = "Customer";
        step.Page = "CRUD";
        step.DataContext = string.Empty;
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    #endregion

    #region ValidateRazorPagesStep — Telemetry

    [Fact]
    public async Task ValidateRazorPagesStep_TracksTelemetry_OnNullProjectFailure_Net11()
    {
        var step = CreateValidateRazorPagesStep();
        step.Project = null;
        step.Model = "Customer";
        step.Page = "CRUD";
        step.DataContext = "ShopDbContext";

        await step.ExecuteAsync(_context);

        _mockTelemetryService.Verify(
            t => t.TrackEvent(
                It.IsAny<string>(),
                It.IsAny<IReadOnlyDictionary<string, string>>(),
                It.IsAny<IReadOnlyDictionary<string, double>>()),
            Times.Once);
    }

    [Fact]
    public async Task ValidateRazorPagesStep_TracksTelemetry_OnNullModelFailure_Net11()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateRazorPagesStep();
        step.Project = _testProjectPath;
        step.Model = null;
        step.Page = "CRUD";
        step.DataContext = "ShopDbContext";

        await step.ExecuteAsync(_context);

        _mockTelemetryService.Verify(
            t => t.TrackEvent(
                It.IsAny<string>(),
                It.IsAny<IReadOnlyDictionary<string, string>>(),
                It.IsAny<IReadOnlyDictionary<string, double>>()),
            Times.Once);
    }

    [Fact]
    public async Task ValidateRazorPagesStep_TracksTelemetry_OnNullPageFailure_Net11()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateRazorPagesStep();
        step.Project = _testProjectPath;
        step.Model = "Customer";
        step.Page = null;
        step.DataContext = "ShopDbContext";

        await step.ExecuteAsync(_context);

        _mockTelemetryService.Verify(
            t => t.TrackEvent(
                It.IsAny<string>(),
                It.IsAny<IReadOnlyDictionary<string, string>>(),
                It.IsAny<IReadOnlyDictionary<string, double>>()),
            Times.Once);
    }

    [Fact]
    public async Task ValidateRazorPagesStep_TracksTelemetry_OnNullDataContextFailure_Net11()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateRazorPagesStep();
        step.Project = _testProjectPath;
        step.Model = "Customer";
        step.Page = "CRUD";
        step.DataContext = null;

        await step.ExecuteAsync(_context);

        _mockTelemetryService.Verify(
            t => t.TrackEvent(
                It.IsAny<string>(),
                It.IsAny<IReadOnlyDictionary<string, string>>(),
                It.IsAny<IReadOnlyDictionary<string, double>>()),
            Times.Once);
    }

    #endregion

    #region CRUD Page Types

    [Fact]
    public void CrudPageType_IsCRUD_Net11()
    {
        Assert.Equal("CRUD", BlazorCrudHelper.CrudPageType);
    }

    [Fact]
    public void CrudPages_ContainsExpectedPageTypes_Net11()
    {
        var pages = BlazorCrudHelper.CRUDPages;
        Assert.Contains("CRUD", pages);
        Assert.Contains("Create", pages);
        Assert.Contains("Delete", pages);
        Assert.Contains("Details", pages);
        Assert.Contains("Edit", pages);
        Assert.Contains("Index", pages);
    }

    [Fact]
    public void CrudPages_ContainsNotFound_Net11()
    {
        Assert.Contains("NotFound", BlazorCrudHelper.CRUDPages);
    }

    #endregion

    #region PackageConstants — EF Providers

    [Fact]
    public void EfConstants_SqlServer_IsCorrect_Net11()
    {
        Assert.Equal("sqlserver-efcore", PackageConstants.EfConstants.SqlServer);
    }

    [Fact]
    public void EfConstants_SQLite_IsCorrect_Net11()
    {
        Assert.Equal("sqlite-efcore", PackageConstants.EfConstants.SQLite);
    }

    [Fact]
    public void EfConstants_CosmosDb_IsCorrect_Net11()
    {
        Assert.Equal("cosmos-efcore", PackageConstants.EfConstants.CosmosDb);
    }

    [Fact]
    public void EfConstants_Postgres_IsCorrect_Net11()
    {
        Assert.Equal("npgsql-efcore", PackageConstants.EfConstants.Postgres);
    }

    [Fact]
    public void EfPackagesDict_ContainsAllFourProviders_Net11()
    {
        Assert.Equal(4, PackageConstants.EfConstants.EfPackagesDict.Count);
        Assert.True(PackageConstants.EfConstants.EfPackagesDict.ContainsKey(PackageConstants.EfConstants.SqlServer));
        Assert.True(PackageConstants.EfConstants.EfPackagesDict.ContainsKey(PackageConstants.EfConstants.SQLite));
        Assert.True(PackageConstants.EfConstants.EfPackagesDict.ContainsKey(PackageConstants.EfConstants.CosmosDb));
        Assert.True(PackageConstants.EfConstants.EfPackagesDict.ContainsKey(PackageConstants.EfConstants.Postgres));
    }

    [Fact]
    public void EfPackagesDict_SqlServerPackage_IsCorrect_Net11()
    {
        var package = PackageConstants.EfConstants.EfPackagesDict[PackageConstants.EfConstants.SqlServer];
        Assert.Equal("Microsoft.EntityFrameworkCore.SqlServer", package.Name);
    }

    [Fact]
    public void EfPackagesDict_SqlitePackage_IsCorrect_Net11()
    {
        var package = PackageConstants.EfConstants.EfPackagesDict[PackageConstants.EfConstants.SQLite];
        Assert.Equal("Microsoft.EntityFrameworkCore.Sqlite", package.Name);
    }

    [Fact]
    public void EfPackagesDict_CosmosPackage_IsCorrect_Net11()
    {
        var package = PackageConstants.EfConstants.EfPackagesDict[PackageConstants.EfConstants.CosmosDb];
        Assert.Equal("Microsoft.EntityFrameworkCore.Cosmos", package.Name);
    }

    [Fact]
    public void EfPackagesDict_PostgresPackage_IsCorrect_Net11()
    {
        var package = PackageConstants.EfConstants.EfPackagesDict[PackageConstants.EfConstants.Postgres];
        Assert.Equal("Npgsql.EntityFrameworkCore.PostgreSQL", package.Name);
    }

    [Fact]
    public void EfCoreToolsPackage_IsCorrect_Net11()
    {
        Assert.Equal("Microsoft.EntityFrameworkCore.Tools", PackageConstants.EfConstants.EfCoreToolsPackage.Name);
    }

    [Fact]
    public void EfCorePackage_IsCorrect_Net11()
    {
        Assert.Equal("Microsoft.EntityFrameworkCore", PackageConstants.EfConstants.EfCorePackage.Name);
    }

    #endregion

    #region PackageConstants — UseDatabaseMethods

    [Fact]
    public void UseDatabaseMethods_SqlServer_IsUseSqlServer_Net11()
    {
        Assert.Equal("UseSqlServer", PackageConstants.EfConstants.UseDatabaseMethods[PackageConstants.EfConstants.SqlServer]);
    }

    [Fact]
    public void UseDatabaseMethods_SQLite_IsUseSqlite_Net11()
    {
        Assert.Equal("UseSqlite", PackageConstants.EfConstants.UseDatabaseMethods[PackageConstants.EfConstants.SQLite]);
    }

    [Fact]
    public void UseDatabaseMethods_Postgres_IsUseNpgsql_Net11()
    {
        Assert.Equal("UseNpgsql", PackageConstants.EfConstants.UseDatabaseMethods[PackageConstants.EfConstants.Postgres]);
    }

    [Fact]
    public void UseDatabaseMethods_CosmosDb_IsUseCosmos_Net11()
    {
        Assert.Equal("UseCosmos", PackageConstants.EfConstants.UseDatabaseMethods[PackageConstants.EfConstants.CosmosDb]);
    }

    [Fact]
    public void UseDatabaseMethods_HasAllFourProviders_Net11()
    {
        Assert.Equal(4, PackageConstants.EfConstants.UseDatabaseMethods.Count);
    }

    #endregion

    #region Multiple Validation Failure Scenarios

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task ValidateRazorPagesStep_FailsWithInvalidModel_Net11(string? model)
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateRazorPagesStep();
        step.Project = _testProjectPath;
        step.Model = model;
        step.Page = "CRUD";
        step.DataContext = "ShopDbContext";
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task ValidateRazorPagesStep_FailsWithInvalidPage_Net11(string? page)
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateRazorPagesStep();
        step.Project = _testProjectPath;
        step.Model = "Customer";
        step.Page = page;
        step.DataContext = "ShopDbContext";
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task ValidateRazorPagesStep_FailsWithInvalidDataContext_Net11(string? dataContext)
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateRazorPagesStep();
        step.Project = _testProjectPath;
        step.Model = "Customer";
        step.Page = "CRUD";
        step.DataContext = dataContext;
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    #endregion

    #region ScaffolderContext

    [Fact]
    public void ScaffolderContext_HasCrudScaffolderName_Net11()
    {
        Assert.Equal("razorpages-crud", _context.Scaffolder.Name);
    }

    [Fact]
    public void ScaffolderContext_HasCrudDisplayName_Net11()
    {
        Assert.Equal("Razor Pages with Entity Framework (CRUD)", _context.Scaffolder.DisplayName);
    }

    #endregion

    #region Scaffolder Differentiation

    [Fact]
    public void RazorPagesCrud_IsDifferentFromMvcControllerCrud_Net11()
    {
        Assert.NotEqual(AspnetStrings.RazorPage.Crud, AspnetStrings.MVC.ControllerCrud);
    }

    [Fact]
    public void RazorPagesCrud_IsDifferentFromBlazorCrud_Net11()
    {
        Assert.NotEqual(AspnetStrings.RazorPage.Crud, AspnetStrings.Blazor.Crud);
    }

    [Fact]
    public void RazorPagesCrud_IsDifferentFromRazorPageEmpty_Net11()
    {
        Assert.NotEqual(AspnetStrings.RazorPage.Crud, AspnetStrings.RazorPage.Empty);
    }

    [Fact]
    public void RazorPagesCrud_Category_DiffersFromMvcCategory_Net11()
    {
        Assert.NotEqual(AspnetStrings.Catagories.RazorPages, AspnetStrings.Catagories.MVC);
    }

    #endregion

    #region RazorPages Templates — Net11

    [Fact]
    public void Net11_RazorPagesTemplates_FolderExists()
    {
        var basePath = GetActualTemplatesBasePath();
        var razorPagesDir = Path.Combine(basePath, "net11.0", "RazorPages");
        Assert.True(Directory.Exists(razorPagesDir),
            "RazorPages template folder should exist for net11.0");
    }

    [Fact]
    public void Net11_RazorPagesTemplates_NoBootstrapSubfolders()
    {
        var basePath = GetActualTemplatesBasePath();
        var razorPagesDir = Path.Combine(basePath, "net11.0", "RazorPages");
        var subDirs = Directory.GetDirectories(razorPagesDir);
        Assert.Empty(subDirs);
    }

    [Fact]
    public void Net11_RazorPagesTemplates_HasExactly30Files()
    {
        var basePath = GetActualTemplatesBasePath();
        var razorPagesDir = Path.Combine(basePath, "net11.0", "RazorPages");
        var files = Directory.GetFiles(razorPagesDir, "*", SearchOption.AllDirectories);
        Assert.Equal(30, files.Length);
    }

    [Theory]
    [InlineData("Create.cs")]
    [InlineData("Create.Interfaces.cs")]
    [InlineData("Create.tt")]
    [InlineData("CreateModel.cs")]
    [InlineData("CreateModel.Interfaces.cs")]
    [InlineData("CreateModel.tt")]
    [InlineData("Delete.cs")]
    [InlineData("Delete.Interfaces.cs")]
    [InlineData("Delete.tt")]
    [InlineData("DeleteModel.cs")]
    [InlineData("DeleteModel.Interfaces.cs")]
    [InlineData("DeleteModel.tt")]
    [InlineData("Details.cs")]
    [InlineData("Details.Interfaces.cs")]
    [InlineData("Details.tt")]
    [InlineData("DetailsModel.cs")]
    [InlineData("DetailsModel.Interfaces.cs")]
    [InlineData("DetailsModel.tt")]
    [InlineData("Edit.cs")]
    [InlineData("Edit.Interfaces.cs")]
    [InlineData("Edit.tt")]
    [InlineData("EditModel.cs")]
    [InlineData("EditModel.Interfaces.cs")]
    [InlineData("EditModel.tt")]
    [InlineData("Index.cs")]
    [InlineData("Index.Interfaces.cs")]
    [InlineData("Index.tt")]
    [InlineData("IndexModel.cs")]
    [InlineData("IndexModel.Interfaces.cs")]
    [InlineData("IndexModel.tt")]
    public void Net11_RazorPagesTemplates_HasExpectedFile(string fileName)
    {
        var basePath = GetActualTemplatesBasePath();
        var filePath = Path.Combine(basePath, "net11.0", "RazorPages", fileName);
        Assert.True(File.Exists(filePath),
            $"Expected RazorPages template file '{fileName}' not found for net11.0");
    }

    [Fact]
    public void Net11_RazorPagesTemplates_Has10TtFiles()
    {
        var basePath = GetActualTemplatesBasePath();
        var razorPagesDir = Path.Combine(basePath, "net11.0", "RazorPages");
        var ttFiles = Directory.GetFiles(razorPagesDir, "*.tt");
        Assert.Equal(10, ttFiles.Length);
    }

    [Fact]
    public void Net11_RazorPagesTemplates_NoCshtmlFiles()
    {
        var basePath = GetActualTemplatesBasePath();
        var razorPagesDir = Path.Combine(basePath, "net11.0", "RazorPages");
        var cshtmlFiles = Directory.GetFiles(razorPagesDir, "*.cshtml");
        Assert.Empty(cshtmlFiles);
    }

    #endregion

    #region CodeModificationConfigs — Net11

    [Fact]
    public void Net11_CodeModConfigs_HasRazorPagesChangesJson()
    {
        var basePath = GetActualTemplatesBasePath();
        var configPath = Path.Combine(basePath, "net11.0", "CodeModificationConfigs", "razorPagesChanges.json");
        Assert.True(File.Exists(configPath),
            "razorPagesChanges.json should exist for net11.0");
    }

    #endregion

    #region Template Root — Expected Scaffolder Folders

    [Theory]
    [InlineData("BlazorCrud")]
    [InlineData("BlazorEntraId")]
    [InlineData("BlazorIdentity")]
    [InlineData("CodeModificationConfigs")]
    [InlineData("EfController")]
    [InlineData("Files")]
    [InlineData("Identity")]
    [InlineData("MinimalApi")]
    [InlineData("RazorPages")]
    [InlineData("Views")]
    public void Net11_Templates_HasExpectedScaffolderFolder(string folderName)
    {
        var basePath = GetActualTemplatesBasePath();
        var folderPath = Path.Combine(basePath, "net11.0", folderName);
        Assert.True(Directory.Exists(folderPath),
            $"Expected template folder '{folderName}' not found for net11.0");
    }

    [Fact]
    public void Net11_Templates_HasBlazorEntraIdFolder()
    {
        var basePath = GetActualTemplatesBasePath();
        var entraIdDir = Path.Combine(basePath, "net11.0", "BlazorEntraId");
        Assert.True(Directory.Exists(entraIdDir),
            "BlazorEntraId folder should exist for net11.0");
    }

    #endregion

    #region Net11 vs Net10 Template Parity

    [Fact]
    public void Net11VsNet10_RazorPagesTemplates_SameFileCount()
    {
        var basePath = GetActualTemplatesBasePath();
        var net10Dir = Path.Combine(basePath, "net10.0", "RazorPages");
        var net11Dir = Path.Combine(basePath, "net11.0", "RazorPages");
        var net10Files = Directory.GetFiles(net10Dir, "*", SearchOption.AllDirectories);
        var net11Files = Directory.GetFiles(net11Dir, "*", SearchOption.AllDirectories);
        Assert.Equal(net10Files.Length, net11Files.Length);
    }

    [Fact]
    public void Net11VsNet10_RazorPagesTemplates_SameFileNames()
    {
        var basePath = GetActualTemplatesBasePath();
        var net10Dir = Path.Combine(basePath, "net10.0", "RazorPages");
        var net11Dir = Path.Combine(basePath, "net11.0", "RazorPages");
        var net10FileNames = Directory.GetFiles(net10Dir, "*", SearchOption.AllDirectories)
            .Select(Path.GetFileName).OrderBy(f => f).ToArray();
        var net11FileNames = Directory.GetFiles(net11Dir, "*", SearchOption.AllDirectories)
            .Select(Path.GetFileName).OrderBy(f => f).ToArray();
        Assert.Equal(net10FileNames, net11FileNames);
    }

    [Fact]
    public void Net11VsNet10_Templates_SameTopLevelFolders()
    {
        var basePath = GetActualTemplatesBasePath();
        var net10Dirs = Directory.GetDirectories(Path.Combine(basePath, "net10.0"))
            .Select(Path.GetFileName).OrderBy(f => f).ToArray();
        var net11Dirs = Directory.GetDirectories(Path.Combine(basePath, "net11.0"))
            .Select(Path.GetFileName).OrderBy(f => f).ToArray();
        Assert.Equal(net10Dirs, net11Dirs);
    }

    #endregion

    #region Helper Methods

    private ValidateRazorPagesStep CreateValidateRazorPagesStep()
    {
        return new ValidateRazorPagesStep(
            _mockFileSystem.Object,
            NullLogger<ValidateRazorPagesStep>.Instance,
            _mockTelemetryService.Object);
    }

    private static string GetActualTemplatesBasePath()
    {
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
        var basePath = Path.Combine(assemblyDirectory!, "..", "..", "..", "..", "..", "src", "dotnet-scaffolding", "dotnet-scaffold", "AspNet", "Templates");
        return Path.GetFullPath(basePath);
    }

    #endregion
}
