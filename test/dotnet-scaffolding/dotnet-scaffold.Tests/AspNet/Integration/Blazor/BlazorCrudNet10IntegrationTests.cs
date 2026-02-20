// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
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

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Integration.Blazor;

/// <summary>
/// Integration tests for the Blazor CRUD (blazor-crud) scaffolder targeting .NET 10.
/// Validates ValidateBlazorCrudStep validation logic, BlazorCrudModel input type mapping,
/// BlazorCrudHelper template/output path resolution, scaffolder definition constants,
/// code change generation, template property resolution, and pipeline registration.
/// .NET 10 adds the NotFound template to the BlazorCrud template folder alongside
/// Create, Delete, Details, Edit, and Index.
/// </summary>
public class BlazorCrudNet10IntegrationTests : IDisposable
{
    private const string TargetFramework = "net10.0";
    private readonly string _testDirectory;
    private readonly string _testProjectDir;
    private readonly string _testProjectPath;
    private readonly Mock<IFileSystem> _mockFileSystem;
    private readonly TestTelemetryService _testTelemetryService;
    private readonly Mock<IScaffolder> _mockScaffolder;
    private readonly ScaffolderContext _context;

    public BlazorCrudNet10IntegrationTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "BlazorCrudNet10IntegrationTests", Guid.NewGuid().ToString());
        _testProjectDir = Path.Combine(_testDirectory, "TestProject");
        _testProjectPath = Path.Combine(_testProjectDir, "TestProject.csproj");
        Directory.CreateDirectory(_testProjectDir);

        _mockFileSystem = new Mock<IFileSystem>();
        _testTelemetryService = new TestTelemetryService();
        _mockScaffolder = new Mock<IScaffolder>();
        _mockScaffolder.Setup(s => s.DisplayName).Returns(AspnetStrings.Blazor.CrudDisplayName);
        _mockScaffolder.Setup(s => s.Name).Returns(AspnetStrings.Blazor.Crud);
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
    public void ScaffolderName_IsBlazorCrud_Net10()
    {
        Assert.Equal("blazor-crud", AspnetStrings.Blazor.Crud);
    }

    [Fact]
    public void ScaffolderDisplayName_IsRazorComponentsWithEntityFrameworkCoreCRUD_Net10()
    {
        Assert.Equal("Razor Components with EntityFrameworkCore (CRUD)", AspnetStrings.Blazor.CrudDisplayName);
    }

    [Fact]
    public void ScaffolderDescription_DescribesCrudGeneration_Net10()
    {
        Assert.Contains("Razor Components", AspnetStrings.Blazor.CrudDescription);
        Assert.Contains("Entity Framework", AspnetStrings.Blazor.CrudDescription);
        Assert.Contains("Create", AspnetStrings.Blazor.CrudDescription);
        Assert.Contains("Delete", AspnetStrings.Blazor.CrudDescription);
    }

    [Fact]
    public void ScaffolderDescription_MentionsAllCrudOperations_Net10()
    {
        Assert.Contains("Create", AspnetStrings.Blazor.CrudDescription);
        Assert.Contains("Delete", AspnetStrings.Blazor.CrudDescription);
        Assert.Contains("Details", AspnetStrings.Blazor.CrudDescription);
        Assert.Contains("Edit", AspnetStrings.Blazor.CrudDescription);
        Assert.Contains("List", AspnetStrings.Blazor.CrudDescription);
    }

    [Fact]
    public void ScaffolderExample1_ContainsBlazorCrudCommand_Net10()
    {
        Assert.Contains("blazor-crud", AspnetStrings.Blazor.CrudExample1);
    }

    [Fact]
    public void ScaffolderExample1_ContainsRequiredOptions_Net10()
    {
        Assert.Contains("--project", AspnetStrings.Blazor.CrudExample1);
        Assert.Contains("--model", AspnetStrings.Blazor.CrudExample1);
        Assert.Contains("--data-context", AspnetStrings.Blazor.CrudExample1);
        Assert.Contains("--database-provider", AspnetStrings.Blazor.CrudExample1);
        Assert.Contains("--page", AspnetStrings.Blazor.CrudExample1);
    }

    [Fact]
    public void ScaffolderExample2_ContainsBlazorCrudCommand_Net10()
    {
        Assert.Contains("blazor-crud", AspnetStrings.Blazor.CrudExample2);
    }

    [Fact]
    public void ScaffolderExampleDescriptions_AreNotEmpty_Net10()
    {
        Assert.False(string.IsNullOrEmpty(AspnetStrings.Blazor.CrudExample1Description));
        Assert.False(string.IsNullOrEmpty(AspnetStrings.Blazor.CrudExample2Description));
    }

    [Fact]
    public void BlazorCrud_IsDifferentFromBlazorEmpty_Net10()
    {
        Assert.NotEqual(AspnetStrings.Blazor.Crud, AspnetStrings.Blazor.Empty);
    }

    [Fact]
    public void BlazorCrud_IsDifferentFromBlazorIdentity_Net10()
    {
        Assert.NotEqual(AspnetStrings.Blazor.Crud, AspnetStrings.Blazor.Identity);
    }

    [Fact]
    public void BlazorCrud_BlazorExtensionConstant_IsRazor_Net10()
    {
        Assert.Equal(".razor", AspNetConstants.BlazorExtension);
    }

    #endregion

    #region ValidateBlazorCrudStep — Validation (Null/Empty/Missing Inputs)

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenProjectIsNull_Net10()
    {
        var step = new ValidateBlazorCrudStep(
            _mockFileSystem.Object,
            NullLogger<ValidateBlazorCrudStep>.Instance,
            _testTelemetryService)
        {
            Project = null,
            Model = "Product",
            Page = "CRUD",
            DataContext = "AppDbContext",
            DatabaseProvider = PackageConstants.EfConstants.SqlServer
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenProjectIsEmpty_Net10()
    {
        var step = new ValidateBlazorCrudStep(
            _mockFileSystem.Object,
            NullLogger<ValidateBlazorCrudStep>.Instance,
            _testTelemetryService)
        {
            Project = string.Empty,
            Model = "Product",
            Page = "CRUD",
            DataContext = "AppDbContext",
            DatabaseProvider = PackageConstants.EfConstants.SqlServer
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenProjectDoesNotExist_Net10()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(false);

        var step = new ValidateBlazorCrudStep(
            _mockFileSystem.Object,
            NullLogger<ValidateBlazorCrudStep>.Instance,
            _testTelemetryService)
        {
            Project = Path.Combine(_testProjectDir, "NonExistent.csproj"),
            Model = "Product",
            Page = "CRUD",
            DataContext = "AppDbContext",
            DatabaseProvider = PackageConstants.EfConstants.SqlServer
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenModelIsNull_Net10()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = new ValidateBlazorCrudStep(
            _mockFileSystem.Object,
            NullLogger<ValidateBlazorCrudStep>.Instance,
            _testTelemetryService)
        {
            Project = _testProjectPath,
            Model = null,
            Page = "CRUD",
            DataContext = "AppDbContext",
            DatabaseProvider = PackageConstants.EfConstants.SqlServer
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenModelIsEmpty_Net10()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = new ValidateBlazorCrudStep(
            _mockFileSystem.Object,
            NullLogger<ValidateBlazorCrudStep>.Instance,
            _testTelemetryService)
        {
            Project = _testProjectPath,
            Model = string.Empty,
            Page = "CRUD",
            DataContext = "AppDbContext",
            DatabaseProvider = PackageConstants.EfConstants.SqlServer
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenPageIsNull_Net10()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = new ValidateBlazorCrudStep(
            _mockFileSystem.Object,
            NullLogger<ValidateBlazorCrudStep>.Instance,
            _testTelemetryService)
        {
            Project = _testProjectPath,
            Model = "Product",
            Page = null,
            DataContext = "AppDbContext",
            DatabaseProvider = PackageConstants.EfConstants.SqlServer
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenPageIsEmpty_Net10()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = new ValidateBlazorCrudStep(
            _mockFileSystem.Object,
            NullLogger<ValidateBlazorCrudStep>.Instance,
            _testTelemetryService)
        {
            Project = _testProjectPath,
            Model = "Product",
            Page = string.Empty,
            DataContext = "AppDbContext",
            DatabaseProvider = PackageConstants.EfConstants.SqlServer
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenDataContextIsNull_Net10()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = new ValidateBlazorCrudStep(
            _mockFileSystem.Object,
            NullLogger<ValidateBlazorCrudStep>.Instance,
            _testTelemetryService)
        {
            Project = _testProjectPath,
            Model = "Product",
            Page = "CRUD",
            DataContext = null,
            DatabaseProvider = PackageConstants.EfConstants.SqlServer
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenDataContextIsEmpty_Net10()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);

        var step = new ValidateBlazorCrudStep(
            _mockFileSystem.Object,
            NullLogger<ValidateBlazorCrudStep>.Instance,
            _testTelemetryService)
        {
            Project = _testProjectPath,
            Model = "Product",
            Page = "CRUD",
            DataContext = string.Empty,
            DatabaseProvider = PackageConstants.EfConstants.SqlServer
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.False(result);
    }

    #endregion

    #region ValidateBlazorCrudStep — Telemetry

    [Fact]
    public async Task ExecuteAsync_TracksTelemetryEvent_OnValidationFailure_NullProject_Net10()
    {
        var telemetry = new TestTelemetryService();
        var step = new ValidateBlazorCrudStep(
            _mockFileSystem.Object,
            NullLogger<ValidateBlazorCrudStep>.Instance,
            telemetry)
        {
            Project = null,
            Model = "Product",
            Page = "CRUD",
            DataContext = "AppDbContext",
            DatabaseProvider = PackageConstants.EfConstants.SqlServer
        };

        await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.Single(telemetry.TrackedEvents);
        Assert.Equal("ValidateBlazorCrudStepEvent", telemetry.TrackedEvents[0].EventName);
    }

    [Fact]
    public async Task ExecuteAsync_TracksTelemetryEvent_OnValidationFailure_NullModel_Net10()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(_testProjectPath)).Returns(true);
        var telemetry = new TestTelemetryService();

        var step = new ValidateBlazorCrudStep(
            _mockFileSystem.Object,
            NullLogger<ValidateBlazorCrudStep>.Instance,
            telemetry)
        {
            Project = _testProjectPath,
            Model = null,
            Page = "CRUD",
            DataContext = "AppDbContext",
            DatabaseProvider = PackageConstants.EfConstants.SqlServer
        };

        await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.Single(telemetry.TrackedEvents);
        Assert.Equal("ValidateBlazorCrudStepEvent", telemetry.TrackedEvents[0].EventName);
    }

    [Fact]
    public async Task ExecuteAsync_TracksTelemetryEvent_WithScaffolderDisplayName_Net10()
    {
        var telemetry = new TestTelemetryService();
        var step = new ValidateBlazorCrudStep(
            _mockFileSystem.Object,
            NullLogger<ValidateBlazorCrudStep>.Instance,
            telemetry)
        {
            Project = null,
            Model = "Product",
            Page = "CRUD",
            DataContext = "AppDbContext",
            DatabaseProvider = PackageConstants.EfConstants.SqlServer
        };

        await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.Single(telemetry.TrackedEvents);
        Assert.Equal(AspnetStrings.Blazor.CrudDisplayName, telemetry.TrackedEvents[0].Properties["ScaffolderName"]);
    }

    #endregion

    #region ValidateBlazorCrudStep — Cancellation Token

    [Fact]
    public async Task ExecuteAsync_AcceptsCancellationToken_WithoutThrowing_Net10()
    {
        var step = new ValidateBlazorCrudStep(
            _mockFileSystem.Object,
            NullLogger<ValidateBlazorCrudStep>.Instance,
            _testTelemetryService)
        {
            Project = null,
            Model = "Product",
            Page = "CRUD",
            DataContext = "AppDbContext",
            DatabaseProvider = PackageConstants.EfConstants.SqlServer
        };

        using var cts = new CancellationTokenSource();

        bool result = await step.ExecuteAsync(_context, cts.Token);

        Assert.False(result);
    }

    #endregion

    #region BlazorCrudModel — Input Type Mapping

    [Theory]
    [InlineData("string", "InputText")]
    [InlineData("DateTime", "InputDate")]
    [InlineData("DateTimeOffset", "InputDate")]
    [InlineData("DateOnly", "InputDate")]
    [InlineData("TimeOnly", "InputDate")]
    [InlineData("System.DateTime", "InputDate")]
    [InlineData("System.DateTimeOffset", "InputDate")]
    [InlineData("System.DateOnly", "InputDate")]
    [InlineData("System.TimeOnly", "InputDate")]
    [InlineData("int", "InputNumber")]
    [InlineData("long", "InputNumber")]
    [InlineData("short", "InputNumber")]
    [InlineData("float", "InputNumber")]
    [InlineData("decimal", "InputNumber")]
    [InlineData("double", "InputNumber")]
    [InlineData("bool", "InputCheckbox")]
    [InlineData("enum", "InputSelect")]
    [InlineData("enum[]", "InputSelect")]
    public void GetInputType_ReturnsCorrectBlazorInputComponent_Net10(string dotnetType, string expectedInputType)
    {
        var model = CreateTestBlazorCrudModel();
        string result = model.GetInputType(dotnetType);
        Assert.Equal(expectedInputType, result);
    }

    [Fact]
    public void GetInputType_ReturnsInputText_ForUnknownType_Net10()
    {
        var model = CreateTestBlazorCrudModel();
        string result = model.GetInputType("SomeCustomType");
        Assert.Equal("InputText", result);
    }

    [Fact]
    public void GetInputType_ReturnsInputText_ForNullInput_Net10()
    {
        var model = CreateTestBlazorCrudModel();
        string result = model.GetInputType(null!);
        Assert.Equal("InputText", result);
    }

    [Fact]
    public void GetInputType_ReturnsInputText_ForEmptyInput_Net10()
    {
        var model = CreateTestBlazorCrudModel();
        string result = model.GetInputType(string.Empty);
        Assert.Equal("InputText", result);
    }

    #endregion

    #region BlazorCrudModel — Input Class Type (CSS)

    [Fact]
    public void GetInputClassType_ReturnsFormCheckInput_ForBool_Net10()
    {
        var model = CreateTestBlazorCrudModel();
        string result = model.GetInputClassType("bool");
        Assert.Equal("form-check-input", result);
    }

    [Fact]
    public void GetInputClassType_ReturnsFormControl_ForString_Net10()
    {
        var model = CreateTestBlazorCrudModel();
        string result = model.GetInputClassType("string");
        Assert.Equal("form-control", result);
    }

    [Fact]
    public void GetInputClassType_ReturnsFormControl_ForInt_Net10()
    {
        var model = CreateTestBlazorCrudModel();
        string result = model.GetInputClassType("int");
        Assert.Equal("form-control", result);
    }

    [Fact]
    public void GetInputClassType_ReturnsFormControl_ForDateTime_Net10()
    {
        var model = CreateTestBlazorCrudModel();
        string result = model.GetInputClassType("DateTime");
        Assert.Equal("form-control", result);
    }

    [Theory]
    [InlineData("Bool")]
    [InlineData("BOOL")]
    public void GetInputClassType_IsCaseInsensitive_ForBool_Net10(string boolVariant)
    {
        var model = CreateTestBlazorCrudModel();
        string result = model.GetInputClassType(boolVariant);
        Assert.Equal("form-check-input", result);
    }

    #endregion

    #region BlazorCrudModel — Property Initialization

    [Fact]
    public void BlazorCrudModel_HasMainLayout_DefaultsFalse_Net10()
    {
        var model = CreateTestBlazorCrudModel();
        Assert.False(model.HasMainLayout);
    }

    [Fact]
    public void BlazorCrudModel_HasMainLayout_CanBeSetToTrue_Net10()
    {
        var model = CreateTestBlazorCrudModel();
        model.HasMainLayout = true;
        Assert.True(model.HasMainLayout);
    }

    [Fact]
    public void BlazorCrudModel_PageType_CanBeRead_Net10()
    {
        var model = CreateTestBlazorCrudModel();
        Assert.Equal("CRUD", model.PageType);
    }

    [Fact]
    public void BlazorCrudModel_ModelInfo_IsNotNull_Net10()
    {
        var model = CreateTestBlazorCrudModel();
        Assert.NotNull(model.ModelInfo);
        Assert.Equal("Product", model.ModelInfo.ModelTypeName);
    }

    [Fact]
    public void BlazorCrudModel_DbContextInfo_IsNotNull_Net10()
    {
        var model = CreateTestBlazorCrudModel();
        Assert.NotNull(model.DbContextInfo);
        Assert.Equal("AppDbContext", model.DbContextInfo.DbContextClassName);
    }

    #endregion

    #region BlazorCrudHelper — Template Type Mapping

    [Fact]
    public void GetTemplateType_WithCreateTemplate_ReturnsNonNull_Net10()
    {
        string templatePath = Path.Combine("templates", BlazorCrudHelper.CreateBlazorTemplate);
        Type? result = BlazorCrudHelper.GetTemplateType(templatePath);
        Assert.NotNull(result);
    }

    [Fact]
    public void GetTemplateType_WithDeleteTemplate_ReturnsNonNull_Net10()
    {
        string templatePath = Path.Combine("templates", BlazorCrudHelper.DeleteBlazorTemplate);
        Type? result = BlazorCrudHelper.GetTemplateType(templatePath);
        Assert.NotNull(result);
    }

    [Fact]
    public void GetTemplateType_WithDetailsTemplate_ReturnsNonNull_Net10()
    {
        string templatePath = Path.Combine("templates", BlazorCrudHelper.DetailsBlazorTemplate);
        Type? result = BlazorCrudHelper.GetTemplateType(templatePath);
        Assert.NotNull(result);
    }

    [Fact]
    public void GetTemplateType_WithEditTemplate_ReturnsNonNull_Net10()
    {
        string templatePath = Path.Combine("templates", BlazorCrudHelper.EditBlazorTemplate);
        Type? result = BlazorCrudHelper.GetTemplateType(templatePath);
        Assert.NotNull(result);
    }

    [Fact]
    public void GetTemplateType_WithIndexTemplate_ReturnsNonNull_Net10()
    {
        string templatePath = Path.Combine("templates", BlazorCrudHelper.IndexBlazorTemplate);
        Type? result = BlazorCrudHelper.GetTemplateType(templatePath);
        Assert.NotNull(result);
    }

    [Fact]
    public void GetTemplateType_WithNotFoundTemplate_ReturnsNonNull_Net10()
    {
        string templatePath = Path.Combine("templates", BlazorCrudHelper.NotFoundBlazorTemplate);
        Type? result = BlazorCrudHelper.GetTemplateType(templatePath);
        Assert.NotNull(result);
    }

    [Fact]
    public void GetTemplateType_WithNull_ReturnsNull_Net10()
    {
        Type? result = BlazorCrudHelper.GetTemplateType(null);
        Assert.Null(result);
    }

    [Fact]
    public void GetTemplateType_WithEmpty_ReturnsNull_Net10()
    {
        Type? result = BlazorCrudHelper.GetTemplateType(string.Empty);
        Assert.Null(result);
    }

    [Fact]
    public void GetTemplateType_WithUnknownTemplate_ReturnsNull_Net10()
    {
        Type? result = BlazorCrudHelper.GetTemplateType(Path.Combine("templates", "Unknown.tt"));
        Assert.Null(result);
    }

    #endregion

    #region BlazorCrudHelper — Template Validation

    [Theory]
    [InlineData("CRUD", "Create", true)]
    [InlineData("CRUD", "Delete", true)]
    [InlineData("CRUD", "Details", true)]
    [InlineData("CRUD", "Edit", true)]
    [InlineData("CRUD", "Index", true)]
    [InlineData("CRUD", "NotFound", true)]
    [InlineData("Create", "Create", true)]
    [InlineData("Delete", "Delete", true)]
    [InlineData("Details", "Details", true)]
    [InlineData("Edit", "Edit", true)]
    [InlineData("Index", "Index", true)]
    [InlineData("Create", "Delete", false)]
    [InlineData("Edit", "Index", false)]
    [InlineData("Details", "Create", false)]
    public void IsValidTemplate_ReturnsExpectedResult_Net10(string templateType, string templateFileName, bool expected)
    {
        bool result = BlazorCrudHelper.IsValidTemplate(templateType, templateFileName);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsValidTemplate_CRUDType_AlwaysReturnsTrue_RegardlessOfFileName_Net10()
    {
        Assert.True(BlazorCrudHelper.IsValidTemplate("CRUD", "AnyFileName"));
        Assert.True(BlazorCrudHelper.IsValidTemplate("CRUD", ""));
        Assert.True(BlazorCrudHelper.IsValidTemplate("crud", "SomeName"));
    }

    [Theory]
    [InlineData("create", "CREATE")]
    [InlineData("DELETE", "delete")]
    [InlineData("Edit", "edit")]
    public void IsValidTemplate_IsCaseInsensitive_Net10(string templateType, string templateFileName)
    {
        bool result = BlazorCrudHelper.IsValidTemplate(templateType, templateFileName);
        Assert.True(result);
    }

    #endregion

    #region BlazorCrudHelper — Output Path Resolution

    [Fact]
    public void GetBaseOutputPath_WithValidInputs_ContainsComponentsPagesModelPages_Net10()
    {
        string projectPath = Path.Combine("C:", "Projects", "MyApp", "MyApp.csproj");
        string modelName = "Product";

        string result = BlazorCrudHelper.GetBaseOutputPath(modelName, projectPath);

        Assert.Contains("Components", result);
        Assert.Contains("Pages", result);
        Assert.Contains("ProductPages", result);
    }

    [Fact]
    public void GetBaseOutputPath_WithNullProjectPath_StillReturnsPath_Net10()
    {
        string modelName = "Product";

        string result = BlazorCrudHelper.GetBaseOutputPath(modelName, null);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains("ProductPages", result);
    }

    [Fact]
    public void GetBaseOutputPath_ModelName_AppendedAsModelNamePages_Net10()
    {
        string projectPath = Path.Combine("C:", "TestApp", "TestApp.csproj");
        string modelName = "Customer";

        string result = BlazorCrudHelper.GetBaseOutputPath(modelName, projectPath);

        Assert.EndsWith("CustomerPages", result);
    }

    [Fact]
    public void GetBaseOutputPath_DifferentModels_ProduceDifferentPaths_Net10()
    {
        string projectPath = Path.Combine("C:", "TestApp", "TestApp.csproj");

        string productPath = BlazorCrudHelper.GetBaseOutputPath("Product", projectPath);
        string customerPath = BlazorCrudHelper.GetBaseOutputPath("Customer", projectPath);

        Assert.NotEqual(productPath, customerPath);
    }

    #endregion

    #region BlazorCrudHelper — CRUD Pages List

    [Fact]
    public void CRUDPages_ContainsAllSevenPageTypes_Net10()
    {
        Assert.Equal(7, BlazorCrudHelper.CRUDPages.Count);
    }

    [Fact]
    public void CRUDPages_ContainsCRUD_Net10()
    {
        Assert.Contains("CRUD", BlazorCrudHelper.CRUDPages);
    }

    [Fact]
    public void CRUDPages_ContainsCreate_Net10()
    {
        Assert.Contains("Create", BlazorCrudHelper.CRUDPages);
    }

    [Fact]
    public void CRUDPages_ContainsDelete_Net10()
    {
        Assert.Contains("Delete", BlazorCrudHelper.CRUDPages);
    }

    [Fact]
    public void CRUDPages_ContainsDetails_Net10()
    {
        Assert.Contains("Details", BlazorCrudHelper.CRUDPages);
    }

    [Fact]
    public void CRUDPages_ContainsEdit_Net10()
    {
        Assert.Contains("Edit", BlazorCrudHelper.CRUDPages);
    }

    [Fact]
    public void CRUDPages_ContainsIndex_Net10()
    {
        Assert.Contains("Index", BlazorCrudHelper.CRUDPages);
    }

    [Fact]
    public void CRUDPages_ContainsNotFound_Net10()
    {
        Assert.Contains("NotFound", BlazorCrudHelper.CRUDPages);
    }

    #endregion

    #region BlazorCrudHelper — Template Constants

    [Fact]
    public void CreateBlazorTemplate_HasExpectedValue_Net10()
    {
        Assert.Equal("Create.tt", BlazorCrudHelper.CreateBlazorTemplate);
    }

    [Fact]
    public void DeleteBlazorTemplate_HasExpectedValue_Net10()
    {
        Assert.Equal("Delete.tt", BlazorCrudHelper.DeleteBlazorTemplate);
    }

    [Fact]
    public void DetailsBlazorTemplate_HasExpectedValue_Net10()
    {
        Assert.Equal("Details.tt", BlazorCrudHelper.DetailsBlazorTemplate);
    }

    [Fact]
    public void EditBlazorTemplate_HasExpectedValue_Net10()
    {
        Assert.Equal("Edit.tt", BlazorCrudHelper.EditBlazorTemplate);
    }

    [Fact]
    public void IndexBlazorTemplate_HasExpectedValue_Net10()
    {
        Assert.Equal("Index.tt", BlazorCrudHelper.IndexBlazorTemplate);
    }

    [Fact]
    public void NotFoundBlazorTemplate_HasExpectedValue_Net10()
    {
        Assert.Equal("NotFound.tt", BlazorCrudHelper.NotFoundBlazorTemplate);
    }

    #endregion

    #region BlazorCrudHelper — Net10 Template Folder Verification

    [Fact]
    public void Net10TemplateFolderContainsNotFoundTemplate()
    {
        // Verify the net10.0 BlazorCrud template folder includes the NotFound template
        // that was added in .NET 10 (not present in net8.0/net9.0 template folders).
        string templateFolder = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "AspNet", "Templates", "net10.0", "BlazorCrud");

        if (Directory.Exists(templateFolder))
        {
            string notFoundPath = Path.Combine(templateFolder, "NotFound.tt");
            Assert.True(File.Exists(notFoundPath),
                "net10.0 BlazorCrud template folder should contain NotFound.tt");
        }
    }

    [Fact]
    public void Net10TemplateFolderContainsAllSixTemplates()
    {
        // net10.0 should have 6 .tt templates: Create, Delete, Details, Edit, Index, NotFound
        string templateFolder = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "AspNet", "Templates", "net10.0", "BlazorCrud");

        if (Directory.Exists(templateFolder))
        {
            string[] expectedTemplates = new[]
            {
                "Create.tt", "Delete.tt", "Details.tt", "Edit.tt", "Index.tt", "NotFound.tt"
            };

            foreach (var template in expectedTemplates)
            {
                Assert.True(File.Exists(Path.Combine(templateFolder, template)),
                    $"net10.0 BlazorCrud template folder should contain {template}");
            }
        }
    }

    #endregion

    #region BlazorCrudHelper — Code Change Snippets

    [Fact]
    public void AddRazorComponentsSnippet_ContainsExpectedMethod_Net10()
    {
        Assert.Contains("AddRazorComponents()", BlazorCrudHelper.AddRazorComponentsSnippet);
    }

    [Fact]
    public void AddRazorComponentsSnippet_ContainsInsertBeforeMarker_Net10()
    {
        Assert.Contains("InsertBefore", BlazorCrudHelper.AddRazorComponentsSnippet);
        Assert.Contains("var app = WebApplication.CreateBuilder.Build()", BlazorCrudHelper.AddRazorComponentsSnippet);
    }

    [Fact]
    public void AddMapRazorComponentsSnippet_ContainsExpectedMethod_Net10()
    {
        Assert.Contains("MapRazorComponents<App>()", BlazorCrudHelper.AddMapRazorComponentsSnippet);
    }

    [Fact]
    public void AddMapRazorComponentsSnippet_ContainsInsertBeforeAppRun_Net10()
    {
        Assert.Contains("app.Run()", BlazorCrudHelper.AddMapRazorComponentsSnippet);
    }

    [Fact]
    public void AddInteractiveServerRenderModeSnippet_ContainsExpectedMethod_Net10()
    {
        Assert.Contains("AddInteractiveServerRenderMode()", BlazorCrudHelper.AddInteractiveServerRenderModeSnippet);
    }

    [Fact]
    public void AddInteractiveServerRenderModeSnippet_HasMapRazorComponentsParent_Net10()
    {
        Assert.Contains("MapRazorComponents<App>", BlazorCrudHelper.AddInteractiveServerRenderModeSnippet);
    }

    [Fact]
    public void AddInteractiveServerComponentsSnippet_ContainsExpectedMethod_Net10()
    {
        Assert.Contains("AddInteractiveServerComponents()", BlazorCrudHelper.AddInteractiveServerComponentsSnippet);
    }

    [Fact]
    public void AddInteractiveServerComponentsSnippet_HasAddRazorComponentsParent_Net10()
    {
        Assert.Contains("AddRazorComponents()", BlazorCrudHelper.AddInteractiveServerComponentsSnippet);
    }

    [Fact]
    public void AdditionalCodeModificationJson_ContainsPlaceholder_Net10()
    {
        Assert.Contains("$(CodeChanges)", BlazorCrudHelper.AdditionalCodeModificationJson);
    }

    [Fact]
    public void AdditionalCodeModificationJson_TargetsProgramCs_Net10()
    {
        Assert.Contains("Program.cs", BlazorCrudHelper.AdditionalCodeModificationJson);
    }

    #endregion

    #region BlazorCrudHelper — Method/Type Constants

    [Fact]
    public void AddRazorComponentsMethod_HasExpectedValue_Net10()
    {
        Assert.Equal("AddRazorComponents", BlazorCrudHelper.AddRazorComponentsMethod);
    }

    [Fact]
    public void MapRazorComponentsMethod_HasExpectedValue_Net10()
    {
        Assert.Equal("MapRazorComponents", BlazorCrudHelper.MapRazorComponentsMethod);
    }

    [Fact]
    public void AddInteractiveServerComponentsMethod_HasExpectedValue_Net10()
    {
        Assert.Equal("AddInteractiveServerComponents", BlazorCrudHelper.AddInteractiveServerComponentsMethod);
    }

    [Fact]
    public void AddInteractiveServerRenderModeMethod_HasExpectedValue_Net10()
    {
        Assert.Equal("AddInteractiveServerRenderMode", BlazorCrudHelper.AddInteractiveServerRenderModeMethod);
    }

    [Fact]
    public void AddInteractiveWebAssemblyComponentsMethod_HasExpectedValue_Net10()
    {
        Assert.Equal("AddInteractiveWebAssemblyComponents", BlazorCrudHelper.AddInteractiveWebAssemblyComponentsMethod);
    }

    [Fact]
    public void AddInteractiveWebAssemblyRenderModeMethod_HasExpectedValue_Net10()
    {
        Assert.Equal("AddInteractiveWebAssemblyRenderMode", BlazorCrudHelper.AddInteractiveWebAssemblyRenderModeMethod);
    }

    [Fact]
    public void IRazorComponentsBuilderType_HasExpectedFullyQualifiedName_Net10()
    {
        Assert.Equal("Microsoft.Extensions.DependencyInjection.IRazorComponentsBuilder", BlazorCrudHelper.IRazorComponentsBuilderType);
    }

    [Fact]
    public void IServiceCollectionType_HasExpectedFullyQualifiedName_Net10()
    {
        Assert.Equal("Microsoft.Extensions.DependencyInjection.IServiceCollection", BlazorCrudHelper.IServiceCollectionType);
    }

    [Fact]
    public void RazorComponentsEndpointsConventionBuilderType_HasExpectedFullyQualifiedName_Net10()
    {
        Assert.Equal("Microsoft.AspNetCore.Builder.RazorComponentsEndpointConventionBuilder", BlazorCrudHelper.RazorComponentsEndpointsConventionBuilderType);
    }

    [Fact]
    public void IEndpointRouteBuilderContainingType_HasExpectedFullyQualifiedName_Net10()
    {
        Assert.Equal("Microsoft.AspNetCore.Routing.IEndpointRouteBuilder", BlazorCrudHelper.IEndpointRouteBuilderContainingType);
    }

    [Fact]
    public void IServerSideBlazorBuilderType_HasExpectedFullyQualifiedName_Net10()
    {
        Assert.Equal("Microsoft.Extensions.DependencyInjection.IServerSideBlazorBuilder", BlazorCrudHelper.IServerSideBlazorBuilderType);
    }

    #endregion

    #region BlazorCrudHelper — Global Render Mode Texts

    [Fact]
    public void GlobalServerRenderModeText_ContainsInteractiveServer_Net10()
    {
        Assert.Contains("InteractiveServer", BlazorCrudHelper.GlobalServerRenderModeText);
        Assert.Contains("HeadOutlet", BlazorCrudHelper.GlobalServerRenderModeText);
    }

    [Fact]
    public void GlobalWebAssemblyRenderModeText_ContainsInteractiveWebAssembly_Net10()
    {
        Assert.Contains("InteractiveWebAssembly", BlazorCrudHelper.GlobalWebAssemblyRenderModeText);
        Assert.Contains("HeadOutlet", BlazorCrudHelper.GlobalWebAssemblyRenderModeText);
    }

    [Fact]
    public void GlobalServerRenderModeRoutesText_ContainsRoutes_Net10()
    {
        Assert.Contains("Routes", BlazorCrudHelper.GlobalServerRenderModeRoutesText);
        Assert.Contains("InteractiveServer", BlazorCrudHelper.GlobalServerRenderModeRoutesText);
    }

    [Fact]
    public void GlobalWebAssemblyRenderModeRoutesText_ContainsRoutes_Net10()
    {
        Assert.Contains("Routes", BlazorCrudHelper.GlobalWebAssemblyRenderModeRoutesText);
        Assert.Contains("InteractiveWebAssembly", BlazorCrudHelper.GlobalWebAssemblyRenderModeRoutesText);
    }

    #endregion

    #region BlazorCrudHelper — GetTextTemplatingProperties

    [Fact]
    public void GetTextTemplatingProperties_WithEmptyTemplatePaths_ReturnsEmpty_Net10()
    {
        var model = CreateTestBlazorCrudModelWithProjectInfo();
        var result = BlazorCrudHelper.GetTextTemplatingProperties(Enumerable.Empty<string>(), model);
        Assert.Empty(result);
    }

    [Fact]
    public void GetTextTemplatingProperties_WithNullProjectInfo_ReturnsEmpty_Net10()
    {
        var model = new BlazorCrudModel
        {
            PageType = "CRUD",
            ModelInfo = CreateTestModelInfo(),
            DbContextInfo = CreateTestDbContextInfo(),
            ProjectInfo = null!
        };

        var templatePaths = new[] { Path.Combine("templates", BlazorCrudHelper.CreateBlazorTemplate) };
        var result = BlazorCrudHelper.GetTextTemplatingProperties(templatePaths, model);
        Assert.Empty(result);
    }

    [Fact]
    public void GetTextTemplatingProperties_WithValidCRUDType_GeneratesPropertiesForMatchingTemplates_Net10()
    {
        var model = CreateTestBlazorCrudModelWithProjectInfo();
        var templatePaths = new[]
        {
            Path.Combine("templates", BlazorCrudHelper.CreateBlazorTemplate),
            Path.Combine("templates", BlazorCrudHelper.DeleteBlazorTemplate),
            Path.Combine("templates", BlazorCrudHelper.DetailsBlazorTemplate),
            Path.Combine("templates", BlazorCrudHelper.EditBlazorTemplate),
            Path.Combine("templates", BlazorCrudHelper.IndexBlazorTemplate),
        };

        var result = BlazorCrudHelper.GetTextTemplatingProperties(templatePaths, model).ToList();

        Assert.NotEmpty(result);
        Assert.All(result, prop =>
        {
            Assert.NotNull(prop.TemplateType);
            Assert.NotNull(prop.OutputPath);
            Assert.EndsWith(AspNetConstants.BlazorExtension, prop.OutputPath);
            Assert.Equal("Model", prop.TemplateModelName);
        });
    }

    [Fact]
    public void GetTextTemplatingProperties_WithAllSixTemplatesIncludingNotFound_Net10()
    {
        // Net10 includes the NotFound template in its template folder.
        // Verify that GetTextTemplatingProperties handles all 6 templates.
        var model = CreateTestBlazorCrudModelWithProjectInfo();
        var templatePaths = new[]
        {
            Path.Combine("templates", BlazorCrudHelper.CreateBlazorTemplate),
            Path.Combine("templates", BlazorCrudHelper.DeleteBlazorTemplate),
            Path.Combine("templates", BlazorCrudHelper.DetailsBlazorTemplate),
            Path.Combine("templates", BlazorCrudHelper.EditBlazorTemplate),
            Path.Combine("templates", BlazorCrudHelper.IndexBlazorTemplate),
            Path.Combine("templates", BlazorCrudHelper.NotFoundBlazorTemplate),
        };

        var result = BlazorCrudHelper.GetTextTemplatingProperties(templatePaths, model).ToList();

        Assert.Equal(6, result.Count);
        Assert.All(result, prop =>
        {
            Assert.NotNull(prop.TemplateType);
            Assert.NotNull(prop.OutputPath);
            Assert.EndsWith(AspNetConstants.BlazorExtension, prop.OutputPath);
            Assert.Equal("Model", prop.TemplateModelName);
        });
    }

    [Fact]
    public void GetTextTemplatingProperties_WithSpecificPageType_OnlyGeneratesMatchingTemplate_Net10()
    {
        var model = new BlazorCrudModel
        {
            PageType = "Create",
            ModelInfo = CreateTestModelInfo(),
            DbContextInfo = CreateTestDbContextInfo(),
            ProjectInfo = new ProjectInfo(_testProjectPath)
        };

        var templatePaths = new[]
        {
            Path.Combine("templates", BlazorCrudHelper.CreateBlazorTemplate),
            Path.Combine("templates", BlazorCrudHelper.DeleteBlazorTemplate),
        };

        var result = BlazorCrudHelper.GetTextTemplatingProperties(templatePaths, model).ToList();

        // With a specific page type (non-CRUD), only the matching template should be generated.
        // IsValidTemplate returns false for non-matching, causing a "break" in the loop.
        Assert.Single(result);
        Assert.Contains("Create", result[0].OutputPath);
    }

    [Fact]
    public void GetTextTemplatingProperties_OutputPaths_ContainModelNamePages_Net10()
    {
        var model = CreateTestBlazorCrudModelWithProjectInfo();
        var templatePaths = new[]
        {
            Path.Combine("templates", BlazorCrudHelper.CreateBlazorTemplate),
        };

        var result = BlazorCrudHelper.GetTextTemplatingProperties(templatePaths, model).ToList();

        Assert.NotEmpty(result);
        Assert.Contains("ProductPages", result[0].OutputPath);
    }

    [Fact]
    public void GetTextTemplatingProperties_NotFoundTemplate_OutputsToComponentsPages_Net10()
    {
        var model = CreateTestBlazorCrudModelWithProjectInfo();
        var templatePaths = new[]
        {
            Path.Combine("templates", BlazorCrudHelper.NotFoundBlazorTemplate),
        };

        var result = BlazorCrudHelper.GetTextTemplatingProperties(templatePaths, model).ToList();

        Assert.NotEmpty(result);
        // NotFound goes to Components/Pages/ (not Components/Pages/{Model}Pages/)
        Assert.Contains(Path.Combine("Components", "Pages"), result[0].OutputPath);
        Assert.DoesNotContain("ProductPages", result[0].OutputPath);
    }

    #endregion

    #region BlazorCrudAppProperties — Defaults

    [Fact]
    public void BlazorCrudAppProperties_DefaultsToAllFalse_Net10()
    {
        var props = new BlazorCrudAppProperties();

        Assert.False(props.AddRazorComponentsExists);
        Assert.False(props.InteractiveServerComponentsExists);
        Assert.False(props.InteractiveWebAssemblyComponentsExists);
        Assert.False(props.MapRazorComponentsExists);
        Assert.False(props.InteractiveServerRenderModeNeeded);
        Assert.False(props.InteractiveWebAssemblyRenderModeNeeded);
        Assert.False(props.IsHeadOutletGlobal);
        Assert.False(props.AreRoutesGlobal);
    }

    [Fact]
    public void BlazorCrudAppProperties_PropertiesCanBeSet_Net10()
    {
        var props = new BlazorCrudAppProperties
        {
            AddRazorComponentsExists = true,
            InteractiveServerComponentsExists = true,
            MapRazorComponentsExists = true,
            IsHeadOutletGlobal = true,
            AreRoutesGlobal = true,
        };

        Assert.True(props.AddRazorComponentsExists);
        Assert.True(props.InteractiveServerComponentsExists);
        Assert.True(props.MapRazorComponentsExists);
        Assert.True(props.IsHeadOutletGlobal);
        Assert.True(props.AreRoutesGlobal);
    }

    #endregion

    #region ModelInfo — Property Behaviors

    [Fact]
    public void ModelInfo_ModelTypeNameCapitalized_CapitalizesFirstLetter_Net10()
    {
        var modelInfo = new ModelInfo { ModelTypeName = "product" };
        Assert.Equal("Product", modelInfo.ModelTypeNameCapitalized);
    }

    [Fact]
    public void ModelInfo_ModelTypePluralName_AppendsSuffix_Net10()
    {
        var modelInfo = new ModelInfo { ModelTypeName = "Product" };
        Assert.Equal("Products", modelInfo.ModelTypePluralName);
    }

    [Fact]
    public void ModelInfo_ModelVariable_IsLowercase_Net10()
    {
        var modelInfo = new ModelInfo { ModelTypeName = "Product" };
        Assert.Equal("product", modelInfo.ModelVariable);
    }

    #endregion

    #region DbContextInfo — Property Behaviors

    [Fact]
    public void DbContextInfo_EfScenario_DefaultsFalse_Net10()
    {
        var dbContextInfo = new DbContextInfo();
        Assert.False(dbContextInfo.EfScenario);
    }

    [Fact]
    public void DbContextInfo_CanSetAllProperties_Net10()
    {
        var dbContextInfo = new DbContextInfo
        {
            DbContextClassName = "AppDbContext",
            DbContextClassPath = "/path/to/AppDbContext.cs",
            DbContextNamespace = "TestProject.Data",
            DatabaseProvider = PackageConstants.EfConstants.SqlServer,
            EfScenario = true,
            EntitySetVariableName = "Products",
            NewDbSetStatement = "public DbSet<Product> Products { get; set; }"
        };

        Assert.Equal("AppDbContext", dbContextInfo.DbContextClassName);
        Assert.Equal("/path/to/AppDbContext.cs", dbContextInfo.DbContextClassPath);
        Assert.Equal("TestProject.Data", dbContextInfo.DbContextNamespace);
        Assert.Equal(PackageConstants.EfConstants.SqlServer, dbContextInfo.DatabaseProvider);
        Assert.True(dbContextInfo.EfScenario);
        Assert.Equal("Products", dbContextInfo.EntitySetVariableName);
    }

    #endregion

    #region PackageConstants — EF Provider Mappings

    [Fact]
    public void EfPackagesDict_ContainsSqlServer_Net10()
    {
        Assert.True(PackageConstants.EfConstants.EfPackagesDict.ContainsKey(PackageConstants.EfConstants.SqlServer));
    }

    [Fact]
    public void EfPackagesDict_ContainsSqlite_Net10()
    {
        Assert.True(PackageConstants.EfConstants.EfPackagesDict.ContainsKey(PackageConstants.EfConstants.SQLite));
    }

    [Fact]
    public void EfPackagesDict_ContainsCosmosDb_Net10()
    {
        Assert.True(PackageConstants.EfConstants.EfPackagesDict.ContainsKey(PackageConstants.EfConstants.CosmosDb));
    }

    [Fact]
    public void EfPackagesDict_ContainsPostgres_Net10()
    {
        Assert.True(PackageConstants.EfConstants.EfPackagesDict.ContainsKey(PackageConstants.EfConstants.Postgres));
    }

    [Fact]
    public void EfPackagesDict_ContainsExactlyFourProviders_Net10()
    {
        Assert.Equal(4, PackageConstants.EfConstants.EfPackagesDict.Count);
    }

    [Fact]
    public void SqlServerConstant_HasExpectedValue_Net10()
    {
        Assert.Equal("sqlserver-efcore", PackageConstants.EfConstants.SqlServer);
    }

    [Fact]
    public void SQLiteConstant_HasExpectedValue_Net10()
    {
        Assert.Equal("sqlite-efcore", PackageConstants.EfConstants.SQLite);
    }

    [Fact]
    public void CosmosDbConstant_HasExpectedValue_Net10()
    {
        Assert.Equal("cosmos-efcore", PackageConstants.EfConstants.CosmosDb);
    }

    [Fact]
    public void PostgresConstant_HasExpectedValue_Net10()
    {
        Assert.Equal("npgsql-efcore", PackageConstants.EfConstants.Postgres);
    }

    [Fact]
    public void QuickGridEfAdapterPackage_HasCorrectName_Net10()
    {
        Assert.Equal("Microsoft.AspNetCore.Components.QuickGrid.EntityFrameworkAdapter", PackageConstants.AspNetCorePackages.QuickGridEfAdapterPackage.Name);
    }

    [Fact]
    public void AspNetCoreDiagnosticsEfCorePackage_HasCorrectName_Net10()
    {
        Assert.Equal("Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore", PackageConstants.AspNetCorePackages.AspNetCoreDiagnosticsEfCorePackage.Name);
    }

    [Fact]
    public void EfCoreToolsPackage_HasCorrectName_Net10()
    {
        Assert.Equal("Microsoft.EntityFrameworkCore.Tools", PackageConstants.EfConstants.EfCoreToolsPackage.Name);
    }

    #endregion

    #region Scaffolder Registration — Builder Extensions

    [Fact]
    public void WithBlazorCrudTextTemplatingStep_ReturnsBuilder_Net10()
    {
        Mock<IScaffoldBuilder> mockBuilder = new Mock<IScaffoldBuilder>();
        mockBuilder.Setup(b => b.WithStep<WrappedTextTemplatingStep>(It.IsAny<Action<ScaffoldStepConfigurator<WrappedTextTemplatingStep>>>()))
            .Returns(mockBuilder.Object);

        IScaffoldBuilder result = Scaffolding.Core.Hosting.BlazorCrudScaffolderBuilderExtensions.WithBlazorCrudTextTemplatingStep(mockBuilder.Object);

        Assert.NotNull(result);
        mockBuilder.Verify(b => b.WithStep<WrappedTextTemplatingStep>(It.IsAny<Action<ScaffoldStepConfigurator<WrappedTextTemplatingStep>>>()), Times.Once);
    }

    [Fact]
    public void WithBlazorCrudAddPackagesStep_ReturnsBuilder_Net10()
    {
        Mock<IScaffoldBuilder> mockBuilder = new Mock<IScaffoldBuilder>();
        mockBuilder.Setup(b => b.WithStep<WrappedAddPackagesStep>(It.IsAny<Action<ScaffoldStepConfigurator<WrappedAddPackagesStep>>>()))
            .Returns(mockBuilder.Object);

        IScaffoldBuilder result = Scaffolding.Core.Hosting.BlazorCrudScaffolderBuilderExtensions.WithBlazorCrudAddPackagesStep(mockBuilder.Object);

        Assert.NotNull(result);
        mockBuilder.Verify(b => b.WithStep<WrappedAddPackagesStep>(It.IsAny<Action<ScaffoldStepConfigurator<WrappedAddPackagesStep>>>()), Times.Once);
    }

    [Fact]
    public void WithBlazorCrudCodeChangeStep_RegistersTwoCodeModificationSteps_Net10()
    {
        Mock<IScaffoldBuilder> mockBuilder = new Mock<IScaffoldBuilder>();
        int callCount = 0;

        mockBuilder.Setup(b => b.WithStep<WrappedCodeModificationStep>(It.IsAny<Action<ScaffoldStepConfigurator<WrappedCodeModificationStep>>>()))
            .Callback(() => callCount++)
            .Returns(mockBuilder.Object);

        Scaffolding.Core.Hosting.BlazorCrudScaffolderBuilderExtensions.WithBlazorCrudCodeChangeStep(mockBuilder.Object);

        Assert.Equal(2, callCount);
    }

    #endregion

    #region Scaffolder Registration — GetScaffoldSteps Contains ValidateBlazorCrudStep

    [Fact]
    public void GetScaffoldSteps_ContainsValidateBlazorCrudStep_Net10()
    {
        var mockRunnerBuilder = new Mock<IScaffoldRunnerBuilder>();
        var service = new AspNetCommandService(mockRunnerBuilder.Object);

        Type[] stepTypes = service.GetScaffoldSteps();

        Assert.Contains(typeof(ValidateBlazorCrudStep), stepTypes);
    }

    [Fact]
    public void GetScaffoldSteps_ContainsWrappedTextTemplatingStep_Net10()
    {
        var mockRunnerBuilder = new Mock<IScaffoldRunnerBuilder>();
        var service = new AspNetCommandService(mockRunnerBuilder.Object);

        Type[] stepTypes = service.GetScaffoldSteps();

        Assert.Contains(typeof(WrappedTextTemplatingStep), stepTypes);
    }

    [Fact]
    public void GetScaffoldSteps_ContainsWrappedAddPackagesStep_Net10()
    {
        var mockRunnerBuilder = new Mock<IScaffoldRunnerBuilder>();
        var service = new AspNetCommandService(mockRunnerBuilder.Object);

        Type[] stepTypes = service.GetScaffoldSteps();

        Assert.Contains(typeof(WrappedAddPackagesStep), stepTypes);
    }

    [Fact]
    public void GetScaffoldSteps_ContainsWrappedCodeModificationStep_Net10()
    {
        var mockRunnerBuilder = new Mock<IScaffoldRunnerBuilder>();
        var service = new AspNetCommandService(mockRunnerBuilder.Object);

        Type[] stepTypes = service.GetScaffoldSteps();

        Assert.Contains(typeof(WrappedCodeModificationStep), stepTypes);
    }

    #endregion

    #region CrudSettings — Property Initialization

    [Fact]
    public void CrudSettings_CanBeCreated_WithRequiredProperties_Net10()
    {
        var settings = new CrudSettings
        {
            Project = _testProjectPath,
            Model = "Product",
            Page = "CRUD",
            DataContext = "AppDbContext",
            DatabaseProvider = PackageConstants.EfConstants.SqlServer,
            Prerelease = false
        };

        Assert.Equal(_testProjectPath, settings.Project);
        Assert.Equal("Product", settings.Model);
        Assert.Equal("CRUD", settings.Page);
        Assert.Equal("AppDbContext", settings.DataContext);
        Assert.Equal(PackageConstants.EfConstants.SqlServer, settings.DatabaseProvider);
        Assert.False(settings.Prerelease);
    }

    [Fact]
    public void CrudSettings_Prerelease_CanBeSetToTrue_Net10()
    {
        var settings = new CrudSettings
        {
            Project = _testProjectPath,
            Model = "Product",
            Page = "CRUD",
            Prerelease = true
        };

        Assert.True(settings.Prerelease);
    }

    [Fact]
    public void CrudSettings_Page_SupportsAllPageTypes_Net10()
    {
        foreach (var pageType in BlazorCrudHelper.CRUDPages)
        {
            var settings = new CrudSettings
            {
                Project = _testProjectPath,
                Model = "Product",
                Page = pageType
            };

            Assert.Equal(pageType, settings.Page);
        }
    }

    #endregion

    #region Regression Guards

    [Fact]
    public async Task RegressionGuard_AllNullInputs_DoNotThrow_Net10()
    {
        var step = new ValidateBlazorCrudStep(
            _mockFileSystem.Object,
            NullLogger<ValidateBlazorCrudStep>.Instance,
            _testTelemetryService)
        {
            Project = null,
            Model = null,
            Page = null,
            DataContext = null,
            DatabaseProvider = null
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);
        Assert.False(result);
    }

    [Fact]
    public async Task RegressionGuard_AllEmptyInputs_DoNotThrow_Net10()
    {
        var step = new ValidateBlazorCrudStep(
            _mockFileSystem.Object,
            NullLogger<ValidateBlazorCrudStep>.Instance,
            _testTelemetryService)
        {
            Project = string.Empty,
            Model = string.Empty,
            Page = string.Empty,
            DataContext = string.Empty,
            DatabaseProvider = string.Empty
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);
        Assert.False(result);
    }

    [Fact]
    public async Task RegressionGuard_NonExistentProject_ReturnsFalseNotException_Net10()
    {
        _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(false);

        var step = new ValidateBlazorCrudStep(
            _mockFileSystem.Object,
            NullLogger<ValidateBlazorCrudStep>.Instance,
            _testTelemetryService)
        {
            Project = @"C:\NonExistent\Path\Project.csproj",
            Model = "Product",
            Page = "CRUD",
            DataContext = "AppDbContext",
            DatabaseProvider = PackageConstants.EfConstants.SqlServer
        };

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);
        Assert.False(result);
    }

    [Fact]
    public void RegressionGuard_GetTemplateType_AllTemplatesReturnNonNull_Net10()
    {
        string[] templates = new[]
        {
            BlazorCrudHelper.CreateBlazorTemplate,
            BlazorCrudHelper.DeleteBlazorTemplate,
            BlazorCrudHelper.DetailsBlazorTemplate,
            BlazorCrudHelper.EditBlazorTemplate,
            BlazorCrudHelper.IndexBlazorTemplate,
            BlazorCrudHelper.NotFoundBlazorTemplate
        };

        foreach (var template in templates)
        {
            var result = BlazorCrudHelper.GetTemplateType(Path.Combine("any", template));
            Assert.NotNull(result);
        }
    }

    [Fact]
    public void RegressionGuard_CRUDPages_ListHasNoDuplicates_Net10()
    {
        var distinct = BlazorCrudHelper.CRUDPages.Distinct(StringComparer.OrdinalIgnoreCase);
        Assert.Equal(BlazorCrudHelper.CRUDPages.Count, distinct.Count());
    }

    #endregion

    #region Test Helpers

    private static ModelInfo CreateTestModelInfo()
    {
        return new ModelInfo
        {
            ModelTypeName = "Product",
            ModelNamespace = "TestProject.Models",
            ModelFullName = "TestProject.Models.Product",
            PrimaryKeyName = "Id",
            PrimaryKeyShortTypeName = "int",
            PrimaryKeyTypeName = "System.Int32"
        };
    }

    private static DbContextInfo CreateTestDbContextInfo()
    {
        return new DbContextInfo
        {
            DbContextClassName = "AppDbContext",
            DbContextNamespace = "TestProject.Data",
            DatabaseProvider = PackageConstants.EfConstants.SqlServer,
            EfScenario = true,
            EntitySetVariableName = "Products"
        };
    }

    private BlazorCrudModel CreateTestBlazorCrudModel()
    {
        return new BlazorCrudModel
        {
            PageType = "CRUD",
            ModelInfo = CreateTestModelInfo(),
            DbContextInfo = CreateTestDbContextInfo(),
            ProjectInfo = new ProjectInfo(null)
        };
    }

    private BlazorCrudModel CreateTestBlazorCrudModelWithProjectInfo()
    {
        return new BlazorCrudModel
        {
            PageType = "CRUD",
            ModelInfo = CreateTestModelInfo(),
            DbContextInfo = CreateTestDbContextInfo(),
            ProjectInfo = new ProjectInfo(_testProjectPath)
        };
    }

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
