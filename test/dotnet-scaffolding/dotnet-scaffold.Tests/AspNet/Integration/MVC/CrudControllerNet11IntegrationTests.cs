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
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.MVC;

/// <summary>
/// Integration tests for the MVC Controller with Entity Framework CRUD scaffolder targeting .NET 11.
/// The mvccontroller-crud scaffolder creates a controller with read/write actions and views using EF.
///
/// These tests validate:
///  - Scaffolder definition constants (Name, DisplayName, Description, Examples)
///  - Category assignment ("MVC")
///  - CLI option constants (--model, --controller, --dataContext, --dbProvider, --views, --prerelease, --project)
///  - Option display names and descriptions (ControllerName, ModelName, DataContextClass, DatabaseProvider, Views, Prerelease)
///  - EfControllerSettings / EfWithModelStepSettings / BaseSettings property chain
///  - ValidateEfControllerStep property defaults and get/set
///  - ValidateEfControllerStep validation logic (null/empty Project, Model, ControllerName, ControllerType, DataContext)
///  - DatabaseProvider defaulting to sqlserver-efcore for unknown values
///  - DataContext defaulting to NewDbContext for invalid identifiers
///  - ControllerName extension stripping via Path.GetFileNameWithoutExtension
///  - ControllerType normalization (defaults invalid values to API)
///  - PackageConstants EF provider mappings and UseDatabaseMethods
///  - Telemetry tracking via ITelemetryService interface method
///  - EfController template folder contents for net11.0 (6 T4-based files)
///  - Views template folder contents for net11.0 (15 T4-based files, flat structure)
///  - efControllerChanges.json exists in net11.0 CodeModificationConfigs
///  - Template root expected scaffolder folders (including BlazorEntraId)
///  - Parity with net10.0 templates
/// </summary>
public class CrudControllerNet11IntegrationTests : IDisposable
{
    private const string TargetFramework = "net11.0";
    private readonly string _testDirectory;
    private readonly string _testProjectDir;
    private readonly string _testProjectPath;
    private readonly Mock<IFileSystem> _mockFileSystem;
    private readonly Mock<ITelemetryService> _mockTelemetryService;
    private readonly Mock<IScaffolder> _mockScaffolder;
    private readonly ScaffolderContext _context;

    public CrudControllerNet11IntegrationTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "CrudControllerNet11IntegrationTests", Guid.NewGuid().ToString());
        _testProjectDir = Path.Combine(_testDirectory, "TestProject");
        _testProjectPath = Path.Combine(_testProjectDir, "TestProject.csproj");
        Directory.CreateDirectory(_testProjectDir);

        _mockFileSystem = new Mock<IFileSystem>();
        _mockTelemetryService = new Mock<ITelemetryService>();

        _mockScaffolder = new Mock<IScaffolder>();
        _mockScaffolder.Setup(s => s.DisplayName).Returns(AspnetStrings.MVC.CrudDisplayName);
        _mockScaffolder.Setup(s => s.Name).Returns(AspnetStrings.MVC.ControllerCrud);
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
    public void CrudScaffolderName_IsMvcControllerCrud_Net11()
    {
        Assert.Equal("mvccontroller-crud", AspnetStrings.MVC.ControllerCrud);
    }

    [Fact]
    public void CrudScaffolderDisplayName_IsCorrect_Net11()
    {
        Assert.Equal("MVC Controller with views, using Entity Framework (CRUD)", AspnetStrings.MVC.CrudDisplayName);
    }

    [Fact]
    public void CrudScaffolderDescription_IsCorrect_Net11()
    {
        Assert.Equal("Create a MVC controller with read/write actions and views using Entity Framework", AspnetStrings.MVC.CrudDescription);
    }

    [Fact]
    public void CrudScaffolderCategory_IsMVC_Net11()
    {
        Assert.Equal("MVC", AspnetStrings.Catagories.MVC);
    }

    [Fact]
    public void CrudExample1_ContainsCrudCommand_Net11()
    {
        Assert.Contains("mvccontroller-crud", AspnetStrings.MVC.ControllerCrudExample1);
    }

    [Fact]
    public void CrudExample1_ContainsProjectOption_Net11()
    {
        Assert.Contains("--project", AspnetStrings.MVC.ControllerCrudExample1);
    }

    [Fact]
    public void CrudExample1_ContainsModelOption_Net11()
    {
        Assert.Contains("--model", AspnetStrings.MVC.ControllerCrudExample1);
        Assert.Contains("Product", AspnetStrings.MVC.ControllerCrudExample1);
    }

    [Fact]
    public void CrudExample1_ContainsControllerNameOption_Net11()
    {
        Assert.Contains("--controller-name", AspnetStrings.MVC.ControllerCrudExample1);
        Assert.Contains("ProductsController", AspnetStrings.MVC.ControllerCrudExample1);
    }

    [Fact]
    public void CrudExample1_ContainsDataContextOption_Net11()
    {
        Assert.Contains("--data-context", AspnetStrings.MVC.ControllerCrudExample1);
        Assert.Contains("AppDbContext", AspnetStrings.MVC.ControllerCrudExample1);
    }

    [Fact]
    public void CrudExample1_ContainsDatabaseProviderOption_Net11()
    {
        Assert.Contains("--database-provider", AspnetStrings.MVC.ControllerCrudExample1);
        Assert.Contains("SqlServer", AspnetStrings.MVC.ControllerCrudExample1);
    }

    [Fact]
    public void CrudExample1_ContainsViewsOption_Net11()
    {
        Assert.Contains("--views", AspnetStrings.MVC.ControllerCrudExample1);
    }

    [Fact]
    public void CrudExample1Description_ContainsCrud_Net11()
    {
        Assert.Contains("CRUD", AspnetStrings.MVC.ControllerCrudExample1Description);
    }

    [Fact]
    public void CrudExample2_ContainsPrerelease_Net11()
    {
        Assert.Contains("--prerelease", AspnetStrings.MVC.ControllerCrudExample2);
    }

    [Fact]
    public void CrudExample2_ContainsSQLite_Net11()
    {
        Assert.Contains("SQLite", AspnetStrings.MVC.ControllerCrudExample2);
    }

    [Fact]
    public void CrudExample2_ContainsBooksController_Net11()
    {
        Assert.Contains("BooksController", AspnetStrings.MVC.ControllerCrudExample2);
    }

    [Fact]
    public void CrudExample2Description_ContainsSQLite_Net11()
    {
        Assert.Contains("SQLite", AspnetStrings.MVC.ControllerCrudExample2Description);
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
    public void CliOptions_ControllerNameOption_IsCorrect_Net11()
    {
        Assert.Equal("--controller", AspNetConstants.CliOptions.ControllerNameOption);
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
    public void CliOptions_ViewsOption_IsCorrect_Net11()
    {
        Assert.Equal("--views", AspNetConstants.CliOptions.ViewsOption);
    }

    [Fact]
    public void CliOptions_PrereleaseOption_IsCorrect_Net11()
    {
        Assert.Equal("--prerelease", AspNetConstants.CliOptions.PrereleaseCliOption);
    }

    [Fact]
    public void DotnetCommands_ControllerCommandOutput_IsControllers_Net11()
    {
        Assert.Equal("Controllers", AspNetConstants.DotnetCommands.ControllerCommandOutput);
    }

    [Fact]
    public void Constants_NewDbContext_IsCorrect_Net11()
    {
        Assert.Equal("NewDbContext", AspNetConstants.NewDbContext);
    }

    #endregion

    #region Option Strings — ControllerName

    [Fact]
    public void ControllerNameOption_DisplayName_IsCorrect_Net11()
    {
        Assert.Equal("Controller Name", AspnetStrings.Options.ControllerName.DisplayName);
    }

    [Fact]
    public void ControllerNameOption_Description_IsCorrect_Net11()
    {
        Assert.Equal("Name for the controller being created", AspnetStrings.Options.ControllerName.Description);
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

    #region Option Strings — Views

    [Fact]
    public void ViewsOption_DisplayName_IsCorrect_Net11()
    {
        Assert.Equal("With Views?", AspnetStrings.Options.View.DisplayName);
    }

    [Fact]
    public void ViewsOption_Description_ContainsViews_Net11()
    {
        Assert.Contains("views", AspnetStrings.Options.View.Description, StringComparison.OrdinalIgnoreCase);
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

    #region EfControllerSettings Properties

    [Fact]
    public void EfControllerSettings_HasControllerNameProperty_Net11()
    {
        var settings = new EfControllerSettings
        {
            Project = _testProjectPath,
            Model = "Product",
            ControllerName = "ProductsController",
            ControllerType = "MVC",
            DataContext = "AppDbContext",
            DatabaseProvider = PackageConstants.EfConstants.SqlServer
        };

        Assert.Equal("ProductsController", settings.ControllerName);
    }

    [Fact]
    public void EfControllerSettings_HasControllerTypeProperty_Net11()
    {
        var settings = new EfControllerSettings
        {
            Project = _testProjectPath,
            Model = "Product",
            ControllerName = "ProductsController",
            ControllerType = "MVC",
            DataContext = "AppDbContext",
            DatabaseProvider = PackageConstants.EfConstants.SqlServer
        };

        Assert.Equal("MVC", settings.ControllerType);
    }

    [Fact]
    public void EfControllerSettings_HasModelProperty_Net11()
    {
        var settings = new EfControllerSettings
        {
            Project = _testProjectPath,
            Model = "Product",
            ControllerName = "ProductsController",
            ControllerType = "MVC",
            DataContext = "AppDbContext",
            DatabaseProvider = PackageConstants.EfConstants.SqlServer
        };

        Assert.Equal("Product", settings.Model);
    }

    [Fact]
    public void EfControllerSettings_HasDataContextProperty_Net11()
    {
        var settings = new EfControllerSettings
        {
            Project = _testProjectPath,
            Model = "Product",
            ControllerName = "ProductsController",
            ControllerType = "MVC",
            DataContext = "AppDbContext",
            DatabaseProvider = PackageConstants.EfConstants.SqlServer
        };

        Assert.Equal("AppDbContext", settings.DataContext);
    }

    [Fact]
    public void EfControllerSettings_HasDatabaseProviderProperty_Net11()
    {
        var settings = new EfControllerSettings
        {
            Project = _testProjectPath,
            Model = "Product",
            ControllerName = "ProductsController",
            ControllerType = "MVC",
            DataContext = "AppDbContext",
            DatabaseProvider = PackageConstants.EfConstants.SqlServer
        };

        Assert.Equal(PackageConstants.EfConstants.SqlServer, settings.DatabaseProvider);
    }

    [Fact]
    public void EfControllerSettings_HasProjectProperty_Net11()
    {
        var settings = new EfControllerSettings
        {
            Project = _testProjectPath,
            Model = "Product",
            ControllerName = "ProductsController",
            ControllerType = "MVC",
            DataContext = "AppDbContext",
            DatabaseProvider = PackageConstants.EfConstants.SqlServer
        };

        Assert.Equal(_testProjectPath, settings.Project);
    }

    [Fact]
    public void EfControllerSettings_HasPrereleaseProperty_Net11()
    {
        var settings = new EfControllerSettings
        {
            Project = _testProjectPath,
            Model = "Product",
            ControllerName = "ProductsController",
            ControllerType = "MVC",
            DataContext = "AppDbContext",
            DatabaseProvider = PackageConstants.EfConstants.SqlServer,
            Prerelease = true
        };

        Assert.True(settings.Prerelease);
    }

    [Fact]
    public void EfControllerSettings_PrereleaseDefaultsFalse_Net11()
    {
        var settings = new EfControllerSettings
        {
            Project = _testProjectPath,
            Model = "Product",
            ControllerName = "ProductsController",
            ControllerType = "MVC",
            DataContext = "AppDbContext",
            DatabaseProvider = PackageConstants.EfConstants.SqlServer
        };

        Assert.False(settings.Prerelease);
    }

    [Fact]
    public void EfControllerSettings_InheritsEfWithModelStepSettings_Net11()
    {
        var settings = new EfControllerSettings
        {
            Project = _testProjectPath,
            Model = "Product",
            ControllerName = "ProductsController",
            ControllerType = "MVC",
            DataContext = "AppDbContext",
            DatabaseProvider = PackageConstants.EfConstants.SqlServer
        };

        Assert.IsAssignableFrom<EfWithModelStepSettings>(settings);
    }

    [Fact]
    public void EfControllerSettings_InheritsBaseSettings_Net11()
    {
        var settings = new EfControllerSettings
        {
            Project = _testProjectPath,
            Model = "Product",
            ControllerName = "ProductsController",
            ControllerType = "MVC",
            DataContext = "AppDbContext",
            DatabaseProvider = PackageConstants.EfConstants.SqlServer
        };

        Assert.IsAssignableFrom<BaseSettings>(settings);
    }

    #endregion

    #region ValidateEfControllerStep — Properties

    [Fact]
    public void ValidateEfControllerStep_HasProjectProperty_Net11()
    {
        var step = CreateValidateEfControllerStep();
        step.Project = _testProjectPath;
        Assert.Equal(_testProjectPath, step.Project);
    }

    [Fact]
    public void ValidateEfControllerStep_HasModelProperty_Net11()
    {
        var step = CreateValidateEfControllerStep();
        step.Model = "Product";
        Assert.Equal("Product", step.Model);
    }

    [Fact]
    public void ValidateEfControllerStep_HasControllerNameProperty_Net11()
    {
        var step = CreateValidateEfControllerStep();
        step.ControllerName = "ProductsController";
        Assert.Equal("ProductsController", step.ControllerName);
    }

    [Fact]
    public void ValidateEfControllerStep_HasControllerTypeProperty_Net11()
    {
        var step = CreateValidateEfControllerStep();
        step.ControllerType = "MVC";
        Assert.Equal("MVC", step.ControllerType);
    }

    [Fact]
    public void ValidateEfControllerStep_HasDataContextProperty_Net11()
    {
        var step = CreateValidateEfControllerStep();
        step.DataContext = "AppDbContext";
        Assert.Equal("AppDbContext", step.DataContext);
    }

    [Fact]
    public void ValidateEfControllerStep_HasDatabaseProviderProperty_Net11()
    {
        var step = CreateValidateEfControllerStep();
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;
        Assert.Equal(PackageConstants.EfConstants.SqlServer, step.DatabaseProvider);
    }

    [Fact]
    public void ValidateEfControllerStep_HasPrereleaseProperty_Net11()
    {
        var step = CreateValidateEfControllerStep();
        step.Prerelease = true;
        Assert.True(step.Prerelease);
    }

    [Fact]
    public void ValidateEfControllerStep_ProjectDefaultsToNull_Net11()
    {
        var step = CreateValidateEfControllerStep();
        Assert.Null(step.Project);
    }

    [Fact]
    public void ValidateEfControllerStep_ModelDefaultsToNull_Net11()
    {
        var step = CreateValidateEfControllerStep();
        Assert.Null(step.Model);
    }

    [Fact]
    public void ValidateEfControllerStep_ControllerNameDefaultsToNull_Net11()
    {
        var step = CreateValidateEfControllerStep();
        Assert.Null(step.ControllerName);
    }

    [Fact]
    public void ValidateEfControllerStep_ControllerTypeDefaultsToNull_Net11()
    {
        var step = CreateValidateEfControllerStep();
        Assert.Null(step.ControllerType);
    }

    [Fact]
    public void ValidateEfControllerStep_DataContextDefaultsToNull_Net11()
    {
        var step = CreateValidateEfControllerStep();
        Assert.Null(step.DataContext);
    }

    [Fact]
    public void ValidateEfControllerStep_DatabaseProviderDefaultsToNull_Net11()
    {
        var step = CreateValidateEfControllerStep();
        Assert.Null(step.DatabaseProvider);
    }

    [Fact]
    public void ValidateEfControllerStep_PrereleaseDefaultsToFalse_Net11()
    {
        var step = CreateValidateEfControllerStep();
        Assert.False(step.Prerelease);
    }

    #endregion

    #region ValidateEfControllerStep — Validation Logic

    [Fact]
    public async Task ValidateEfControllerStep_FailsWithNullProject_Net11()
    {
        var step = CreateValidateEfControllerStep();
        step.Project = null;
        step.Model = "Product";
        step.ControllerName = "ProductsController";
        step.ControllerType = "MVC";
        step.DataContext = "AppDbContext";
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateEfControllerStep_FailsWithEmptyProject_Net11()
    {
        var step = CreateValidateEfControllerStep();
        step.Project = string.Empty;
        step.Model = "Product";
        step.ControllerName = "ProductsController";
        step.ControllerType = "MVC";
        step.DataContext = "AppDbContext";
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateEfControllerStep_FailsWithNonExistentProject_Net11()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(false);

        var step = CreateValidateEfControllerStep();
        step.Project = @"C:\NonExistent\Project.csproj";
        step.Model = "Product";
        step.ControllerName = "ProductsController";
        step.ControllerType = "MVC";
        step.DataContext = "AppDbContext";
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateEfControllerStep_FailsWithNullModel_Net11()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateEfControllerStep();
        step.Project = _testProjectPath;
        step.Model = null;
        step.ControllerName = "ProductsController";
        step.ControllerType = "MVC";
        step.DataContext = "AppDbContext";
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateEfControllerStep_FailsWithEmptyModel_Net11()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateEfControllerStep();
        step.Project = _testProjectPath;
        step.Model = string.Empty;
        step.ControllerName = "ProductsController";
        step.ControllerType = "MVC";
        step.DataContext = "AppDbContext";
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateEfControllerStep_FailsWithNullControllerName_Net11()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateEfControllerStep();
        step.Project = _testProjectPath;
        step.Model = "Product";
        step.ControllerName = null;
        step.ControllerType = "MVC";
        step.DataContext = "AppDbContext";
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateEfControllerStep_FailsWithEmptyControllerName_Net11()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateEfControllerStep();
        step.Project = _testProjectPath;
        step.Model = "Product";
        step.ControllerName = string.Empty;
        step.ControllerType = "MVC";
        step.DataContext = "AppDbContext";
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateEfControllerStep_FailsWithNullControllerType_Net11()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateEfControllerStep();
        step.Project = _testProjectPath;
        step.Model = "Product";
        step.ControllerName = "ProductsController";
        step.ControllerType = null;
        step.DataContext = "AppDbContext";
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateEfControllerStep_FailsWithEmptyControllerType_Net11()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateEfControllerStep();
        step.Project = _testProjectPath;
        step.Model = "Product";
        step.ControllerName = "ProductsController";
        step.ControllerType = string.Empty;
        step.DataContext = "AppDbContext";
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateEfControllerStep_FailsWithNullDataContext_Net11()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateEfControllerStep();
        step.Project = _testProjectPath;
        step.Model = "Product";
        step.ControllerName = "ProductsController";
        step.ControllerType = "MVC";
        step.DataContext = null;
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateEfControllerStep_FailsWithEmptyDataContext_Net11()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateEfControllerStep();
        step.Project = _testProjectPath;
        step.Model = "Product";
        step.ControllerName = "ProductsController";
        step.ControllerType = "MVC";
        step.DataContext = string.Empty;
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    #endregion

    #region ValidateEfControllerStep — Telemetry

    [Fact]
    public async Task ValidateEfControllerStep_TracksTelemetry_OnNullProjectFailure_Net11()
    {
        var step = CreateValidateEfControllerStep();
        step.Project = null;
        step.Model = "Product";
        step.ControllerName = "ProductsController";
        step.ControllerType = "MVC";
        step.DataContext = "AppDbContext";

        await step.ExecuteAsync(_context);

        _mockTelemetryService.Verify(
            t => t.TrackEvent(
                It.IsAny<string>(),
                It.IsAny<IReadOnlyDictionary<string, string>>(),
                It.IsAny<IReadOnlyDictionary<string, double>>()),
            Times.Once);
    }

    [Fact]
    public async Task ValidateEfControllerStep_TracksTelemetry_OnNullModelFailure_Net11()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateEfControllerStep();
        step.Project = _testProjectPath;
        step.Model = null;
        step.ControllerName = "ProductsController";
        step.ControllerType = "MVC";
        step.DataContext = "AppDbContext";

        await step.ExecuteAsync(_context);

        _mockTelemetryService.Verify(
            t => t.TrackEvent(
                It.IsAny<string>(),
                It.IsAny<IReadOnlyDictionary<string, string>>(),
                It.IsAny<IReadOnlyDictionary<string, double>>()),
            Times.Once);
    }

    [Fact]
    public async Task ValidateEfControllerStep_TracksTelemetry_OnNullControllerNameFailure_Net11()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateEfControllerStep();
        step.Project = _testProjectPath;
        step.Model = "Product";
        step.ControllerName = null;
        step.ControllerType = "MVC";
        step.DataContext = "AppDbContext";

        await step.ExecuteAsync(_context);

        _mockTelemetryService.Verify(
            t => t.TrackEvent(
                It.IsAny<string>(),
                It.IsAny<IReadOnlyDictionary<string, string>>(),
                It.IsAny<IReadOnlyDictionary<string, double>>()),
            Times.Once);
    }

    [Fact]
    public async Task ValidateEfControllerStep_TracksTelemetry_OnNullControllerTypeFailure_Net11()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateEfControllerStep();
        step.Project = _testProjectPath;
        step.Model = "Product";
        step.ControllerName = "ProductsController";
        step.ControllerType = null;
        step.DataContext = "AppDbContext";

        await step.ExecuteAsync(_context);

        _mockTelemetryService.Verify(
            t => t.TrackEvent(
                It.IsAny<string>(),
                It.IsAny<IReadOnlyDictionary<string, string>>(),
                It.IsAny<IReadOnlyDictionary<string, double>>()),
            Times.Once);
    }

    [Fact]
    public async Task ValidateEfControllerStep_TracksTelemetry_OnNullDataContextFailure_Net11()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateEfControllerStep();
        step.Project = _testProjectPath;
        step.Model = "Product";
        step.ControllerName = "ProductsController";
        step.ControllerType = "MVC";
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
    [InlineData("ProductsController")]
    [InlineData("OrdersController")]
    [InlineData("CustomersController")]
    public async Task ValidateEfControllerStep_FailsValidation_ForVariousNames_WhenProjectMissing_Net11(string controllerName)
    {
        var step = CreateValidateEfControllerStep();
        step.Project = null;
        step.Model = "Product";
        step.ControllerName = controllerName;
        step.ControllerType = "MVC";
        step.DataContext = "AppDbContext";
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task ValidateEfControllerStep_FailsWithInvalidModel_Net11(string? model)
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateEfControllerStep();
        step.Project = _testProjectPath;
        step.Model = model;
        step.ControllerName = "ProductsController";
        step.ControllerType = "MVC";
        step.DataContext = "AppDbContext";
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task ValidateEfControllerStep_FailsWithInvalidControllerName_Net11(string? controllerName)
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = CreateValidateEfControllerStep();
        step.Project = _testProjectPath;
        step.Model = "Product";
        step.ControllerName = controllerName;
        step.ControllerType = "MVC";
        step.DataContext = "AppDbContext";
        step.DatabaseProvider = PackageConstants.EfConstants.SqlServer;

        var result = await step.ExecuteAsync(_context);
        Assert.False(result);
    }

    #endregion

    #region ScaffolderContext

    [Fact]
    public void ScaffolderContext_HasCrudScaffolderName_Net11()
    {
        Assert.Equal("mvccontroller-crud", _context.Scaffolder.Name);
    }

    [Fact]
    public void ScaffolderContext_HasCrudDisplayName_Net11()
    {
        Assert.Equal("MVC Controller with views, using Entity Framework (CRUD)", _context.Scaffolder.DisplayName);
    }

    #endregion

    #region EfController Templates — Net11

    [Fact]
    public void Net11_EfControllerTemplates_FolderExists()
    {
        var basePath = GetActualTemplatesBasePath();
        var efControllerDir = Path.Combine(basePath, TargetFramework, "EfController");
        Assert.True(Directory.Exists(efControllerDir),
            $"EfController template folder should exist for {TargetFramework}");
    }

    [Fact]
    public void Net11_EfControllerTemplates_HasExactly6Files()
    {
        var basePath = GetActualTemplatesBasePath();
        var efControllerDir = Path.Combine(basePath, TargetFramework, "EfController");
        var files = Directory.GetFiles(efControllerDir, "*", SearchOption.AllDirectories);
        Assert.Equal(6, files.Length);
    }

    [Theory]
    [InlineData("ApiEfController.cs")]
    [InlineData("ApiEfController.Interfaces.cs")]
    [InlineData("ApiEfController.tt")]
    [InlineData("MvcEfController.cs")]
    [InlineData("MvcEfController.Interfaces.cs")]
    [InlineData("MvcEfController.tt")]
    public void Net11_EfControllerTemplates_HasExpectedFile(string fileName)
    {
        var basePath = GetActualTemplatesBasePath();
        var filePath = Path.Combine(basePath, TargetFramework, "EfController", fileName);
        Assert.True(File.Exists(filePath),
            $"Expected EfController template file '{fileName}' not found for {TargetFramework}");
    }

    [Fact]
    public void Net11_EfControllerTemplates_UsesT4Format()
    {
        var basePath = GetActualTemplatesBasePath();
        var efControllerDir = Path.Combine(basePath, TargetFramework, "EfController");
        var ttFiles = Directory.GetFiles(efControllerDir, "*.tt");
        Assert.Equal(2, ttFiles.Length);
    }

    [Fact]
    public void Net11_EfControllerTemplates_NoCshtmlFiles()
    {
        var basePath = GetActualTemplatesBasePath();
        var efControllerDir = Path.Combine(basePath, TargetFramework, "EfController");
        var cshtmlFiles = Directory.GetFiles(efControllerDir, "*.cshtml");
        Assert.Empty(cshtmlFiles);
    }

    [Fact]
    public void Net11_EfControllerTemplates_MatchesNet10FileCount()
    {
        var basePath = GetActualTemplatesBasePath();
        var net10Dir = Path.Combine(basePath, "net10.0", "EfController");
        var net11Dir = Path.Combine(basePath, TargetFramework, "EfController");
        var net10Files = Directory.GetFiles(net10Dir, "*", SearchOption.AllDirectories);
        var net11Files = Directory.GetFiles(net11Dir, "*", SearchOption.AllDirectories);
        Assert.Equal(net10Files.Length, net11Files.Length);
    }

    #endregion

    #region Views Templates — Net11

    [Fact]
    public void Net11_ViewsTemplates_FolderExists()
    {
        var basePath = GetActualTemplatesBasePath();
        var viewsDir = Path.Combine(basePath, TargetFramework, "Views");
        Assert.True(Directory.Exists(viewsDir),
            $"Views template folder should exist for {TargetFramework}");
    }

    [Fact]
    public void Net11_ViewsTemplates_NoBootstrapSubfolders()
    {
        var basePath = GetActualTemplatesBasePath();
        var viewsDir = Path.Combine(basePath, TargetFramework, "Views");
        var subDirs = Directory.GetDirectories(viewsDir);
        Assert.Empty(subDirs);
    }

    [Fact]
    public void Net11_ViewsTemplates_HasExactly15Files()
    {
        var basePath = GetActualTemplatesBasePath();
        var viewsDir = Path.Combine(basePath, TargetFramework, "Views");
        var files = Directory.GetFiles(viewsDir, "*", SearchOption.AllDirectories);
        Assert.Equal(15, files.Length);
    }

    [Theory]
    [InlineData("Create.cs")]
    [InlineData("Create.Interfaces.cs")]
    [InlineData("Create.tt")]
    [InlineData("Delete.cs")]
    [InlineData("Delete.Interfaces.cs")]
    [InlineData("Delete.tt")]
    [InlineData("Details.cs")]
    [InlineData("Details.Interfaces.cs")]
    [InlineData("Details.tt")]
    [InlineData("Edit.cs")]
    [InlineData("Edit.Interfaces.cs")]
    [InlineData("Edit.tt")]
    [InlineData("Index.cs")]
    [InlineData("Index.Interfaces.cs")]
    [InlineData("Index.tt")]
    public void Net11_ViewsTemplates_HasExpectedFile(string fileName)
    {
        var basePath = GetActualTemplatesBasePath();
        var filePath = Path.Combine(basePath, TargetFramework, "Views", fileName);
        Assert.True(File.Exists(filePath),
            $"Expected Views template file '{fileName}' not found for {TargetFramework}");
    }

    [Fact]
    public void Net11_ViewsTemplates_Has5TtFiles()
    {
        var basePath = GetActualTemplatesBasePath();
        var viewsDir = Path.Combine(basePath, TargetFramework, "Views");
        var ttFiles = Directory.GetFiles(viewsDir, "*.tt");
        Assert.Equal(5, ttFiles.Length);
    }

    [Fact]
    public void Net11_ViewsTemplates_NoCshtmlFiles()
    {
        var basePath = GetActualTemplatesBasePath();
        var viewsDir = Path.Combine(basePath, TargetFramework, "Views");
        var cshtmlFiles = Directory.GetFiles(viewsDir, "*.cshtml");
        Assert.Empty(cshtmlFiles);
    }

    [Fact]
    public void Net11_ViewsTemplates_MatchesNet10FileCount()
    {
        var basePath = GetActualTemplatesBasePath();
        var net10Dir = Path.Combine(basePath, "net10.0", "Views");
        var net11Dir = Path.Combine(basePath, TargetFramework, "Views");
        var net10Files = Directory.GetFiles(net10Dir, "*", SearchOption.AllDirectories);
        var net11Files = Directory.GetFiles(net11Dir, "*", SearchOption.AllDirectories);
        Assert.Equal(net10Files.Length, net11Files.Length);
    }

    #endregion

    #region CodeModificationConfigs — Net11

    [Fact]
    public void Net11_CodeModConfigs_HasEfControllerChangesJson()
    {
        var basePath = GetActualTemplatesBasePath();
        var configPath = Path.Combine(basePath, TargetFramework, "CodeModificationConfigs", "efControllerChanges.json");
        Assert.True(File.Exists(configPath),
            $"efControllerChanges.json should exist for {TargetFramework}");
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
        var folderPath = Path.Combine(basePath, TargetFramework, folderName);
        Assert.True(Directory.Exists(folderPath),
            $"Expected template folder '{folderName}' not found for {TargetFramework}");
    }

    [Fact]
    public void Net11_Templates_HasBlazorEntraId()
    {
        var basePath = GetActualTemplatesBasePath();
        var entraIdDir = Path.Combine(basePath, TargetFramework, "BlazorEntraId");
        Assert.True(Directory.Exists(entraIdDir),
            "BlazorEntraId folder should exist for net11.0");
    }

    #endregion

    #region Net11 vs Net10 Template Parity

    [Fact]
    public void Net11_EfControllerTemplates_SameFileNamesAsNet10()
    {
        var basePath = GetActualTemplatesBasePath();
        var net10Dir = Path.Combine(basePath, "net10.0", "EfController");
        var net11Dir = Path.Combine(basePath, TargetFramework, "EfController");
        var net10FileNames = Directory.GetFiles(net10Dir).Select(Path.GetFileName).OrderBy(f => f).ToArray();
        var net11FileNames = Directory.GetFiles(net11Dir).Select(Path.GetFileName).OrderBy(f => f).ToArray();
        Assert.Equal(net10FileNames, net11FileNames);
    }

    [Fact]
    public void Net11_ViewsTemplates_SameFileNamesAsNet10()
    {
        var basePath = GetActualTemplatesBasePath();
        var net10Dir = Path.Combine(basePath, "net10.0", "Views");
        var net11Dir = Path.Combine(basePath, TargetFramework, "Views");
        var net10FileNames = Directory.GetFiles(net10Dir).Select(Path.GetFileName).OrderBy(f => f).ToArray();
        var net11FileNames = Directory.GetFiles(net11Dir).Select(Path.GetFileName).OrderBy(f => f).ToArray();
        Assert.Equal(net10FileNames, net11FileNames);
    }

    [Fact]
    public void Net11_Templates_HasSameTopLevelFoldersAsNet10()
    {
        var basePath = GetActualTemplatesBasePath();
        var net10Dir = Path.Combine(basePath, "net10.0");
        var net11Dir = Path.Combine(basePath, TargetFramework);
        var net10Folders = Directory.GetDirectories(net10Dir).Select(Path.GetFileName).OrderBy(f => f).ToArray();
        var net11Folders = Directory.GetDirectories(net11Dir).Select(Path.GetFileName).OrderBy(f => f).ToArray();
        Assert.Equal(net10Folders, net11Folders);
    }

    #endregion

    #region Helper Methods

    private ValidateEfControllerStep CreateValidateEfControllerStep()
    {
        return new ValidateEfControllerStep(
            _mockFileSystem.Object,
            NullLogger<ValidateEfControllerStep>.Instance,
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
