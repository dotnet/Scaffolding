// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Scaffolding.Internal.Telemetry;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Common;
using Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.AspNet.ScaffoldSteps;

public class ValidateEfControllerStepTests
{
    private readonly Mock<IFileSystem> _fileSystem = new();
    private readonly Mock<ILogger<ValidateEfControllerStep>> _logger = new();
    private readonly Mock<ITelemetryService> _telemetry = new();
    private readonly string _projectPath = Path.Combine("test", "TestProject.csproj");

    [Fact]
    public void ValidateSettings_FormattingCharacter_IsRejectedBeforeDerivingViewFolder()
    {
        const string controllerName = "Movie\u200CController";
        Assert.True(SyntaxFacts.IsValidIdentifier(controllerName));
        Assert.Equal("MovieController", SyntaxFactory.ParseToken(controllerName).ValueText);

        var step = CreateStep(controllerName);

        Assert.Null(step.ValidateEfControllerSettings(addViews: true));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(".")]
    [InlineData("..")]
    [InlineData(".cs")]
    [InlineData("../MoviesController")]
    [InlineData(@"..\MoviesController")]
    [InlineData("Controllers/MoviesController.cs")]
    [InlineData(@"Controllers\MoviesController.cs")]
    [InlineData("/MoviesController")]
    [InlineData(@"C:\Controllers\MoviesController.cs")]
    [InlineData("MoviesController:stream")]
    [InlineData("Movies Controller")]
    [InlineData("123MoviesController")]
    [InlineData("Movies-Controller")]
    [InlineData("Movies.Controller")]
    [InlineData("MoviesController.dll")]
    [InlineData("MoviesController.cs.cs")]
    [InlineData("MoviesController\0")]
    [InlineData("Movie\u200CController")]
    [InlineData("Movie\u200CController.cs")]
    [InlineData("Movie\u200CController.CS")]
    [InlineData("MovieCon\u200Ctroller")]
    [InlineData("Movie\u200DController")]
    [InlineData("Movie\uFEFFController")]
    [InlineData("class")]
    [InlineData("namespace")]
    [InlineData("@MoviesController")]
    [InlineData("Controller")]
    [InlineData("controller.cs")]
    [InlineData("CON")]
    [InlineData("ConController")]
    [InlineData("PRNController")]
    [InlineData("AUXController")]
    [InlineData("NULController")]
    [InlineData("COM1Controller")]
    [InlineData("Lpt9Controller")]
    [InlineData("COM\u00b9Controller")]
    [InlineData("LPT\u00b3Controller")]
    public async Task ExecuteAsync_InvalidControllerName_FailsBeforeInitializingModels(string? controllerName)
    {
        var step = CreateStep(controllerName);
        var context = new ScaffolderContext(Mock.Of<IScaffolder>());
        context.OptionResults[new ScaffolderOption<bool> { DisplayName = "Views", CliOption = Constants.CliOptions.ViewsOption }] = true;

        Assert.False(await step.ExecuteAsync(context));

        Assert.Empty(context.Properties);
        Assert.Contains(_logger.Invocations, invocation =>
            invocation.Method.Name == nameof(ILogger.Log) &&
            Equals(invocation.Arguments[0], LogLevel.Error) &&
            invocation.Arguments[2].ToString()!.Contains(Constants.CliOptions.ControllerNameOption, StringComparison.Ordinal));
        Assert.Single(_telemetry.Invocations);
        Assert.Equal(nameof(ITelemetryService.TrackEvent), _telemetry.Invocations[0].Method.Name);
        _fileSystem.Verify(fs => fs.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData("MovieController", "MovieController", "MVC")]
    [InlineData("MoviesController", "MoviesController", "MVC")]
    [InlineData("CatalogController", "CatalogController", "MVC")]
    [InlineData("PeopleController", "PeopleController", "MVC")]
    [InlineData("Moviescontroller", "Moviescontroller", "MVC")]
    [InlineData("MoviesController.cs", "MoviesController", "MVC")]
    [InlineData("MoviesController.CS", "MoviesController", "MVC")]
    [InlineData("Movies", "Movies", "MVC")]
    [InlineData("_MoviesController", "_MoviesController", "MVC")]
    [InlineData("Movies2Controller", "Movies2Controller", "MVC")]
    [InlineData("MovieControllerController", "MovieControllerController", "MVC")]
    [InlineData("ControllerMoviesController", "ControllerMoviesController", "MVC")]
    [InlineData("COM10Controller", "COM10Controller", "MVC")]
    [InlineData("M\u00F6vieController", "M\u00F6vieController", "MVC")]
    [InlineData("\u7535\u5F71Controller", "\u7535\u5F71Controller", "MVC")]
    [InlineData("MoviesController", "MoviesController", "API")]
    [InlineData("MoviesController.cs", "MoviesController", "API")]
    [InlineData("ConController", "ConController", "API")]
    [InlineData("NULController", "NULController", "API")]
    public void ValidateSettings_ValidControllerName_PreservesExplicitName(string controllerName, string expectedName, string controllerType)
    {
        var step = CreateStep(controllerName);
        step.ControllerType = controllerType;

        var settings = step.ValidateEfControllerSettings(addViews: controllerType == "MVC");

        Assert.NotNull(settings);
        Assert.Equal(expectedName, settings.ControllerName);
        Assert.Equal(expectedName, step.ControllerName);
        Assert.Equal(controllerType, settings.ControllerType);
        Assert.Equal("Movie", settings.Model);
    }

    [Theory]
    [InlineData("ConController")]
    [InlineData("ConController.cs")]
    [InlineData("NULController")]
    [InlineData("PRNController")]
    [InlineData("AUXController")]
    [InlineData("COM1Controller")]
    [InlineData("Lpt9Controller")]
    public void ValidateSettings_ViewFolderRestrictions_ApplyOnlyWhenGeneratingViews(string controllerName)
    {
        var apiStep = CreateStep(controllerName);
        apiStep.ControllerType = "API";
        Assert.NotNull(apiStep.ValidateEfControllerSettings(addViews: false));

        var mvcStep = CreateStep(controllerName);
        Assert.NotNull(mvcStep.ValidateEfControllerSettings(addViews: false));
        Assert.Null(mvcStep.ValidateEfControllerSettings(addViews: true));
    }

    [Theory]
    [InlineData("CON")]
    [InlineData("NUL.cs")]
    [InlineData("COM1")]
    [InlineData("../MoviesController")]
    [InlineData(@"..\MoviesController")]
    [InlineData("MoviesController:stream")]
    [InlineData("Movies-Controller")]
    [InlineData("class")]
    [InlineData("COM\u00b9Controller")]
    [InlineData("Movie\u200CController")]
    [InlineData("Movie\u200CController.cs")]
    [InlineData("Co\u200CnController")]
    [InlineData("NU\u200CL.cs")]
    public void ValidateSettings_InvalidControllerFileName_IsRejectedWithOrWithoutViews(string controllerName)
    {
        var apiStep = CreateStep(controllerName);
        apiStep.ControllerType = "API";
        Assert.Null(apiStep.ValidateEfControllerSettings(addViews: false));

        var mvcStep = CreateStep(controllerName);
        Assert.Null(mvcStep.ValidateEfControllerSettings(addViews: false));
        Assert.Null(mvcStep.ValidateEfControllerSettings(addViews: true));
    }

    private ValidateEfControllerStep CreateStep(string? controllerName)
    {
        _fileSystem.Setup(fs => fs.FileExists(_projectPath)).Returns(true);
        return new ValidateEfControllerStep(_fileSystem.Object, _logger.Object, _telemetry.Object)
        {
            Project = _projectPath,
            Model = "Movie",
            ControllerName = controllerName,
            ControllerType = "MVC",
            DataContext = "ApplicationDbContext",
            DatabaseProvider = PackageConstants.EfConstants.SQLite
        };
    }
}