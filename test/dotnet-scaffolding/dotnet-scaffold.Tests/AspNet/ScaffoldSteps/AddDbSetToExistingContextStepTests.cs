// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.TextTemplating.DbContext;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.ScaffoldSteps;

public class AddDbSetToExistingContextStepTests : IDisposable
{
    private readonly Mock<IScaffolder> _mockScaffolder;
    private readonly ScaffolderContext _context;
    private readonly string _tempDir;

    public AddDbSetToExistingContextStepTests()
    {
        _mockScaffolder = new Mock<IScaffolder>();
        _mockScaffolder.Setup(s => s.DisplayName).Returns("TestScaffolder");
        _mockScaffolder.Setup(s => s.Name).Returns("test-scaffolder");
        _context = new ScaffolderContext(_mockScaffolder.Object);
        _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    private static AddDbSetToExistingContextStep CreateStep() =>
        new AddDbSetToExistingContextStep(NullLogger<AddDbSetToExistingContextStep>.Instance);

    private string WriteContextFile(string content)
    {
        string path = Path.Combine(_tempDir, "ApplicationDbContext.cs");
        File.WriteAllText(path, content);
        return path;
    }

    // -----------------------------------------------------------------------
    // Scenario 1: No DbContextProperties in context — step is a no-op
    // -----------------------------------------------------------------------
    [Fact]
    public async Task ExecuteAsync_ReturnsTrue_WhenNoDbContextPropertiesInContext()
    {
        AddDbSetToExistingContextStep step = CreateStep();
        bool result = await step.ExecuteAsync(_context, CancellationToken.None);
        Assert.True(result);
    }

    // -----------------------------------------------------------------------
    // Scenario 2: DbContextProperties present but DbSetStatement is null
    //             (DbSet already existed — EntitySetVariableName was found)
    // -----------------------------------------------------------------------
    [Fact]
    public async Task ExecuteAsync_ReturnsTrue_WhenDbSetStatementIsNull()
    {
        string path = WriteContextFile("public class ApplicationDbContext { }");
        DbContextProperties props = new DbContextProperties { DbContextPath = path, DbSetStatement = null };
        _context.Properties[nameof(DbContextProperties)] = props;

        AddDbSetToExistingContextStep step = CreateStep();
        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.True(result);
    }

    // -----------------------------------------------------------------------
    // Scenario 3: DbContextProperties present but DbContextPath does not exist
    //             (new context — WithDbContextStep will create it via T4 template)
    // -----------------------------------------------------------------------
    [Fact]
    public async Task ExecuteAsync_ReturnsTrue_WhenFileDoesNotExist()
    {
        DbContextProperties props = new DbContextProperties
        {
            DbContextPath = Path.Combine(_tempDir, "NonExistent.cs"),
            DbSetStatement = "public DbSet<Movie> Movie { get; set; } = default!;"
        };
        _context.Properties[nameof(DbContextProperties)] = props;

        AddDbSetToExistingContextStep step = CreateStep();
        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.True(result);
    }

    // -----------------------------------------------------------------------
    // Scenario 4: DbSetStatement is null — step is a no-op regardless of file
    // -----------------------------------------------------------------------
    [Fact]
    public async Task ExecuteAsync_ReturnsTrue_WhenDbSetStatementIsEmpty()
    {
        string path = WriteContextFile("public class ApplicationDbContext { }");
        DbContextProperties props = new DbContextProperties { DbContextPath = path, DbSetStatement = string.Empty };
        _context.Properties[nameof(DbContextProperties)] = props;

        AddDbSetToExistingContextStep step = CreateStep();
        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.True(result);
    }

    // -----------------------------------------------------------------------
    // Scenario 5: ProjectPath is null — step is a no-op (Roslyn workspace
    //             cannot be opened without the .csproj path)
    // -----------------------------------------------------------------------
    [Fact]
    public async Task ExecuteAsync_ReturnsTrue_WhenProjectPathIsNull()
    {
        string path = WriteContextFile("public class ApplicationDbContext { }");
        DbContextProperties props = new DbContextProperties
        {
            DbContextPath = path,
            DbSetStatement = "public DbSet<Movie> Movie { get; set; } = default!;"
        };
        _context.Properties[nameof(DbContextProperties)] = props;

        AddDbSetToExistingContextStep step = CreateStep();
        // ProjectPath intentionally left null
        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.True(result);
    }

    // -----------------------------------------------------------------------
    // Scenario 6: BuildCodeModifierConfig produces well-formed JSON
    // -----------------------------------------------------------------------
    [Fact]
    public void BuildCodeModifierConfig_ProducesValidJson()
    {
        const string fileName = "ApplicationDbContext.cs";
        const string dbSetStatement = "public DbSet<Movie> Movie { get; set; } = default!;";

        string json = AddDbSetToExistingContextStep.BuildCodeModifierConfig(fileName, dbSetStatement);

        Assert.False(string.IsNullOrWhiteSpace(json));
        JsonDocument doc = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.Object, doc.RootElement.ValueKind);
    }

    // -----------------------------------------------------------------------
    // Scenario 7: JSON config contains FileName and ClassProperties[0].Block
    //             matching the inputs — DocumentBuilder relies on these fields
    // -----------------------------------------------------------------------
    [Fact]
    public void BuildCodeModifierConfig_ContainsCorrectFileNameAndBlock()
    {
        const string fileName = "MyContext.cs";
        const string dbSetStatement = "public DbSet<Order> Orders { get; set; } = default!;";

        string json = AddDbSetToExistingContextStep.BuildCodeModifierConfig(fileName, dbSetStatement);

        JsonDocument doc = JsonDocument.Parse(json);
        JsonElement filesArray = doc.RootElement.GetProperty("Files");
        Assert.Equal(1, filesArray.GetArrayLength());

        JsonElement fileEntry = filesArray[0];
        Assert.Equal(fileName, fileEntry.GetProperty("FileName").GetString());

        JsonElement classProperties = fileEntry.GetProperty("ClassProperties");
        Assert.Equal(1, classProperties.GetArrayLength());
        Assert.Equal(dbSetStatement, classProperties[0].GetProperty("Block").GetString());
    }

    // -----------------------------------------------------------------------
    // Scenario 8: BuildCodeModifierConfig handles fully-qualified type names
    // -----------------------------------------------------------------------
    [Fact]
    public void BuildCodeModifierConfig_HandlesFullyQualifiedModelName()
    {
        const string fileName = "AppDb.cs";
        const string dbSetStatement = "public DbSet<MyApp.Models.Movie> Movie { get; set; } = default!;";

        string json = AddDbSetToExistingContextStep.BuildCodeModifierConfig(fileName, dbSetStatement);

        JsonDocument doc = JsonDocument.Parse(json);
        JsonElement block = doc.RootElement
            .GetProperty("Files")[0]
            .GetProperty("ClassProperties")[0]
            .GetProperty("Block");

        Assert.Equal(dbSetStatement, block.GetString());
    }

    // -----------------------------------------------------------------------
    // Scenario 9: ProjectPath is exposed as a settable property
    // -----------------------------------------------------------------------
    [Fact]
    public void AddDbSetToExistingContextStep_HasProjectPathProperty()
    {
        Assert.NotNull(typeof(AddDbSetToExistingContextStep).GetProperty("ProjectPath"));
    }

    // -----------------------------------------------------------------------
    // Scenario 10: CodeChangeOptions defaults to an empty list
    // -----------------------------------------------------------------------
    [Fact]
    public void AddDbSetToExistingContextStep_CodeChangeOptions_DefaultsToEmpty()
    {
        AddDbSetToExistingContextStep step = CreateStep();
        Assert.NotNull(step.CodeChangeOptions);
        Assert.Empty(step.CodeChangeOptions);
    }

    // -----------------------------------------------------------------------
    // Scenario 11: File exists and already contains the DbSet
    //              (newly-created context from WithDbContextStep) — step skips
    // -----------------------------------------------------------------------
    [Fact]
    public async Task ExecuteAsync_ReturnsTrue_WhenDbSetAlreadyPresentInFile()
    {
        const string dbSetStatement = "public DbSet<Movie> Movie { get; set; } = default!;";
        string path = WriteContextFile($"public class ApplicationDbContext {{ {dbSetStatement} }}");
        DbContextProperties props = new DbContextProperties
        {
            DbContextPath = path,
            DbSetStatement = dbSetStatement
        };
        _context.Properties[nameof(DbContextProperties)] = props;

        AddDbSetToExistingContextStep step = CreateStep();
        step.ProjectPath = Path.Combine(_tempDir, "App.csproj");

        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        Assert.True(result);
    }

    // -----------------------------------------------------------------------
    // Scenario 12: DbSetAlreadyPresent returns true when marker is in file
    // -----------------------------------------------------------------------
    [Fact]
    public void DbSetAlreadyPresent_ReturnsTrue_WhenDbSetIsInFile()
    {
        const string statement = "public DbSet<Movie> Movie { get; set; } = default!;";
        const string fileContent = "public class Ctx { public DbSet<Movie> Movie { get; set; } = default!; }";

        Assert.True(AddDbSetToExistingContextStep.DbSetAlreadyPresent(fileContent, statement));
    }

    // -----------------------------------------------------------------------
    // Scenario 13: DbSetAlreadyPresent returns false when marker is absent
    // -----------------------------------------------------------------------
    [Fact]
    public void DbSetAlreadyPresent_ReturnsFalse_WhenDbSetNotInFile()
    {
        const string statement = "public DbSet<Movie> Movie { get; set; } = default!;";
        const string fileContent = "public class Ctx { }";

        Assert.False(AddDbSetToExistingContextStep.DbSetAlreadyPresent(fileContent, statement));
    }

    // -----------------------------------------------------------------------
    // Scenario 14: DbSetAlreadyPresent returns true for fully-qualified names
    // -----------------------------------------------------------------------
    [Fact]
    public void DbSetAlreadyPresent_ReturnsTrue_WithFullyQualifiedTypeName()
    {
        const string statement = "public DbSet<MyApp.Models.Movie> Movie { get; set; } = default!;";
        const string fileContent = "public class Ctx { public DbSet<MyApp.Models.Movie> Movie { get; set; } = default!; }";

        Assert.True(AddDbSetToExistingContextStep.DbSetAlreadyPresent(fileContent, statement));
    }

    // -----------------------------------------------------------------------
    // Scenario 15: DbSetAlreadyPresent returns false when statement lacks DbSet<
    // -----------------------------------------------------------------------
    [Fact]
    public void DbSetAlreadyPresent_ReturnsFalse_WhenStatementHasNoDbSetMarker()
    {
        const string statement = "public string Foo { get; set; }";
        const string fileContent = "public class Ctx { public string Foo { get; set; } }";

        Assert.False(AddDbSetToExistingContextStep.DbSetAlreadyPresent(fileContent, statement));
    }
}

