// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.ScaffoldSteps;

public class IdentityDatabaseGuidanceStepTests
{
    [Fact]
    public async Task ExecuteAsync_ExplainsExplicitDatabaseWorkflow()
    {
        var logger = new GuidanceLogger();
        var step = new IdentityDatabaseGuidanceStep(logger)
        {
            ProjectPath = "My App.csproj",
            DbContextName = "ApplicationDbContext"
        };

        Assert.True(await step.ExecuteAsync(new ScaffolderContext(Mock.Of<IScaffolder>())));
        foreach (var message in new[]
        {
            "Identity scaffolding does not create migrations or update the database.",
            "dotnet ef migrations add AddIdentity --project \"My App.csproj\" --context \"ApplicationDbContext\"",
            "Apply migrations explicitly using your normal database deployment workflow."
        })
        {
            Assert.Contains((LogLevel.Information, message), logger.Messages);
        }
    }

    [Theory]
    [InlineData("", "ApplicationDbContext")]
    [InlineData("MyApp.csproj", "")]
    public async Task ExecuteAsync_ReportsMissingConfiguration(string project, string dbContext)
    {
        var logger = new GuidanceLogger();
        var step = new IdentityDatabaseGuidanceStep(logger)
        {
            ProjectPath = project,
            DbContextName = dbContext
        };
        Assert.False(await step.ExecuteAsync(new ScaffolderContext(Mock.Of<IScaffolder>())));
        Assert.Equal(LogLevel.Error, Assert.Single(logger.Messages).Level);
    }

    private sealed class GuidanceLogger : ILogger<IdentityDatabaseGuidanceStep>
    {
        public List<(LogLevel Level, string Message)> Messages { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
            => Messages.Add((logLevel, formatter(state, exception)));
    }
}
