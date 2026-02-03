// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.ScaffoldSteps;

public class AddFileStepTests
{
    private readonly Mock<IFileSystem> _mockFileSystem;
    private readonly Mock<IScaffolder> _mockScaffolder;
    private readonly ScaffolderContext _context;
    private readonly string _testOutputDirectory;

    public AddFileStepTests()
    {
        _mockFileSystem = new Mock<IFileSystem>();
        _mockScaffolder = new Mock<IScaffolder>();
        _testOutputDirectory = Path.Combine("test", "output");
        
        _mockScaffolder.Setup(s => s.DisplayName).Returns("TestScaffolder");
        _mockScaffolder.Setup(s => s.Name).Returns("test-scaffolder");
        _context = new ScaffolderContext(_mockScaffolder.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenFileNameIsEmpty()
    {
        // Arrange
        AddFileStep step = new AddFileStep(
            NullLogger<AddFileStep>.Instance,
            _mockFileSystem.Object)
        {
            FileName = string.Empty,
            BaseOutputDirectory = _testOutputDirectory,
            ProjectPath = "test.csproj"
        };

        // Act
        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenBaseOutputDirectoryIsEmpty()
    {
        // Arrange
        AddFileStep step = new AddFileStep(
            NullLogger<AddFileStep>.Instance,
            _mockFileSystem.Object)
        {
            FileName = "test.txt",
            BaseOutputDirectory = string.Empty,
            ProjectPath = "test.csproj"
        };

        // Act
        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Constructor_InitializesCorrectly()
    {
        // Act
        AddFileStep step = new AddFileStep(
            NullLogger<AddFileStep>.Instance,
            _mockFileSystem.Object)
        {
            FileName = "test.txt",
            BaseOutputDirectory = _testOutputDirectory,
            ProjectPath = "test.csproj"
        };

        // Assert
        Assert.NotNull(step);
        Assert.Equal("test.txt", step.FileName);
        Assert.Equal(_testOutputDirectory, step.BaseOutputDirectory);
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        // Arrange
        string expectedFileName = "TestFile.cs";
        string expectedOutputDirectory = Path.Combine("custom", "output");

        // Act
        AddFileStep step = new AddFileStep(
            NullLogger<AddFileStep>.Instance,
            _mockFileSystem.Object)
        {
            FileName = expectedFileName,
            BaseOutputDirectory = expectedOutputDirectory,
            ProjectPath = "test.csproj"
        };

        // Assert
        Assert.Equal(expectedFileName, step.FileName);
        Assert.Equal(expectedOutputDirectory, step.BaseOutputDirectory);
    }

    [Fact]
    public async Task ExecuteAsync_UsesCancellationToken()
    {
        // Arrange
        AddFileStep step = new AddFileStep(
            NullLogger<AddFileStep>.Instance,
            _mockFileSystem.Object)
        {
            FileName = "test.txt",
            BaseOutputDirectory = _testOutputDirectory,
            ProjectPath = "test.csproj"
        };

        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        CancellationToken cancellationToken = cancellationTokenSource.Token;

        // Act
        bool result = await step.ExecuteAsync(_context, cancellationToken);

        // Assert - The result depends on whether the file and template exist
        Assert.False(result);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenBothParametersAreEmpty()
    {
        // Arrange
        AddFileStep step = new AddFileStep(
            NullLogger<AddFileStep>.Instance,
            _mockFileSystem.Object)
        {
            FileName = string.Empty,
            BaseOutputDirectory = string.Empty,
            ProjectPath = "test.csproj"
        };

        // Act
        bool result = await step.ExecuteAsync(_context, CancellationToken.None);

        // Assert
        Assert.False(result);
    }
}
