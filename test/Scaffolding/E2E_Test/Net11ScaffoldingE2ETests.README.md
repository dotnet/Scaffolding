# .NET 11 Scaffolding End-to-End Tests

## Overview

This document describes the comprehensive end-to-end testing suite for .NET 11 project scaffolding. The tests verify that all scaffolding operations work correctly with the `net11.0` target framework.

## Test File Location

**Test File**: `test\Scaffolding\E2E_Test\Net11ScaffoldingE2ETests.cs`

## Prerequisites

1. .NET 11 SDK installed
2. Set the environment variable `SCAFFOLDING_RunSkippableTests` to any non-empty value to enable test execution:
   ```powershell
   $env:SCAFFOLDING_RunSkippableTests = "true"
   ```

## Test Categories

### 1. Controller Scaffolding Tests

#### `TestNet11_MvcController_EmptyScaffold`
- **Purpose**: Tests generating an empty MVC controller for .NET 11
- **Validates**: 
  - Controller file creation
  - Basic controller structure
  - .NET 11 compatibility

#### `TestNet11_MvcController_WithModel`
- **Purpose**: Tests generating an MVC controller with a model and DbContext
- **Validates**:
  - Controller with CRUD operations
  - DbContext integration
  - Model binding for .NET 11

#### `TestNet11_ApiController`
- **Purpose**: Tests generating a REST API controller
- **Validates**:
  - API controller attributes
  - RESTful endpoint generation
  - DbContext usage in API

### 2. Minimal API Tests

#### `TestNet11_MinimalApi`
- **Purpose**: Tests generating minimal API endpoints for .NET 11
- **Validates**:
  - Endpoint file creation
  - CRUD operation methods (MapGet, MapPost, MapPut, MapDelete)
  - .NET 11 minimal API patterns

#### `TestNet11_MinimalApi_WithDatabase`
- **Purpose**: Tests generating minimal API with database context
- **Validates**:
  - DbContext integration in minimal APIs
  - Data access patterns

### 3. Blazor Scaffolding Tests

#### `TestNet11_BlazorCrud`
- **Purpose**: Tests generating Blazor CRUD pages for .NET 11
- **Validates**:
  - All CRUD pages (Index, Create, Edit, Delete, Details)
  - .NET 11 Blazor component structure
  - @page and @inject directives

#### `TestNet11_BlazorIdentity`
- **Purpose**: Tests generating Blazor Identity pages
- **Validates**:
  - Identity component scaffolding
  - Account pages (Register, Login, Logout)

### 4. Razor Pages Tests

#### `TestNet11_RazorPages`
- **Purpose**: Tests generating Razor Pages with model
- **Validates**:
  - All CRUD Razor Pages
  - PageModel files (.cshtml.cs)
  - DbContext integration

### 5. View Scaffolding Tests

#### `TestNet11_Views_AllTemplates`
- **Purpose**: Tests all view template types for .NET 11
- **Validates**:
  - Empty, Create, Edit, Delete, Details, List views
  - @model directives
  - View content generation

### 6. Identity Scaffolding Tests

#### `TestNet11_Identity`
- **Purpose**: Tests generating ASP.NET Core Identity pages
- **Validates**:
  - Identity areas folder structure
  - Identity page generation
  - DbContext configuration

### 7. Area Scaffolding Tests

#### `TestNet11_Area`
- **Purpose**: Tests generating MVC areas for .NET 11
- **Validates**:
  - Area folder structure
  - Controllers, Views, and Data folders

### 8. Aspire Integration Tests

#### `TestNet11_AspireIntegration`
- **Purpose**: Tests Aspire integration scaffolding for .NET 11
- **Validates**:
  - Aspire package references
  - Aspire storage configuration

## Running the Tests

### Run All .NET 11 E2E Tests
```powershell
# Set environment variable
$env:SCAFFOLDING_RunSkippableTests = "true"

# Run tests
dotnet test test\Scaffolding\E2E_Test\E2E_Test.Tests.csproj --filter "FullyQualifiedName~Net11ScaffoldingE2ETests"
```

### Run Specific Test
```powershell
$env:SCAFFOLDING_RunSkippableTests = "true"
dotnet test test\Scaffolding\E2E_Test\E2E_Test.Tests.csproj --filter "FullyQualifiedName~Net11ScaffoldingE2ETests.TestNet11_MvcController_EmptyScaffold"
```

### Run Tests by Category
```powershell
# Run only controller tests
dotnet test --filter "FullyQualifiedName~Net11ScaffoldingE2ETests&FullyQualifiedName~Controller"

# Run only Blazor tests
dotnet test --filter "FullyQualifiedName~Net11ScaffoldingE2ETests&FullyQualifiedName~Blazor"

# Run only Minimal API tests
dotnet test --filter "FullyQualifiedName~Net11ScaffoldingE2ETests&FullyQualifiedName~MinimalApi"
```

## Test Infrastructure

### Helper Classes

#### `MsBuildProjectSetupHelper` (.NET 11 Methods)
Located in `test\Scaffolding\Shared\MsBuildProjectSetupHelper.cs`

New methods added for .NET 11:
- `SetupNet11Project()` - Basic .NET 11 web project
- `SetupNet11ProjectWithModels()` - Project with models and DbContext
- `SetupNet11MinimalApiProject()` - Minimal API project
- `SetupNet11MinimalApiProjectWithDb()` - Minimal API with database
- `SetupNet11BlazorProject()` - Blazor project
- `SetupNet11BlazorIdentityProject()` - Blazor with Identity
- `SetupNet11IdentityProject()` - MVC with Identity
- `SetupNet11AspireProject()` - Aspire-enabled project

#### `MsBuildProjectStrings` (.NET 11 Templates)
Located in `test\Scaffolding\Shared\MsBuildProjectStrings.cs`

New template strings for .NET 11:
- `Net11ProjectTxt` - Standard web project template
- `Net11LibraryProjectTxt` - Class library template
- `Net11ProgramFileText` - Program.cs for web apps
- `Net11MinimalApiProjectTxt` - Minimal API project template
- `Net11MinimalApiProgramText` - Program.cs for minimal APIs
- `Net11BlazorProjectTxt` - Blazor project template
- `Net11BlazorProgramText` - Program.cs for Blazor
- `Net11BlazorIdentityProjectTxt` - Blazor with Identity template
- `Net11IdentityProjectTxt` - Identity project template
- `Net11AspireProjectTxt` - Aspire project template
- `Net11AspireProgramText` - Program.cs for Aspire
- `ProductContextTxt` - Sample DbContext

## Expected Results

All tests should:
1. ✅ Create temporary test projects
2. ✅ Successfully scaffold requested items
3. ✅ Generate files in correct locations
4. ✅ Include proper .NET 11 syntax and patterns
5. ✅ Build without errors
6. ✅ Clean up temporary files after execution

## Troubleshooting

### Tests are Skipped
**Solution**: Set the environment variable:
```powershell
$env:SCAFFOLDING_RunSkippableTests = "true"
```

### .NET 11 SDK Not Found
**Solution**: Ensure .NET 11 SDK is installed and available in PATH:
```powershell
dotnet --list-sdks
```

### Build Failures
**Solution**: Check package versions in project templates match your installed .NET 11 SDK version

### Test Timeout
**Solution**: Some E2E tests take longer. Increase timeout in test settings or run tests sequentially:
```powershell
dotnet test --settings:test.runsettings
```

## Coverage Summary

| Feature | Test Coverage |
|---------|---------------|
| MVC Controllers | ✅ Full |
| API Controllers | ✅ Full |
| Minimal APIs | ✅ Full |
| Blazor Components | ✅ Full |
| Razor Pages | ✅ Full |
| Views | ✅ Full |
| Identity | ✅ Full |
| Areas | ✅ Full |
| Aspire | ✅ Basic |

## Contributing

When adding new .NET 11 scaffolding features:

1. Add test method to `Net11ScaffoldingE2ETests.cs`
2. Add any required helper methods to `MsBuildProjectSetupHelper.cs`
3. Add any required template strings to `MsBuildProjectStrings.cs`
4. Update this documentation
5. Ensure all tests pass before submitting PR

## Related Files

- **E2E Tests**: `test\Scaffolding\E2E_Test\Net11ScaffoldingE2ETests.cs`
- **Setup Helpers**: `test\Scaffolding\Shared\MsBuildProjectSetupHelper.cs`
- **Template Strings**: `test\Scaffolding\Shared\MsBuildProjectStrings.cs`
- **Template Existence Tests**: `test\dotnet-scaffolding\dotnet-scaffold.Tests\AspNet\Templates\Net11TemplateExistenceTests.cs`

## See Also

- [Getting Started Guide](../../../Getting-Started.md)
- [E2E Test Base Class](E2ETestBase.cs)
- [Template Existence Tests for .NET 11](../../dotnet-scaffolding/dotnet-scaffold.Tests/AspNet/Templates/Net11TemplateExistenceTests.cs)
