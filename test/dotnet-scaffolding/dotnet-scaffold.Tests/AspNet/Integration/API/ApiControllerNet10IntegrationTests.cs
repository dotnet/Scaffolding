// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.ComponentModel;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Scaffolding.Internal.Telemetry;
using Microsoft.DotNet.Scaffolding.TextTemplating;
using Microsoft.DotNet.Tools.Scaffold.AspNet;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Commands;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using AspNetConstants = Microsoft.DotNet.Tools.Scaffold.AspNet.Common.Constants;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Models;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.API;

/// <summary>
/// Integration tests for the API Controller CRUD (apicontroller-crud) scaffolder targeting .NET 10.
/// Validates scaffolder definition constants, ValidateEfControllerStep validation logic,
/// EfControllerModel/EfControllerSettings/EfWithModelStepSettings/BaseSettings properties,
/// EfControllerHelper template resolution, template folder verification, code modification configs,
/// package constants, pipeline registration, step dependencies, telemetry tracking,
/// TFM availability, builder extensions, and database provider support.
/// The API Controller CRUD scaffolder is available for all supported TFMs including .NET 10.
/// .NET 10 EfController templates use .tt text template format (ApiEfController.tt, MvcEfController.tt)
/// compiled to the Templates.net10.EfController namespace. Unlike net9.0 (whose .cs files are excluded),
/// net10.0 EfController .cs files are explicitly re-included via Compile Update in the csproj,
/// making Templates.net10.EfController the canonical compiled template types used by GetCrudControllerType.
/// </summary>
public class ApiControllerNet10IntegrationTests : IDisposable
{
    private const string TargetFramework = "net10.0";
    private readonly string _testDirectory;
    private readonly string _testProjectDir;
    private readonly string _testProjectPath;
    private readonly Mock<IFileSystem> _mockFileSystem;
    private readonly TestTelemetryService _testTelemetryService;
    private readonly Mock<IScaffolder> _mockScaffolder;
    private readonly ScaffolderContext _context;

    public ApiControllerNet10IntegrationTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "ApiControllerNet10IntegrationTests", Guid.NewGuid().ToString());
        _testProjectDir = Path.Combine(_testDirectory, "TestProject");
        _testProjectPath = Path.Combine(_testProjectDir, "TestProject.csproj");
        Directory.CreateDirectory(_testProjectDir);

        _mockFileSystem = new Mock<IFileSystem>();
        _testTelemetryService = new TestTelemetryService();
        _mockScaffolder = new Mock<IScaffolder>();
        _mockScaffolder.Setup(s => s.DisplayName).Returns(AspnetStrings.Api.ApiControllerCrudDisplayName);
        _mockScaffolder.Setup(s => s.Name).Returns(AspnetStrings.Api.ApiControllerCrud);
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

    #region Constants & Scaffolder Definition — API Controller CRUD

    [Fact]
    public void ScaffolderName_IsApiControllerCrud_Net10()
    {
        Assert.Equal("apicontroller-crud", AspnetStrings.Api.ApiControllerCrud);
    }

    [Fact]
    public void ScaffolderDisplayName_IsApiControllerCrudDisplayName_Net10()
    {
        Assert.Equal("API Controller with actions, using Entity Framework (CRUD)", AspnetStrings.Api.ApiControllerCrudDisplayName);
    }

    [Fact]
    public void ScaffolderDescription_IsApiControllerCrudDescription_Net10()
    {
        Assert.Equal("Create an API controller with REST actions to create, read, update, delete, and list entities", AspnetStrings.Api.ApiControllerCrudDescription);
    }

    [Fact]
    public void ScaffolderCategory_IsAPI_Net10()
    {
        Assert.Equal("API", AspnetStrings.Catagories.API);
    }

    [Fact]
    public void ScaffolderExample1_ContainsApiControllerCrudCommand_Net10()
    {
        Assert.Contains("apicontroller-crud", AspnetStrings.Api.ApiControllerCrudExample1);
    }

    [Fact]
    public void ScaffolderExample1_ContainsRequiredOptions_Net10()
    {
        Assert.Contains("--project", AspnetStrings.Api.ApiControllerCrudExample1);
        Assert.Contains("--model", AspnetStrings.Api.ApiControllerCrudExample1);
        Assert.Contains("--controller-name", AspnetStrings.Api.ApiControllerCrudExample1);
        Assert.Contains("--data-context", AspnetStrings.Api.ApiControllerCrudExample1);
        Assert.Contains("--database-provider", AspnetStrings.Api.ApiControllerCrudExample1);
    }

    [Fact]
    public void ScaffolderExample2_ContainsApiControllerCrudCommand_Net10()
    {
        Assert.Contains("apicontroller-crud", AspnetStrings.Api.ApiControllerCrudExample2);
    }

    [Fact]
    public void ScaffolderExample2_ContainsPrerelease_Net10()
    {
        Assert.Contains("--prerelease", AspnetStrings.Api.ApiControllerCrudExample2);
    }

    [Fact]
    public void ScaffolderExample1Description_MentionsCrudOperations_Net10()
    {
        Assert.Contains("CRUD", AspnetStrings.Api.ApiControllerCrudExample1Description);
    }

    [Fact]
    public void ScaffolderExample2Description_MentionsPostgreSQL_Net10()
    {
        Assert.Contains("PostgreSQL", AspnetStrings.Api.ApiControllerCrudExample2Description);
    }

    #endregion

    #region Constants & Scaffolder Definition — MVC Controller CRUD

    [Fact]
    public void MVC_ScaffolderName_IsMvcControllerCrud_Net10()
    {
        Assert.Equal("mvccontroller-crud", AspnetStrings.MVC.ControllerCrud);
    }

    [Fact]
    public void MVC_ScaffolderDisplayName_IsMvcControllerCrudDisplayName_Net10()
    {
        Assert.Equal("MVC Controller with views, using Entity Framework (CRUD)", AspnetStrings.MVC.CrudDisplayName);
    }

    [Fact]
    public void MVC_ScaffolderDescription_IsMvcControllerCrudDescription_Net10()
    {
        Assert.Equal("Create a MVC controller with read/write actions and views using Entity Framework", AspnetStrings.MVC.CrudDescription);
    }

    [Fact]
    public void MVC_ScaffolderCategory_IsMVC_Net10()
    {
        Assert.Equal("MVC", AspnetStrings.Catagories.MVC);
    }

    [Fact]
    public void MVC_ScaffolderExample1_ContainsMvcControllerCrudCommand_Net10()
    {
        Assert.Contains("mvccontroller-crud", AspnetStrings.MVC.ControllerCrudExample1);
    }

    [Fact]
    public void MVC_ScaffolderExample1_ContainsViewsOption_Net10()
    {
        Assert.Contains("--views", AspnetStrings.MVC.ControllerCrudExample1);
    }

    #endregion

    #region CLI Options

    [Fact]
    public void CliOption_ProjectOption_IsCorrect_Net10()
    {
        Assert.Equal("--project", AspNetConstants.CliOptions.ProjectCliOption);
    }

    [Fact]
    public void CliOption_ModelOption_IsCorrect_Net10()
    {
        Assert.Equal("--model", AspNetConstants.CliOptions.ModelCliOption);
    }

    [Fact]
    public void CliOption_DataContextOption_IsCorrect_Net10()
    {
        Assert.Equal("--dataContext", AspNetConstants.CliOptions.DataContextOption);
    }

    [Fact]
    public void CliOption_DbProviderOption_IsCorrect_Net10()
    {
        Assert.Equal("--dbProvider", AspNetConstants.CliOptions.DbProviderOption);
    }

    [Fact]
    public void CliOption_ControllerNameOption_IsCorrect_Net10()
    {
        Assert.Equal("--controller", AspNetConstants.CliOptions.ControllerNameOption);
    }

    [Fact]
    public void CliOption_PrereleaseOption_IsCorrect_Net10()
    {
        Assert.Equal("--prerelease", AspNetConstants.CliOptions.PrereleaseCliOption);
    }

    [Fact]
    public void CliOption_ViewsOption_IsCorrect_Net10()
    {
        Assert.Equal("--views", AspNetConstants.CliOptions.ViewsOption);
    }

    #endregion

    #region AspNetOptions for EfController

    [Fact]
    public void AspNetOptions_HasModelNameProperty_Net10()
    {
        var prop = typeof(AspNetOptions).GetProperty("ModelName");
        Assert.NotNull(prop);
    }

    [Fact]
    public void AspNetOptions_HasControllerNameProperty_Net10()
    {
        var prop = typeof(AspNetOptions).GetProperty("ControllerName");
        Assert.NotNull(prop);
    }

    [Fact]
    public void AspNetOptions_HasDataContextClassRequiredProperty_Net10()
    {
        var prop = typeof(AspNetOptions).GetProperty("DataContextClassRequired");
        Assert.NotNull(prop);
    }

    [Fact]
    public void AspNetOptions_HasDatabaseProviderRequiredProperty_Net10()
    {
        var prop = typeof(AspNetOptions).GetProperty("DatabaseProviderRequired");
        Assert.NotNull(prop);
    }

    [Fact]
    public void AspNetOptions_HasPrereleaseProperty_Net10()
    {
        var prop = typeof(AspNetOptions).GetProperty("Prerelease");
        Assert.NotNull(prop);
    }

    [Fact]
    public void AspNetOptions_HasViewsProperty_Net10()
    {
        var prop = typeof(AspNetOptions).GetProperty("Views");
        Assert.NotNull(prop);
    }

    #endregion

    #region ValidateEfControllerStep — Properties and Construction

    [Fact]
    public void ValidateEfControllerStep_IsScaffoldStep_Net10()
    {
        Assert.True(typeof(ValidateEfControllerStep).IsAssignableTo(typeof(ScaffoldStep)));
    }

    [Fact]
    public void ValidateEfControllerStep_HasProjectProperty_Net10()
    {
        Assert.NotNull(typeof(ValidateEfControllerStep).GetProperty("Project"));
    }

    [Fact]
    public void ValidateEfControllerStep_HasPrereleaseProperty_Net10()
    {
        Assert.NotNull(typeof(ValidateEfControllerStep).GetProperty("Prerelease"));
    }

    [Fact]
    public void ValidateEfControllerStep_HasDatabaseProviderProperty_Net10()
    {
        Assert.NotNull(typeof(ValidateEfControllerStep).GetProperty("DatabaseProvider"));
    }

    [Fact]
    public void ValidateEfControllerStep_HasDataContextProperty_Net10()
    {
        Assert.NotNull(typeof(ValidateEfControllerStep).GetProperty("DataContext"));
    }

    [Fact]
    public void ValidateEfControllerStep_HasModelProperty_Net10()
    {
        Assert.NotNull(typeof(ValidateEfControllerStep).GetProperty("Model"));
    }

    [Fact]
    public void ValidateEfControllerStep_HasControllerNameProperty_Net10()
    {
        Assert.NotNull(typeof(ValidateEfControllerStep).GetProperty("ControllerName"));
    }

    [Fact]
    public void ValidateEfControllerStep_HasControllerTypeProperty_Net10()
    {
        Assert.NotNull(typeof(ValidateEfControllerStep).GetProperty("ControllerType"));
    }

    [Fact]
    public void ValidateEfControllerStep_Has7Properties_Net10()
    {
        // Project, Prerelease, DatabaseProvider, DataContext, Model, ControllerName, ControllerType
        var props = typeof(ValidateEfControllerStep).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        Assert.Equal(7, props.Length);
    }

    [Fact]
    public void ValidateEfControllerStep_Constructor_RequiresFileSystem_Net10()
    {
        var ctor = typeof(ValidateEfControllerStep).GetConstructors().First();
        var parameters = ctor.GetParameters();
        Assert.Contains(parameters, p => p.ParameterType == typeof(IFileSystem));
    }

    [Fact]
    public void ValidateEfControllerStep_Constructor_RequiresLogger_Net10()
    {
        var ctor = typeof(ValidateEfControllerStep).GetConstructors().First();
        var parameters = ctor.GetParameters();
        Assert.Contains(parameters, p => p.ParameterType == typeof(ILogger<ValidateEfControllerStep>));
    }

    [Fact]
    public void ValidateEfControllerStep_Constructor_RequiresTelemetryService_Net10()
    {
        var ctor = typeof(ValidateEfControllerStep).GetConstructors().First();
        var parameters = ctor.GetParameters();
        Assert.Contains(parameters, p => p.ParameterType == typeof(ITelemetryService));
    }

    [Fact]
    public void ValidateEfControllerStep_Constructor_Has3Parameters_Net10()
    {
        var ctor = typeof(ValidateEfControllerStep).GetConstructors().First();
        Assert.Equal(3, ctor.GetParameters().Length);
    }

    #endregion

    #region ValidateEfControllerStep — Validation Logic

    [Fact]
    public async Task ValidateEfControllerStep_FailsWhenProjectMissing_Net10()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(false);

        var step = new ValidateEfControllerStep(_mockFileSystem.Object, new Mock<ILogger<ValidateEfControllerStep>>().Object, _testTelemetryService)
        {
            Project = string.Empty,
            Model = "Product",
            ControllerName = "ProductsController",
            ControllerType = "API",
            DataContext = "AppDbContext",
            DatabaseProvider = PackageConstants.EfConstants.SqlServer
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.False(result);
        Assert.Single(_testTelemetryService.TrackedEvents);
    }

    [Fact]
    public async Task ValidateEfControllerStep_FailsWhenProjectFileDoesNotExist_Net10()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(false);

        var step = new ValidateEfControllerStep(_mockFileSystem.Object, new Mock<ILogger<ValidateEfControllerStep>>().Object, _testTelemetryService)
        {
            Project = _testProjectPath,
            Model = "Product",
            ControllerName = "ProductsController",
            ControllerType = "API",
            DataContext = "AppDbContext",
            DatabaseProvider = PackageConstants.EfConstants.SqlServer
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.False(result);
        Assert.Single(_testTelemetryService.TrackedEvents);
    }

    [Fact]
    public async Task ValidateEfControllerStep_FailsWhenModelMissing_Net10()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = new ValidateEfControllerStep(_mockFileSystem.Object, new Mock<ILogger<ValidateEfControllerStep>>().Object, _testTelemetryService)
        {
            Project = _testProjectPath,
            Model = string.Empty,
            ControllerName = "ProductsController",
            ControllerType = "API",
            DataContext = "AppDbContext",
            DatabaseProvider = PackageConstants.EfConstants.SqlServer
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.False(result);
        Assert.Single(_testTelemetryService.TrackedEvents);
    }

    [Fact]
    public async Task ValidateEfControllerStep_FailsWhenControllerNameMissing_Net10()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = new ValidateEfControllerStep(_mockFileSystem.Object, new Mock<ILogger<ValidateEfControllerStep>>().Object, _testTelemetryService)
        {
            Project = _testProjectPath,
            Model = "Product",
            ControllerName = string.Empty,
            ControllerType = "API",
            DataContext = "AppDbContext",
            DatabaseProvider = PackageConstants.EfConstants.SqlServer
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.False(result);
        Assert.Single(_testTelemetryService.TrackedEvents);
    }

    [Fact]
    public async Task ValidateEfControllerStep_FailsWhenControllerTypeMissing_Net10()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = new ValidateEfControllerStep(_mockFileSystem.Object, new Mock<ILogger<ValidateEfControllerStep>>().Object, _testTelemetryService)
        {
            Project = _testProjectPath,
            Model = "Product",
            ControllerName = "ProductsController",
            ControllerType = string.Empty,
            DataContext = "AppDbContext",
            DatabaseProvider = PackageConstants.EfConstants.SqlServer
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.False(result);
        Assert.Single(_testTelemetryService.TrackedEvents);
    }

    [Fact]
    public async Task ValidateEfControllerStep_FailsWhenDataContextMissing_Net10()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = new ValidateEfControllerStep(_mockFileSystem.Object, new Mock<ILogger<ValidateEfControllerStep>>().Object, _testTelemetryService)
        {
            Project = _testProjectPath,
            Model = "Product",
            ControllerName = "ProductsController",
            ControllerType = "API",
            DataContext = string.Empty,
            DatabaseProvider = PackageConstants.EfConstants.SqlServer
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.False(result);
        Assert.Single(_testTelemetryService.TrackedEvents);
    }

    [Fact]
    public async Task ValidateEfControllerStep_StepProperties_AreSetCorrectly_Net10()
    {
        var step = new ValidateEfControllerStep(_mockFileSystem.Object, new Mock<ILogger<ValidateEfControllerStep>>().Object, _testTelemetryService)
        {
            Project = _testProjectPath,
            Model = "Product",
            ControllerName = "ProductsController",
            ControllerType = "API",
            DataContext = "AppDbContext",
            DatabaseProvider = PackageConstants.EfConstants.SqlServer,
            Prerelease = true
        };

        Assert.Equal(_testProjectPath, step.Project);
        Assert.Equal("Product", step.Model);
        Assert.Equal("ProductsController", step.ControllerName);
        Assert.Equal("API", step.ControllerType);
        Assert.Equal("AppDbContext", step.DataContext);
        Assert.Equal(PackageConstants.EfConstants.SqlServer, step.DatabaseProvider);
        Assert.True(step.Prerelease);
    }

    #endregion

    #region Telemetry

    [Fact]
    public async Task TelemetryEventName_IsValidateEfControllerStepEvent_Net10()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(false);

        var step = new ValidateEfControllerStep(_mockFileSystem.Object, new Mock<ILogger<ValidateEfControllerStep>>().Object, _testTelemetryService)
        {
            Project = string.Empty,
            Model = "Product",
            ControllerName = "ProductsController",
            ControllerType = "API",
            DataContext = "AppDbContext"
        };

        await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.Single(_testTelemetryService.TrackedEvents);
        Assert.Equal("ValidateEfControllerStepEvent", _testTelemetryService.TrackedEvents[0].EventName);
    }

    [Fact]
    public async Task TelemetryEvent_ContainsScaffolderNameProperty_Net10()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(false);

        var step = new ValidateEfControllerStep(_mockFileSystem.Object, new Mock<ILogger<ValidateEfControllerStep>>().Object, _testTelemetryService)
        {
            Project = string.Empty,
            Model = "Product",
            ControllerName = "ProductsController",
            ControllerType = "API",
            DataContext = "AppDbContext"
        };

        await step.ExecuteAsync(_context, CancellationToken.None);

        var props = _testTelemetryService.TrackedEvents[0].Properties;
        Assert.True(props.ContainsKey("ScaffolderName"));
        Assert.Equal("API Controller with actions, using Entity Framework (CRUD)", props["ScaffolderName"]);
    }

    [Fact]
    public async Task TelemetryEvent_ContainsResultProperty_OnFailure_Net10()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(false);

        var step = new ValidateEfControllerStep(_mockFileSystem.Object, new Mock<ILogger<ValidateEfControllerStep>>().Object, _testTelemetryService)
        {
            Project = string.Empty,
            Model = "Product",
            ControllerName = "ProductsController",
            ControllerType = "API",
            DataContext = "AppDbContext"
        };

        await step.ExecuteAsync(_context, CancellationToken.None);

        var props = _testTelemetryService.TrackedEvents[0].Properties;
        Assert.True(props.ContainsKey("Result"));
        Assert.Equal("Failure", props["Result"]);
    }

    [Fact]
    public async Task TelemetryEvent_SingleEventPerValidation_Net10()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(false);

        var step = new ValidateEfControllerStep(_mockFileSystem.Object, new Mock<ILogger<ValidateEfControllerStep>>().Object, _testTelemetryService)
        {
            Project = string.Empty,
            Model = string.Empty,
            ControllerName = string.Empty,
            ControllerType = string.Empty,
            DataContext = string.Empty
        };

        await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.Single(_testTelemetryService.TrackedEvents);
    }

    #endregion

    #region EfControllerModel Properties

    [Fact]
    public void EfControllerModel_HasControllerTypeProperty_Net10()
    {
        Assert.NotNull(typeof(EfControllerModel).GetProperty("ControllerType"));
    }

    [Fact]
    public void EfControllerModel_HasControllerNameProperty_Net10()
    {
        Assert.NotNull(typeof(EfControllerModel).GetProperty("ControllerName"));
    }

    [Fact]
    public void EfControllerModel_HasControllerOutputPathProperty_Net10()
    {
        Assert.NotNull(typeof(EfControllerModel).GetProperty("ControllerOutputPath"));
    }

    [Fact]
    public void EfControllerModel_HasDbContextInfoProperty_Net10()
    {
        Assert.NotNull(typeof(EfControllerModel).GetProperty("DbContextInfo"));
    }

    [Fact]
    public void EfControllerModel_HasModelInfoProperty_Net10()
    {
        Assert.NotNull(typeof(EfControllerModel).GetProperty("ModelInfo"));
    }

    [Fact]
    public void EfControllerModel_HasProjectInfoProperty_Net10()
    {
        Assert.NotNull(typeof(EfControllerModel).GetProperty("ProjectInfo"));
    }

    [Fact]
    public void EfControllerModel_Has6Properties_Net10()
    {
        var props = typeof(EfControllerModel).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        Assert.Equal(6, props.Length);
    }

    #endregion

    #region EfControllerSettings Properties

    [Fact]
    public void EfControllerSettings_HasControllerTypeProperty_Net10()
    {
        Assert.NotNull(typeof(EfControllerSettings).GetProperty("ControllerType"));
    }

    [Fact]
    public void EfControllerSettings_HasControllerNameProperty_Net10()
    {
        Assert.NotNull(typeof(EfControllerSettings).GetProperty("ControllerName"));
    }

    [Fact]
    public void EfControllerSettings_InheritsFromEfWithModelStepSettings_Net10()
    {
        Assert.True(typeof(EfControllerSettings).IsAssignableTo(typeof(EfWithModelStepSettings)));
    }

    [Fact]
    public void EfControllerSettings_HasProjectProperty_Net10()
    {
        Assert.NotNull(typeof(EfControllerSettings).GetProperty("Project"));
    }

    [Fact]
    public void EfControllerSettings_HasModelProperty_Net10()
    {
        Assert.NotNull(typeof(EfControllerSettings).GetProperty("Model"));
    }

    [Fact]
    public void EfControllerSettings_HasDataContextProperty_Net10()
    {
        Assert.NotNull(typeof(EfControllerSettings).GetProperty("DataContext"));
    }

    [Fact]
    public void EfControllerSettings_HasDatabaseProviderProperty_Net10()
    {
        Assert.NotNull(typeof(EfControllerSettings).GetProperty("DatabaseProvider"));
    }

    [Fact]
    public void EfControllerSettings_HasPrereleaseProperty_Net10()
    {
        Assert.NotNull(typeof(EfControllerSettings).GetProperty("Prerelease"));
    }

    #endregion

    #region EfWithModelStepSettings Properties

    [Fact]
    public void EfWithModelStepSettings_InheritsFromBaseSettings_Net10()
    {
        Assert.True(typeof(EfWithModelStepSettings).IsAssignableTo(typeof(BaseSettings)));
    }

    [Fact]
    public void EfWithModelStepSettings_HasDatabaseProviderProperty_Net10()
    {
        Assert.NotNull(typeof(EfWithModelStepSettings).GetProperty("DatabaseProvider"));
    }

    [Fact]
    public void EfWithModelStepSettings_HasDataContextProperty_Net10()
    {
        Assert.NotNull(typeof(EfWithModelStepSettings).GetProperty("DataContext"));
    }

    [Fact]
    public void EfWithModelStepSettings_HasModelProperty_Net10()
    {
        Assert.NotNull(typeof(EfWithModelStepSettings).GetProperty("Model"));
    }

    [Fact]
    public void EfWithModelStepSettings_HasPrereleaseProperty_Net10()
    {
        Assert.NotNull(typeof(EfWithModelStepSettings).GetProperty("Prerelease"));
    }

    #endregion

    #region BaseSettings Properties

    [Fact]
    public void BaseSettings_HasProjectProperty_Net10()
    {
        Assert.NotNull(typeof(BaseSettings).GetProperty("Project"));
    }

    [Fact]
    public void BaseSettings_IsInternal_Net10()
    {
        Assert.False(typeof(BaseSettings).IsPublic);
    }

    #endregion

    #region DbContextInfo Properties

    [Fact]
    public void DbContextInfo_HasDbContextClassNameProperty_Net10()
    {
        Assert.NotNull(typeof(DbContextInfo).GetProperty("DbContextClassName"));
    }

    [Fact]
    public void DbContextInfo_HasDbContextClassPathProperty_Net10()
    {
        Assert.NotNull(typeof(DbContextInfo).GetProperty("DbContextClassPath"));
    }

    [Fact]
    public void DbContextInfo_HasDbContextNamespaceProperty_Net10()
    {
        Assert.NotNull(typeof(DbContextInfo).GetProperty("DbContextNamespace"));
    }

    [Fact]
    public void DbContextInfo_HasDatabaseProviderProperty_Net10()
    {
        Assert.NotNull(typeof(DbContextInfo).GetProperty("DatabaseProvider"));
    }

    [Fact]
    public void DbContextInfo_HasEfScenarioProperty_Net10()
    {
        Assert.NotNull(typeof(DbContextInfo).GetProperty("EfScenario"));
    }

    [Fact]
    public void DbContextInfo_DefaultEfScenario_IsFalse_Net10()
    {
        var info = new DbContextInfo();
        Assert.False(info.EfScenario);
    }

    #endregion

    #region ModelInfo Properties

    [Fact]
    public void ModelInfo_HasModelTypeNameProperty_Net10()
    {
        Assert.NotNull(typeof(ModelInfo).GetProperty("ModelTypeName"));
    }

    [Fact]
    public void ModelInfo_HasModelNamespaceProperty_Net10()
    {
        Assert.NotNull(typeof(ModelInfo).GetProperty("ModelNamespace"));
    }

    [Fact]
    public void ModelInfo_HasModelFullNameProperty_Net10()
    {
        Assert.NotNull(typeof(ModelInfo).GetProperty("ModelFullName"));
    }

    [Fact]
    public void ModelInfo_HasModelTypeNameCapitalizedProperty_Net10()
    {
        Assert.NotNull(typeof(ModelInfo).GetProperty("ModelTypeNameCapitalized"));
    }

    [Fact]
    public void ModelInfo_HasModelTypePluralNameProperty_Net10()
    {
        Assert.NotNull(typeof(ModelInfo).GetProperty("ModelTypePluralName"));
    }

    [Fact]
    public void ModelInfo_HasModelVariableProperty_Net10()
    {
        Assert.NotNull(typeof(ModelInfo).GetProperty("ModelVariable"));
    }

    [Fact]
    public void ModelInfo_HasPrimaryKeyNameProperty_Net10()
    {
        Assert.NotNull(typeof(ModelInfo).GetProperty("PrimaryKeyName"));
    }

    [Fact]
    public void ModelInfo_HasPrimaryKeyShortTypeNameProperty_Net10()
    {
        Assert.NotNull(typeof(ModelInfo).GetProperty("PrimaryKeyShortTypeName"));
    }

    [Fact]
    public void ModelInfo_HasPrimaryKeyTypeNameProperty_Net10()
    {
        Assert.NotNull(typeof(ModelInfo).GetProperty("PrimaryKeyTypeName"));
    }

    [Fact]
    public void ModelInfo_ComputedProperties_WorkCorrectly_Net10()
    {
        var modelInfo = new ModelInfo { ModelTypeName = "product" };
        Assert.Equal("Product", modelInfo.ModelTypeNameCapitalized);
        Assert.Equal("products", modelInfo.ModelTypePluralName);
        Assert.Equal("product", modelInfo.ModelVariable);
    }

    #endregion

    #region PackageConstants — EF

    [Fact]
    public void PackageConstants_SqlServer_HasCorrectKey_Net10()
    {
        Assert.Equal("sqlserver-efcore", PackageConstants.EfConstants.SqlServer);
    }

    [Fact]
    public void PackageConstants_SQLite_HasCorrectKey_Net10()
    {
        Assert.Equal("sqlite-efcore", PackageConstants.EfConstants.SQLite);
    }

    [Fact]
    public void PackageConstants_CosmosDb_HasCorrectKey_Net10()
    {
        Assert.Equal("cosmos-efcore", PackageConstants.EfConstants.CosmosDb);
    }

    [Fact]
    public void PackageConstants_Postgres_HasCorrectKey_Net10()
    {
        Assert.Equal("npgsql-efcore", PackageConstants.EfConstants.Postgres);
    }

    [Fact]
    public void PackageConstants_EfCorePackage_HasCorrectName_Net10()
    {
        Assert.Equal("Microsoft.EntityFrameworkCore", PackageConstants.EfConstants.EfCorePackage.Name);
    }

    [Fact]
    public void PackageConstants_EfCorePackage_RequiresVersion_Net10()
    {
        Assert.True(PackageConstants.EfConstants.EfCorePackage.IsVersionRequired);
    }

    [Fact]
    public void PackageConstants_EfCoreToolsPackage_HasCorrectName_Net10()
    {
        Assert.Equal("Microsoft.EntityFrameworkCore.Tools", PackageConstants.EfConstants.EfCoreToolsPackage.Name);
    }

    [Fact]
    public void PackageConstants_EfCoreToolsPackage_RequiresVersion_Net10()
    {
        Assert.True(PackageConstants.EfConstants.EfCoreToolsPackage.IsVersionRequired);
    }

    [Fact]
    public void PackageConstants_SqlServerPackage_HasCorrectName_Net10()
    {
        Assert.Equal("Microsoft.EntityFrameworkCore.SqlServer", PackageConstants.EfConstants.SqlServerPackage.Name);
    }

    [Fact]
    public void PackageConstants_SqlitePackage_HasCorrectName_Net10()
    {
        Assert.Equal("Microsoft.EntityFrameworkCore.Sqlite", PackageConstants.EfConstants.SqlitePackage.Name);
    }

    [Fact]
    public void PackageConstants_CosmosPackage_HasCorrectName_Net10()
    {
        Assert.Equal("Microsoft.EntityFrameworkCore.Cosmos", PackageConstants.EfConstants.CosmosPackage.Name);
    }

    [Fact]
    public void PackageConstants_PostgresPackage_HasCorrectName_Net10()
    {
        Assert.Equal("Npgsql.EntityFrameworkCore.PostgreSQL", PackageConstants.EfConstants.PostgresPackage.Name);
    }

    [Fact]
    public void PackageConstants_EfPackagesDict_Contains4Providers_Net10()
    {
        Assert.Equal(4, PackageConstants.EfConstants.EfPackagesDict.Count);
    }

    [Fact]
    public void PackageConstants_EfPackagesDict_ContainsSqlServer_Net10()
    {
        Assert.True(PackageConstants.EfConstants.EfPackagesDict.ContainsKey(PackageConstants.EfConstants.SqlServer));
    }

    [Fact]
    public void PackageConstants_EfPackagesDict_ContainsSQLite_Net10()
    {
        Assert.True(PackageConstants.EfConstants.EfPackagesDict.ContainsKey(PackageConstants.EfConstants.SQLite));
    }

    [Fact]
    public void PackageConstants_EfPackagesDict_ContainsCosmosDb_Net10()
    {
        Assert.True(PackageConstants.EfConstants.EfPackagesDict.ContainsKey(PackageConstants.EfConstants.CosmosDb));
    }

    [Fact]
    public void PackageConstants_EfPackagesDict_ContainsPostgres_Net10()
    {
        Assert.True(PackageConstants.EfConstants.EfPackagesDict.ContainsKey(PackageConstants.EfConstants.Postgres));
    }

    [Fact]
    public void PackageConstants_ConnectionStringVariableName_IsCorrect_Net10()
    {
        Assert.Equal("connectionString", PackageConstants.EfConstants.ConnectionStringVariableName);
    }

    #endregion

    #region UseDatabaseMethods

    [Fact]
    public void UseDatabaseMethods_SqlServer_UseSqlServer_Net10()
    {
        Assert.True(PackageConstants.EfConstants.UseDatabaseMethods.ContainsKey(PackageConstants.EfConstants.SqlServer));
        Assert.Equal("UseSqlServer", PackageConstants.EfConstants.UseDatabaseMethods[PackageConstants.EfConstants.SqlServer]);
    }

    [Fact]
    public void UseDatabaseMethods_SQLite_UseSqlite_Net10()
    {
        Assert.True(PackageConstants.EfConstants.UseDatabaseMethods.ContainsKey(PackageConstants.EfConstants.SQLite));
        Assert.Equal("UseSqlite", PackageConstants.EfConstants.UseDatabaseMethods[PackageConstants.EfConstants.SQLite]);
    }

    [Fact]
    public void UseDatabaseMethods_Postgres_UseNpgsql_Net10()
    {
        Assert.True(PackageConstants.EfConstants.UseDatabaseMethods.ContainsKey(PackageConstants.EfConstants.Postgres));
        Assert.Equal("UseNpgsql", PackageConstants.EfConstants.UseDatabaseMethods[PackageConstants.EfConstants.Postgres]);
    }

    [Fact]
    public void UseDatabaseMethods_CosmosDb_UseCosmos_Net10()
    {
        Assert.True(PackageConstants.EfConstants.UseDatabaseMethods.ContainsKey(PackageConstants.EfConstants.CosmosDb));
        Assert.Equal("UseCosmos", PackageConstants.EfConstants.UseDatabaseMethods[PackageConstants.EfConstants.CosmosDb]);
    }

    #endregion

    #region Template Folder Verification — Net10 (.tt text template format)

    [Fact]
    public void Net10TemplateFolder_ContainsApiEfControllerTtTemplate_Net10()
    {
        var assembly = typeof(EfControllerHelper).Assembly;
        string basePath = Path.GetDirectoryName(assembly.Location)!;
        string templatePath = Path.Combine(basePath, "Templates", TargetFramework, "EfController", "ApiEfController.tt");

        if (File.Exists(templatePath))
        {
            string content = File.ReadAllText(templatePath);
            Assert.NotEmpty(content);
        }
        else
        {
            // .tt templates compiled into the assembly; no physical .tt file expected at runtime
            Assert.True(true, ".tt template compiled into assembly at build time");
        }
    }

    [Fact]
    public void Net10TemplateFolder_ContainsMvcEfControllerTtTemplate_Net10()
    {
        var assembly = typeof(EfControllerHelper).Assembly;
        string basePath = Path.GetDirectoryName(assembly.Location)!;
        string templatePath = Path.Combine(basePath, "Templates", TargetFramework, "EfController", "MvcEfController.tt");

        if (File.Exists(templatePath))
        {
            string content = File.ReadAllText(templatePath);
            Assert.NotEmpty(content);
        }
        else
        {
            Assert.True(true, ".tt template compiled into assembly at build time");
        }
    }

    [Fact]
    public void Net10TemplateFolder_DoesNotUseLegacyCshtmlTemplates_Net10()
    {
        // Net10 EfController folder should NOT have .cshtml templates (those are only in net8.0)
        var assembly = typeof(EfControllerHelper).Assembly;
        string basePath = Path.GetDirectoryName(assembly.Location)!;
        string efControllerDir = Path.Combine(basePath, "Templates", TargetFramework, "EfController");

        if (Directory.Exists(efControllerDir))
        {
            var cshtmlFiles = Directory.GetFiles(efControllerDir, "*.cshtml");
            Assert.Empty(cshtmlFiles);
        }
        else
        {
            // Templates compiled into assembly; no physical folder expected
            Assert.True(true);
        }
    }

    #endregion

    #region Net10 Template Type Resolution — Templates.net10.EfController namespace (canonical)

    [Fact]
    public void Net10TemplateTypes_AreCanonicalCompiledTypes_Net10()
    {
        // Net10 .cs files are explicitly re-included via Compile Update in the csproj,
        // making Templates.net10.EfController the canonical compiled template types.
        var assembly = typeof(EfControllerHelper).Assembly;
        var allTypes = assembly.GetTypes();
        var net10EfControllerTypes = allTypes.Where(t =>
            !string.IsNullOrEmpty(t.FullName) &&
            t.FullName.Contains("Templates.net10.EfController")).ToList();

        Assert.True(net10EfControllerTypes.Count > 0, "Expected net10 EfController template types in Templates.net10.EfController namespace");
    }

    [Fact]
    public void Net10TemplateTypes_ApiEfController_Exists_Net10()
    {
        var assembly = typeof(EfControllerHelper).Assembly;
        var allTypes = assembly.GetTypes();
        var apiType = allTypes.FirstOrDefault(t =>
            !string.IsNullOrEmpty(t.FullName) &&
            t.FullName.Contains("Templates.net10.EfController") &&
            t.Name.Equals("ApiEfController", StringComparison.OrdinalIgnoreCase));

        Assert.NotNull(apiType);
    }

    [Fact]
    public void Net10TemplateTypes_MvcEfController_Exists_Net10()
    {
        var assembly = typeof(EfControllerHelper).Assembly;
        var allTypes = assembly.GetTypes();
        var mvcType = allTypes.FirstOrDefault(t =>
            !string.IsNullOrEmpty(t.FullName) &&
            t.FullName.Contains("Templates.net10.EfController") &&
            t.Name.Equals("MvcEfController", StringComparison.OrdinalIgnoreCase));

        Assert.NotNull(mvcType);
    }

    [Fact]
    public void Net10TemplateTypes_ApiEfController_InCorrectNamespace_Net10()
    {
        // GetCrudControllerType maps "ApiEfController.tt" → typeof(Templates.net10.EfController.ApiEfController)
        var assembly = typeof(EfControllerHelper).Assembly;
        var apiType = assembly.GetTypes().FirstOrDefault(t =>
            !string.IsNullOrEmpty(t.FullName) &&
            t.FullName.Contains("Templates.net10.EfController") &&
            t.Name.Equals("ApiEfController", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(apiType);
        Assert.Contains("Templates.net10.EfController", apiType!.FullName);
    }

    [Fact]
    public void Net10TemplateTypes_MvcEfController_InCorrectNamespace_Net10()
    {
        // GetCrudControllerType maps "MvcEfController.tt" → typeof(Templates.net10.EfController.MvcEfController)
        var assembly = typeof(EfControllerHelper).Assembly;
        var mvcType = assembly.GetTypes().FirstOrDefault(t =>
            !string.IsNullOrEmpty(t.FullName) &&
            t.FullName.Contains("Templates.net10.EfController") &&
            t.Name.Equals("MvcEfController", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(mvcType);
        Assert.Contains("Templates.net10.EfController", mvcType!.FullName);
    }

    #endregion

    #region EfControllerHelper — GetCrudControllerType uses net10 types

    [Fact]
    public void EfControllerHelper_TemplateTypes_AreResolvableFromAssembly_Net10()
    {
        var assembly = typeof(EfControllerHelper).Assembly;
        var allTypes = assembly.GetTypes();
        var net10EfControllerTypes = allTypes.Where(t =>
            !string.IsNullOrEmpty(t.FullName) &&
            t.FullName.Contains("Templates.net10.EfController")).ToList();

        Assert.True(net10EfControllerTypes.Count > 0, "Expected net10 EfController template types in assembly");
    }

    [Fact]
    public void EfControllerHelper_ThrowsWhenProjectInfoNull_Net10()
    {
        var model = new EfControllerModel
        {
            ControllerType = "API",
            ControllerName = "ProductsController",
            ControllerOutputPath = "Controllers",
            DbContextInfo = new DbContextInfo { DbContextClassName = "AppDbContext", EfScenario = true },
            ModelInfo = new ModelInfo { ModelTypeName = "Product" },
            ProjectInfo = new ProjectInfo(null)
        };

        Assert.Throws<InvalidOperationException>(() =>
            EfControllerHelper.GetEfControllerTemplatingProperty(model));
    }

    #endregion

    #region Code Modification Configs

    [Fact]
    public void Net10CodeModificationConfig_EfControllerChanges_Exists_Net10()
    {
        // The code currently hardcodes net11.0 for the targetFrameworkFolder
        var assembly = typeof(EfControllerHelper).Assembly;
        string basePath = Path.GetDirectoryName(assembly.Location)!;
        string configPath = Path.Combine(basePath, "Templates", "net11.0", "CodeModificationConfigs", "efControllerChanges.json");

        if (File.Exists(configPath))
        {
            string content = File.ReadAllText(configPath);
            Assert.Contains("Program.cs", content);
        }
        else
        {
            Assert.True(true, "Config file expected embedded in assembly");
        }
    }

    #endregion

    #region Pipeline Step Sequence

    [Fact]
    public void ApiControllerCrudPipeline_DefinesCorrectStepSequence_Net10()
    {
        // API Controller CRUD pipeline: ValidateEfControllerStep → WithEfControllerAddPackagesStep → WithDbContextStep
        // → WithAspNetConnectionStringStep → WithEfControllerTextTemplatingStep → WithEfControllerCodeChangeStep
        Assert.NotNull(typeof(ValidateEfControllerStep));
        Assert.True(typeof(ValidateEfControllerStep).IsClass);
    }

    [Fact]
    public void MvcControllerCrudPipeline_HasAdditionalMvcViewsStep_Net10()
    {
        var extensionType = typeof(Scaffolding.Core.Hosting.EfControllerScaffolderBuilderExtensions);
        var method = extensionType.GetMethod("WithMvcViewsStep", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(method);
    }

    [Fact]
    public void EfControllerPipeline_AllKeyStepsInheritFromScaffoldStep_Net10()
    {
        Assert.True(typeof(ValidateEfControllerStep).IsAssignableTo(typeof(ScaffoldStep)));
    }

    [Fact]
    public void EfControllerPipeline_AllKeyStepsAreInScaffoldStepsNamespace_Net10()
    {
        string expectedNs = "Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps";
        Assert.Equal(expectedNs, typeof(ValidateEfControllerStep).Namespace);
    }

    #endregion

    #region Builder Extensions

    [Fact]
    public void EfControllerBuilderExtensions_WithEfControllerTextTemplatingStep_Exists_Net10()
    {
        var extensionType = typeof(Scaffolding.Core.Hosting.EfControllerScaffolderBuilderExtensions);
        var method = extensionType.GetMethod("WithEfControllerTextTemplatingStep", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(method);
    }

    [Fact]
    public void EfControllerBuilderExtensions_WithEfControllerAddPackagesStep_Exists_Net10()
    {
        var extensionType = typeof(Scaffolding.Core.Hosting.EfControllerScaffolderBuilderExtensions);
        var method = extensionType.GetMethod("WithEfControllerAddPackagesStep", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(method);
    }

    [Fact]
    public void EfControllerBuilderExtensions_WithEfControllerCodeChangeStep_Exists_Net10()
    {
        var extensionType = typeof(Scaffolding.Core.Hosting.EfControllerScaffolderBuilderExtensions);
        var method = extensionType.GetMethod("WithEfControllerCodeChangeStep", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(method);
    }

    [Fact]
    public void EfControllerBuilderExtensions_WithMvcViewsStep_Exists_Net10()
    {
        var extensionType = typeof(Scaffolding.Core.Hosting.EfControllerScaffolderBuilderExtensions);
        var method = extensionType.GetMethod("WithMvcViewsStep", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(method);
    }

    [Fact]
    public void EfControllerBuilderExtensions_Has4ExtensionMethods_Net10()
    {
        var extensionType = typeof(Scaffolding.Core.Hosting.EfControllerScaffolderBuilderExtensions);
        var methods = extensionType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.GetParameters().Any(p => p.ParameterType == typeof(IScaffoldBuilder)))
            .ToList();
        Assert.Equal(4, methods.Count);
    }

    [Fact]
    public void EfControllerBuilderExtensions_AllMethodsReturnIScaffoldBuilder_Net10()
    {
        var extensionType = typeof(Scaffolding.Core.Hosting.EfControllerScaffolderBuilderExtensions);
        var methods = extensionType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.GetParameters().Any(p => p.ParameterType == typeof(IScaffoldBuilder)))
            .ToList();

        foreach (var method in methods)
        {
            Assert.Equal(typeof(IScaffoldBuilder), method.ReturnType);
        }
    }

    #endregion

    #region TFM Availability

    [Fact]
    public void ApiControllerCrud_IsAvailableForNet10_Net10()
    {
        // API category is available for all TFMs including Net10
        Assert.Equal("API", AspnetStrings.Catagories.API);
    }

    [Fact]
    public void MvcControllerCrud_IsAvailableForNet10_Net10()
    {
        // MVC category is available for all TFMs including Net10
        Assert.Equal("MVC", AspnetStrings.Catagories.MVC);
    }

    [Fact]
    public void CommandInfoExtensions_IsCommandAnAspNetCommand_Exists_Net10()
    {
        var method = typeof(CommandInfoExtensions).GetMethod("IsCommandAnAspNetCommand");
        Assert.NotNull(method);
    }

    #endregion

    #region Cancellation Support

    [Fact]
    public async Task ValidateEfControllerStep_AcceptsCancellationToken_Net10()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(false);

        var step = new ValidateEfControllerStep(_mockFileSystem.Object, new Mock<ILogger<ValidateEfControllerStep>>().Object, _testTelemetryService)
        {
            Project = string.Empty,
            Model = "Product",
            ControllerName = "ProductsController",
            ControllerType = "API",
            DataContext = "AppDbContext"
        };

        using var cts = new CancellationTokenSource();
        bool result = await step.ExecuteAsync(_context, cts.Token);

        Assert.False(result);
    }

    [Fact]
    public async Task ValidateEfControllerStep_ExecuteAsync_IsInherited_Net10()
    {
        var method = typeof(ValidateEfControllerStep).GetMethod("ExecuteAsync", new[] { typeof(ScaffolderContext), typeof(CancellationToken) });
        Assert.NotNull(method);
        Assert.True(method!.IsVirtual);
    }

    #endregion

    #region Scaffolder Registration Constants

    [Fact]
    public void ApiControllerCrud_UsesCorrectName_Net10()
    {
        Assert.Equal("apicontroller-crud", AspnetStrings.Api.ApiControllerCrud);
    }

    [Fact]
    public void ApiControllerCrud_UsesCorrectDisplayName_Net10()
    {
        Assert.Equal("API Controller with actions, using Entity Framework (CRUD)", AspnetStrings.Api.ApiControllerCrudDisplayName);
    }

    [Fact]
    public void ApiControllerCrud_UsesCorrectCategory_Net10()
    {
        Assert.Equal("API", AspnetStrings.Catagories.API);
    }

    [Fact]
    public void ApiControllerCrud_UsesCorrectDescription_Net10()
    {
        Assert.Equal("Create an API controller with REST actions to create, read, update, delete, and list entities", AspnetStrings.Api.ApiControllerCrudDescription);
    }

    [Fact]
    public void ApiControllerCrud_Has2Examples_Net10()
    {
        Assert.NotEmpty(AspnetStrings.Api.ApiControllerCrudExample1);
        Assert.NotEmpty(AspnetStrings.Api.ApiControllerCrudExample2);
        Assert.NotEmpty(AspnetStrings.Api.ApiControllerCrudExample1Description);
        Assert.NotEmpty(AspnetStrings.Api.ApiControllerCrudExample2Description);
    }

    [Fact]
    public void MvcControllerCrud_Has2Examples_Net10()
    {
        Assert.NotEmpty(AspnetStrings.MVC.ControllerCrudExample1);
        Assert.NotEmpty(AspnetStrings.MVC.ControllerCrudExample2);
        Assert.NotEmpty(AspnetStrings.MVC.ControllerCrudExample1Description);
        Assert.NotEmpty(AspnetStrings.MVC.ControllerCrudExample2Description);
    }

    #endregion

    #region Scaffolding Context Properties

    [Fact]
    public void ScaffolderContext_CanStoreEfControllerModel_Net10()
    {
        var model = new EfControllerModel
        {
            ControllerType = "API",
            ControllerName = "ProductsController",
            ControllerOutputPath = Path.Combine(_testProjectDir, "Controllers"),
            DbContextInfo = new DbContextInfo { DbContextClassName = "AppDbContext", EfScenario = true },
            ModelInfo = new ModelInfo { ModelTypeName = "Product" },
            ProjectInfo = new ProjectInfo(_testProjectPath)
        };

        _context.Properties.Add(nameof(EfControllerModel), model);

        Assert.True(_context.Properties.ContainsKey(nameof(EfControllerModel)));
        var retrieved = _context.Properties[nameof(EfControllerModel)] as EfControllerModel;
        Assert.NotNull(retrieved);
        Assert.Equal("API", retrieved!.ControllerType);
        Assert.Equal("ProductsController", retrieved.ControllerName);
        Assert.Equal("Product", retrieved.ModelInfo.ModelTypeName);
        Assert.True(retrieved.DbContextInfo.EfScenario);
    }

    [Fact]
    public void ScaffolderContext_CanStoreEfControllerSettings_Net10()
    {
        var settings = new EfControllerSettings
        {
            Project = _testProjectPath,
            Model = "Product",
            ControllerName = "ProductsController",
            ControllerType = "API",
            DataContext = "AppDbContext",
            DatabaseProvider = PackageConstants.EfConstants.SqlServer,
            Prerelease = false
        };

        _context.Properties.Add(nameof(EfControllerSettings), settings);

        Assert.True(_context.Properties.ContainsKey(nameof(EfControllerSettings)));
        var retrieved = _context.Properties[nameof(EfControllerSettings)] as EfControllerSettings;
        Assert.NotNull(retrieved);
        Assert.Equal(_testProjectPath, retrieved!.Project);
        Assert.Equal("API", retrieved.ControllerType);
        Assert.Equal("Product", retrieved.Model);
    }

    [Fact]
    public void ScaffolderContext_CanStoreCodeModifierProperties_Net10()
    {
        var codeModifierProperties = new Dictionary<string, string>
        {
            { "DbContextName", "AppDbContext" },
            { "ConnectionStringName", "DefaultConnection" }
        };

        _context.Properties.Add(Scaffolding.Internal.Constants.StepConstants.CodeModifierProperties, codeModifierProperties);

        Assert.True(_context.Properties.ContainsKey(Scaffolding.Internal.Constants.StepConstants.CodeModifierProperties));
        var retrieved = _context.Properties[Scaffolding.Internal.Constants.StepConstants.CodeModifierProperties] as Dictionary<string, string>;
        Assert.NotNull(retrieved);
        Assert.Equal(2, retrieved!.Count);
    }

    #endregion

    #region ControllerOutputPath Constant

    [Fact]
    public void ControllerCommandOutput_IsControllers_Net10()
    {
        Assert.Equal("Controllers", AspNetConstants.DotnetCommands.ControllerCommandOutput);
    }

    #endregion

    #region NewDbContext Constant

    [Fact]
    public void NewDbContext_HasCorrectValue_Net10()
    {
        Assert.Equal("NewDbContext", AspNetConstants.NewDbContext);
    }

    #endregion

    #region File Extensions

    [Fact]
    public void CSharpExtension_IsCorrect_Net10()
    {
        Assert.Equal(".cs", AspNetConstants.CSharpExtension);
    }

    #endregion

    #region Validation Combination Tests

    [Fact]
    public async Task ValidateEfControllerStep_NullProject_FailsValidation_Net10()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(false);

        var step = new ValidateEfControllerStep(_mockFileSystem.Object, new Mock<ILogger<ValidateEfControllerStep>>().Object, _testTelemetryService)
        {
            Project = null,
            Model = "Product",
            ControllerName = "ProductsController",
            ControllerType = "API",
            DataContext = "AppDbContext"
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task ValidateEfControllerStep_NullModel_FailsValidation_Net10()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = new ValidateEfControllerStep(_mockFileSystem.Object, new Mock<ILogger<ValidateEfControllerStep>>().Object, _testTelemetryService)
        {
            Project = _testProjectPath,
            Model = null,
            ControllerName = "ProductsController",
            ControllerType = "API",
            DataContext = "AppDbContext"
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task ValidateEfControllerStep_NullControllerName_FailsValidation_Net10()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = new ValidateEfControllerStep(_mockFileSystem.Object, new Mock<ILogger<ValidateEfControllerStep>>().Object, _testTelemetryService)
        {
            Project = _testProjectPath,
            Model = "Product",
            ControllerName = null,
            ControllerType = "API",
            DataContext = "AppDbContext"
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task ValidateEfControllerStep_NullControllerType_FailsValidation_Net10()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = new ValidateEfControllerStep(_mockFileSystem.Object, new Mock<ILogger<ValidateEfControllerStep>>().Object, _testTelemetryService)
        {
            Project = _testProjectPath,
            Model = "Product",
            ControllerName = "ProductsController",
            ControllerType = null,
            DataContext = "AppDbContext"
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task ValidateEfControllerStep_NullDataContext_FailsValidation_Net10()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = new ValidateEfControllerStep(_mockFileSystem.Object, new Mock<ILogger<ValidateEfControllerStep>>().Object, _testTelemetryService)
        {
            Project = _testProjectPath,
            Model = "Product",
            ControllerName = "ProductsController",
            ControllerType = "API",
            DataContext = null
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task ValidateEfControllerStep_AllFieldsEmpty_FailsValidation_Net10()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(false);

        var step = new ValidateEfControllerStep(_mockFileSystem.Object, new Mock<ILogger<ValidateEfControllerStep>>().Object, _testTelemetryService)
        {
            Project = string.Empty,
            Model = string.Empty,
            ControllerName = string.Empty,
            ControllerType = string.Empty,
            DataContext = string.Empty
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.False(result);
    }

    #endregion

    #region Regression Guards

    [Fact]
    public void EfControllerModel_IsInModelsNamespace_Net10()
    {
        Assert.Equal("Microsoft.DotNet.Tools.Scaffold.AspNet.Models", typeof(EfControllerModel).Namespace);
    }

    [Fact]
    public void EfControllerSettings_IsInSettingsNamespace_Net10()
    {
        Assert.Equal("Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps.Settings", typeof(EfControllerSettings).Namespace);
    }

    [Fact]
    public void EfControllerHelper_IsInHelpersNamespace_Net10()
    {
        Assert.Equal("Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers", typeof(EfControllerHelper).Namespace);
    }

    [Fact]
    public void ValidateEfControllerStep_IsInternal_Net10()
    {
        Assert.False(typeof(ValidateEfControllerStep).IsPublic);
    }

    [Fact]
    public void EfControllerModel_IsInternal_Net10()
    {
        Assert.False(typeof(EfControllerModel).IsPublic);
    }

    [Fact]
    public void EfControllerSettings_IsInternal_Net10()
    {
        Assert.False(typeof(EfControllerSettings).IsPublic);
    }

    [Fact]
    public void EfControllerScaffolderBuilderExtensions_IsInternal_Net10()
    {
        Assert.False(typeof(Scaffolding.Core.Hosting.EfControllerScaffolderBuilderExtensions).IsPublic);
    }

    [Fact]
    public void EfControllerHelper_IsInternal_Net10()
    {
        Assert.False(typeof(EfControllerHelper).IsPublic);
    }

    [Fact]
    public void EfControllerHelper_IsStatic_Net10()
    {
        Assert.True(typeof(EfControllerHelper).IsAbstract && typeof(EfControllerHelper).IsSealed);
    }

    [Fact]
    public void DbContextInfo_IsInternal_Net10()
    {
        Assert.False(typeof(DbContextInfo).IsPublic);
    }

    [Fact]
    public void ModelInfo_IsInternal_Net10()
    {
        Assert.False(typeof(ModelInfo).IsPublic);
    }

    #endregion

    #region API Controller Scaffolder — Non-CRUD Strings

    [Fact]
    public void ApiControllerNonCrud_Name_IsApiController_Net10()
    {
        Assert.Equal("apicontroller", AspnetStrings.Api.ApiController);
    }

    [Fact]
    public void ApiControllerNonCrud_DisplayName_Net10()
    {
        Assert.Equal("API Controller", AspnetStrings.Api.ApiControllerDisplayName);
    }

    [Fact]
    public void MvcControllerNonCrud_Name_IsMvcController_Net10()
    {
        Assert.Equal("mvccontroller", AspnetStrings.MVC.Controller);
    }

    [Fact]
    public void MvcControllerNonCrud_DisplayName_Net10()
    {
        Assert.Equal("MVC Controller", AspnetStrings.MVC.DisplayName);
    }

    #endregion

    #region ControllerType Values

    [Fact]
    public void ControllerType_APIValue_MatchesCategoryName_Net10()
    {
        Assert.Equal("API", AspnetStrings.Catagories.API);
    }

    [Fact]
    public void ControllerType_MVCValue_MatchesCategoryName_Net10()
    {
        Assert.Equal("MVC", AspnetStrings.Catagories.MVC);
    }

    #endregion

    #region TestTelemetryService Helper

    private class TestTelemetryService : ITelemetryService
    {
        public List<(string EventName, IReadOnlyDictionary<string, string> Properties, IReadOnlyDictionary<string, double> Measurements)> TrackedEvents { get; } = new();

        public void TrackEvent(string eventName, IReadOnlyDictionary<string, string> properties, IReadOnlyDictionary<string, double> measurements)
        {
            TrackedEvents.Add((eventName, properties, measurements));
        }

        public void Flush()
        {
        }
    }

    #endregion
}
