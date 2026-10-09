// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.TextTemplating;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.TextTemplating;

public class TextTemplatingStepTests
{
    private readonly ScaffolderContext _context;

    public TextTemplatingStepTests()
    {
        var mockScaffolder = new Mock<IScaffolder>();
        mockScaffolder.Setup(s => s.DisplayName).Returns("TestScaffolder");
        mockScaffolder.Setup(s => s.Name).Returns("test-scaffolder");
        _context = new ScaffolderContext(mockScaffolder.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenTemplateTypeDoesNotImplementTransformation()
    {
        string outputPath = GetTempOutputPath();
        try
        {
            var step = CreateStep(typeof(object), outputPath);

            bool result = await step.ExecuteAsync(_context, CancellationToken.None);

            Assert.False(result);
            Assert.False(File.Exists(outputPath));
        }
        finally
        {
            DeleteIfExists(outputPath);
        }
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenTemplateProcessingReportsCompilerErrors()
    {
        string outputPath = GetTempOutputPath();
        try
        {
            var step = CreateStep(typeof(ErrorTransformation), outputPath);

            bool result = await step.ExecuteAsync(_context, CancellationToken.None);

            Assert.False(result);
            Assert.False(File.Exists(outputPath));
        }
        finally
        {
            DeleteIfExists(outputPath);
        }
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenTemplateOutputIsEmpty()
    {
        string outputPath = GetTempOutputPath();
        try
        {
            var step = CreateStep(typeof(EmptyOutputTransformation), outputPath);

            bool result = await step.ExecuteAsync(_context, CancellationToken.None);

            Assert.False(result);
            Assert.False(File.Exists(outputPath));
        }
        finally
        {
            DeleteIfExists(outputPath);
        }
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFalse_WhenTemplateOutputIsNull()
    {
        string outputPath = GetTempOutputPath();
        try
        {
            var step = CreateStep(typeof(NullOutputTransformation), outputPath);

            bool result = await step.ExecuteAsync(_context, CancellationToken.None);

            Assert.False(result);
            Assert.False(File.Exists(outputPath));
        }
        finally
        {
            DeleteIfExists(outputPath);
        }
    }

    [Fact]
    public async Task ExecuteAsync_WritesExpectedContent_WhenTemplateSucceeds()
    {
        string outputPath = GetTempOutputPath();
        var step = CreateStep(typeof(SuccessfulTransformation), outputPath);
        try
        {
            bool result = await step.ExecuteAsync(_context, CancellationToken.None);

            Assert.True(result);
            Assert.True(File.Exists(outputPath));
            Assert.Equal("generated-content", File.ReadAllText(outputPath));
        }
        finally
        {
            DeleteIfExists(outputPath);
        }
    }

    private static TextTemplatingStep CreateStep(Type templateType, string outputPath)
    {
        return new TextTemplatingStep(NullLogger<TextTemplatingStep>.Instance)
        {
            TextTemplatingProperties =
            [
                new TextTemplatingProperty
                {
                    TemplatePath = "Employee.tt",
                    TemplateType = templateType,
                    OutputPath = outputPath,
                    TemplateModelName = "Model",
                    TemplateModel = new object()
                }
            ]
        };
    }

    private static string GetTempOutputPath()
    {
        string outputDirectory = Path.Combine(Path.GetTempPath(), nameof(TextTemplatingStepTests), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outputDirectory);
        return Path.Combine(outputDirectory, "Employee.cs");
    }

    private static void DeleteIfExists(string outputPath)
    {
        string? directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private sealed class SuccessfulTransformation : ITextTransformation
    {
        public IDictionary<string, object> Session { get; set; } = new Dictionary<string, object>();
        public CompilerErrorCollection Errors { get; } = new CompilerErrorCollection();

        public void Initialize()
        {
        }

        public string TransformText()
        {
            return "generated-content";
        }
    }

    private sealed class EmptyOutputTransformation : ITextTransformation
    {
        public IDictionary<string, object> Session { get; set; } = new Dictionary<string, object>();
        public CompilerErrorCollection Errors { get; } = new CompilerErrorCollection();

        public void Initialize()
        {
        }

        public string TransformText()
        {
            return string.Empty;
        }
    }

    private sealed class NullOutputTransformation : ITextTransformation
    {
        public IDictionary<string, object> Session { get; set; } = new Dictionary<string, object>();
        public CompilerErrorCollection Errors { get; } = new CompilerErrorCollection();

        public void Initialize()
        {
        }

        public string TransformText()
        {
            return null!;
        }
    }

    private sealed class ErrorTransformation : ITextTransformation
    {
        public IDictionary<string, object> Session { get; set; } = new Dictionary<string, object>();
        public CompilerErrorCollection Errors { get; } = new CompilerErrorCollection();

        public ErrorTransformation()
        {
            Errors.Add(new CompilerError { ErrorText = "Template failed." });
        }

        public void Initialize()
        {
        }

        public string TransformText()
        {
            return "generated-content";
        }
    }
}
