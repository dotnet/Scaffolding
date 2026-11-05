// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Collections.Generic;
using System.IO;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.Helpers;

public class AspNetDbContextHelperTests
{
    [Theory]
    [InlineData(null, "MyDbContext")]
    [InlineData("", "MyDbContext")]
    [InlineData("Library.Data", "Library.Data.MyDbContext")]
    public void GetDbContextCodeModifierProperties_QualifiesOnlyTypeNames(string? contextNamespace, string expectedType)
    {
        var context = new DbContextInfo
        {
            EfScenario = true,
            DatabaseProvider = PackageConstants.EfConstants.CosmosDb,
            DbContextClassName = "MyDbContext",
            DbContextNamespace = contextNamespace
        };

        var properties = AspNetDbContextHelper.GetDbContextCodeModifierProperties(context);

        Assert.Equal(expectedType, properties[Microsoft.DotNet.Scaffolding.Internal.Constants.CodeModifierPropertyConstants.DbContextName]);
        Assert.Equal("MyDbContext", properties[Microsoft.DotNet.Scaffolding.Internal.Constants.CodeModifierPropertyConstants.ConnectionStringName]);
        Assert.Equal("UseCosmos(connectionString, \"MyDbContext\")",
            properties[Microsoft.DotNet.Scaffolding.Internal.Constants.CodeModifierPropertyConstants.UseDbMethod]);
    }

    [Fact]
    public void GetDbContextCodeModifierProperties_WithEfScenarioAndSqlServer_ReturnsExpectedProperties()
    {
        // Arrange
        DbContextInfo dbContextInfo = new DbContextInfo
        {
            EfScenario = true,
            DatabaseProvider = PackageConstants.EfConstants.SqlServer,
            DbContextClassName = "ApplicationDbContext",
            DbContextNamespace = "MyApp.Data"
        };

        // Act
        Dictionary<string, string> result = AspNetDbContextHelper.GetDbContextCodeModifierProperties(dbContextInfo);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.ContainsKey(Microsoft.DotNet.Scaffolding.Internal.Constants.CodeModifierPropertyConstants.UseDbMethod));
        Assert.Equal("UseSqlServer(connectionString)", result[Microsoft.DotNet.Scaffolding.Internal.Constants.CodeModifierPropertyConstants.UseDbMethod]);
        Assert.Equal("MyApp.Data.ApplicationDbContext", result[Microsoft.DotNet.Scaffolding.Internal.Constants.CodeModifierPropertyConstants.DbContextName]);
        Assert.Equal("ApplicationDbContext", result[Microsoft.DotNet.Scaffolding.Internal.Constants.CodeModifierPropertyConstants.ConnectionStringName]);
        Assert.Equal("MyApp.Data", result[Microsoft.DotNet.Scaffolding.Internal.Constants.CodeModifierPropertyConstants.DbContextNamespace]);
    }

    [Fact]
    public void GetDbContextCodeModifierProperties_WithCosmosDb_ReturnsCosmosSpecificMethod()
    {
        // Arrange
        DbContextInfo dbContextInfo = new DbContextInfo
        {
            EfScenario = true,
            DatabaseProvider = PackageConstants.EfConstants.CosmosDb,
            DbContextClassName = "CosmosDbContext"
        };

        // Act
        Dictionary<string, string> result = AspNetDbContextHelper.GetDbContextCodeModifierProperties(dbContextInfo);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.ContainsKey(Microsoft.DotNet.Scaffolding.Internal.Constants.CodeModifierPropertyConstants.UseDbMethod));
        Assert.Equal("UseCosmos(connectionString, \"CosmosDbContext\")", result[Microsoft.DotNet.Scaffolding.Internal.Constants.CodeModifierPropertyConstants.UseDbMethod]);
    }

    [Fact]
    public void GetDbContextCodeModifierProperties_WithSqlite_ReturnsUseSqliteMethod()
    {
        // Arrange
        DbContextInfo dbContextInfo = new DbContextInfo
        {
            EfScenario = true,
            DatabaseProvider = PackageConstants.EfConstants.SQLite,
            DbContextClassName = "SqliteDbContext"
        };

        // Act
        Dictionary<string, string> result = AspNetDbContextHelper.GetDbContextCodeModifierProperties(dbContextInfo);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.ContainsKey(Microsoft.DotNet.Scaffolding.Internal.Constants.CodeModifierPropertyConstants.UseDbMethod));
        Assert.Equal("UseSqlite(connectionString)", result[Microsoft.DotNet.Scaffolding.Internal.Constants.CodeModifierPropertyConstants.UseDbMethod]);
    }

    [Fact]
    public void GetDbContextCodeModifierProperties_WithPostgres_ReturnsUseNpgsqlMethod()
    {
        // Arrange
        DbContextInfo dbContextInfo = new DbContextInfo
        {
            EfScenario = true,
            DatabaseProvider = PackageConstants.EfConstants.Postgres,
            DbContextClassName = "PostgresDbContext"
        };

        // Act
        Dictionary<string, string> result = AspNetDbContextHelper.GetDbContextCodeModifierProperties(dbContextInfo);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.ContainsKey(Microsoft.DotNet.Scaffolding.Internal.Constants.CodeModifierPropertyConstants.UseDbMethod));
        Assert.Equal("UseNpgsql(connectionString)", result[Microsoft.DotNet.Scaffolding.Internal.Constants.CodeModifierPropertyConstants.UseDbMethod]);
    }

    [Fact]
    public void GetDbContextCodeModifierProperties_WithNonEfScenario_ReturnsEmptyDictionary()
    {
        // Arrange
        DbContextInfo dbContextInfo = new DbContextInfo
        {
            EfScenario = false,
            DatabaseProvider = PackageConstants.EfConstants.SqlServer,
            DbContextClassName = "ApplicationDbContext"
        };

        // Act
        Dictionary<string, string> result = AspNetDbContextHelper.GetDbContextCodeModifierProperties(dbContextInfo);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void GetDbContextCodeModifierProperties_WithNullDatabaseProvider_DoesNotIncludeUseDbMethod()
    {
        // Arrange
        DbContextInfo dbContextInfo = new DbContextInfo
        {
            EfScenario = true,
            DatabaseProvider = null,
            DbContextClassName = "ApplicationDbContext",
            DbContextNamespace = "MyApp.Data"
        };

        // Act
        Dictionary<string, string> result = AspNetDbContextHelper.GetDbContextCodeModifierProperties(dbContextInfo);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.ContainsKey(Microsoft.DotNet.Scaffolding.Internal.Constants.CodeModifierPropertyConstants.UseDbMethod));
        Assert.True(result.ContainsKey(Microsoft.DotNet.Scaffolding.Internal.Constants.CodeModifierPropertyConstants.DbContextName));
    }

    [Fact]
    public void GetIdentityDataContextPath_WithValidInputs_ReturnsExpectedPath()
    {
        // Arrange
        string projectPath = Path.Combine("test", "project", "TestProject.csproj");
        string className = "ApplicationDbContext";

        // Act
        string result = AspNetDbContextHelper.GetIdentityDataContextPath(projectPath, className);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains("Data", result);
        Assert.Contains("ApplicationDbContext.cs", result);
    }

    [Fact]
    public void GetIdentityDataContextPath_WithClassNameWithoutExtension_AddsExtension()
    {
        // Arrange
        string projectPath = Path.Combine("test", "project", "TestProject.csproj");
        string className = "MyDbContext";

        // Act
        string result = AspNetDbContextHelper.GetIdentityDataContextPath(projectPath, className);

        // Assert
        Assert.EndsWith(".cs", result);
        Assert.Contains("MyDbContext.cs", result);
    }

    [Fact]
    public void GetIdentityDataContextPath_WithClassNameWithExtension_DoesNotDuplicateExtension()
    {
        // Arrange
        string projectPath = Path.Combine("test", "project", "TestProject.csproj");
        string className = "MyDbContext.cs";

        // Act
        string result = AspNetDbContextHelper.GetIdentityDataContextPath(projectPath, className);

        // Assert
        Assert.EndsWith(".cs", result);
        Assert.DoesNotContain(".cs.cs", result);
    }

    [Fact]
    public void DbContextTypeDefaults_IsNotNull()
    {
        // Assert - Can only verify the dictionary exists since it's internal
        Assert.NotNull(AspNetDbContextHelper.DbContextTypeDefaults);
    }

    [Fact]
    public void IdentityDbContextTypeDefaults_IsNotNull()
    {
        // Assert - Can only verify the dictionary exists since it's internal
        Assert.NotNull(AspNetDbContextHelper.IdentityDbContextTypeDefaults);
    }
}
