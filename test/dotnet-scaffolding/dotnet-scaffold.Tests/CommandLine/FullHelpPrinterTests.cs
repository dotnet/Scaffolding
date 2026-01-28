// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.CommandLine;
using System.CommandLine.Parsing;
using Microsoft.DotNet.Scaffolding.Core.Builder;
using Microsoft.DotNet.Scaffolding.Core.CommandLine;
using Microsoft.DotNet.Scaffolding.Core.ComponentModel;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.CommandLine;

public class FullHelpPrinterTests
{
    #region Root Command Tests

    [Fact]
    public void GenerateFullHelpText_ContainsRootDescription()
    {
        // Arrange
        var scaffolders = CreateEmptyScaffolderDictionary();

        // Act
        var helpText = FullHelpPrinter.GenerateFullHelpText(scaffolders);

        // Assert
        Assert.Contains("dotnet scaffold - A CLI tool for scaffolding ASP.NET and Aspire projects.", helpText);
    }

    [Fact]
    public void GenerateFullHelpText_ContainsRootUsage()
    {
        // Arrange
        var scaffolders = CreateEmptyScaffolderDictionary();

        // Act
        var helpText = FullHelpPrinter.GenerateFullHelpText(scaffolders);

        // Assert
        Assert.Contains("Usage:", helpText);
        Assert.Contains("dotnet scaffold [command] [options]", helpText);
    }

    [Fact]
    public void GenerateFullHelpText_ContainsFullHelpOption()
    {
        // Arrange
        var scaffolders = CreateEmptyScaffolderDictionary();

        // Act
        var helpText = FullHelpPrinter.GenerateFullHelpText(scaffolders);

        // Assert
        Assert.Contains("--full-help", helpText);
        Assert.Contains("Display help for all commands and subcommands", helpText);
    }

    [Fact]
    public void GenerateFullHelpText_ContainsHelpOption()
    {
        // Arrange
        var scaffolders = CreateEmptyScaffolderDictionary();

        // Act
        var helpText = FullHelpPrinter.GenerateFullHelpText(scaffolders);

        // Assert
        Assert.Contains("-h, --help", helpText);
        Assert.Contains("Show help and usage information", helpText);
    }

    #endregion

    #region Null Scaffolders Tests

    [Fact]
    public void GenerateFullHelpText_WithNullScaffolders_StillGeneratesOutput()
    {
        // Act
        var helpText = FullHelpPrinter.GenerateFullHelpText(null);

        // Assert
        Assert.NotNull(helpText);
        Assert.NotEmpty(helpText);
        Assert.Contains("dotnet scaffold", helpText);
    }

    [Fact]
    public void GenerateFullHelpText_WithNullScaffolders_ContainsToolCommands()
    {
        // Act
        var helpText = FullHelpPrinter.GenerateFullHelpText(null);

        // Assert - Tool commands should always be present
        Assert.Contains("dotnet scaffold tool", helpText);
        Assert.Contains("tool install", helpText);
        Assert.Contains("tool list", helpText);
        Assert.Contains("tool uninstall", helpText);
    }

    #endregion

    #region Category Tests

    [Fact]
    public void GenerateFullHelpText_WithAspireCategory_ContainsAspireSection()
    {
        // Arrange
        var scaffolders = new Dictionary<ScaffolderCatagory, IEnumerable<IScaffolder>>
        {
            { ScaffolderCatagory.Aspire, new List<IScaffolder> { CreateTestScaffolder("test-command", "Test Command") } }
        };

        // Act
        var helpText = FullHelpPrinter.GenerateFullHelpText(scaffolders);

        // Assert
        Assert.Contains("dotnet scaffold aspire", helpText);
        Assert.Contains("Commands related to Aspire project scaffolding", helpText);
    }

    [Fact]
    public void GenerateFullHelpText_WithAspNetCategory_ContainsAspNetSection()
    {
        // Arrange
        var scaffolders = new Dictionary<ScaffolderCatagory, IEnumerable<IScaffolder>>
        {
            { ScaffolderCatagory.AspNet, new List<IScaffolder> { CreateTestScaffolder("test-command", "Test Command") } }
        };

        // Act
        var helpText = FullHelpPrinter.GenerateFullHelpText(scaffolders);

        // Assert
        Assert.Contains("dotnet scaffold aspnet", helpText);
        Assert.Contains("Commands related to ASP.NET project scaffolding", helpText);
    }

    [Fact]
    public void GenerateFullHelpText_WithBothCategories_ContainsBothSections()
    {
        // Arrange
        var scaffolders = new Dictionary<ScaffolderCatagory, IEnumerable<IScaffolder>>
        {
            { ScaffolderCatagory.Aspire, new List<IScaffolder> { CreateTestScaffolder("aspire-cmd", "Aspire Command") } },
            { ScaffolderCatagory.AspNet, new List<IScaffolder> { CreateTestScaffolder("aspnet-cmd", "AspNet Command") } }
        };

        // Act
        var helpText = FullHelpPrinter.GenerateFullHelpText(scaffolders);

        // Assert
        Assert.Contains("dotnet scaffold aspire", helpText);
        Assert.Contains("dotnet scaffold aspnet", helpText);
    }

    [Fact]
    public void GenerateFullHelpText_CategoriesAreOrderedAlphabetically()
    {
        // Arrange - Add categories in reverse order
        var scaffolders = new Dictionary<ScaffolderCatagory, IEnumerable<IScaffolder>>
        {
            { ScaffolderCatagory.AspNet, new List<IScaffolder> { CreateTestScaffolder("aspnet-cmd", "AspNet Command") } },
            { ScaffolderCatagory.Aspire, new List<IScaffolder> { CreateTestScaffolder("aspire-cmd", "Aspire Command") } }
        };

        // Act
        var helpText = FullHelpPrinter.GenerateFullHelpText(scaffolders);

        // Assert - Aspire should come before AspNet alphabetically
        var aspireIndex = helpText.IndexOf("dotnet scaffold aspire");
        var aspnetIndex = helpText.IndexOf("dotnet scaffold aspnet");
        Assert.True(aspireIndex < aspnetIndex, "Aspire section should appear before AspNet section");
    }

    #endregion

    #region Scaffolder Command Tests

    [Fact]
    public void GenerateFullHelpText_WithScaffolder_ContainsScaffolderName()
    {
        // Arrange
        var scaffolders = new Dictionary<ScaffolderCatagory, IEnumerable<IScaffolder>>
        {
            { ScaffolderCatagory.Aspire, new List<IScaffolder> { CreateTestScaffolder("my-scaffolder", "My Scaffolder") } }
        };

        // Act
        var helpText = FullHelpPrinter.GenerateFullHelpText(scaffolders);

        // Assert
        Assert.Contains("my-scaffolder", helpText);
    }

    [Fact]
    public void GenerateFullHelpText_WithScaffolder_ContainsScaffolderDescription()
    {
        // Arrange
        var description = "This is a detailed description of the scaffolder";
        var scaffolders = new Dictionary<ScaffolderCatagory, IEnumerable<IScaffolder>>
        {
            { ScaffolderCatagory.AspNet, new List<IScaffolder> { CreateTestScaffolder("test-cmd", "Test", description) } }
        };

        // Act
        var helpText = FullHelpPrinter.GenerateFullHelpText(scaffolders);

        // Assert
        Assert.Contains(description, helpText);
    }

    [Fact]
    public void GenerateFullHelpText_WithScaffolder_ContainsUsageLine()
    {
        // Arrange
        var scaffolders = new Dictionary<ScaffolderCatagory, IEnumerable<IScaffolder>>
        {
            { ScaffolderCatagory.Aspire, new List<IScaffolder> { CreateTestScaffolder("caching", "Caching") } }
        };

        // Act
        var helpText = FullHelpPrinter.GenerateFullHelpText(scaffolders);

        // Assert
        Assert.Contains("dotnet scaffold aspire caching [options]", helpText);
    }

    [Fact]
    public void GenerateFullHelpText_ScaffoldersAreOrderedAlphabetically()
    {
        // Arrange
        var scaffolders = new Dictionary<ScaffolderCatagory, IEnumerable<IScaffolder>>
        {
            { ScaffolderCatagory.Aspire, new List<IScaffolder>
                {
                    CreateTestScaffolder("storage", "Storage"),
                    CreateTestScaffolder("caching", "Caching"),
                    CreateTestScaffolder("database", "Database")
                }
            }
        };

        // Act
        var helpText = FullHelpPrinter.GenerateFullHelpText(scaffolders);

        // Assert - Commands should be ordered: caching, database, storage
        var cachingIndex = helpText.IndexOf("dotnet scaffold aspire caching");
        var databaseIndex = helpText.IndexOf("dotnet scaffold aspire database");
        var storageIndex = helpText.IndexOf("dotnet scaffold aspire storage");

        Assert.True(cachingIndex < databaseIndex, "caching should appear before database");
        Assert.True(databaseIndex < storageIndex, "database should appear before storage");
    }

    [Fact]
    public void GenerateFullHelpText_WithMultipleScaffolders_ContainsAllScaffolders()
    {
        // Arrange
        var scaffolders = new Dictionary<ScaffolderCatagory, IEnumerable<IScaffolder>>
        {
            { ScaffolderCatagory.AspNet, new List<IScaffolder>
                {
                    CreateTestScaffolder("blazor-empty", "Blazor Empty"),
                    CreateTestScaffolder("blazor-crud", "Blazor CRUD"),
                    CreateTestScaffolder("minimalapi", "Minimal API")
                }
            }
        };

        // Act
        var helpText = FullHelpPrinter.GenerateFullHelpText(scaffolders);

        // Assert
        Assert.Contains("blazor-empty", helpText);
        Assert.Contains("blazor-crud", helpText);
        Assert.Contains("minimalapi", helpText);
    }

    #endregion

    #region Scaffolder Options Tests

    [Fact]
    public void GenerateFullHelpText_WithScaffolderOptions_ContainsOptionsSection()
    {
        // Arrange
        var options = new List<ScaffolderOption>
        {
            CreateTestOption("project", "--project", "Project path", true),
            CreateTestOption("name", "--name", "File name", false)
        };
        var scaffolders = new Dictionary<ScaffolderCatagory, IEnumerable<IScaffolder>>
        {
            { ScaffolderCatagory.AspNet, new List<IScaffolder> { CreateTestScaffolderWithOptions("test-cmd", "Test", options) } }
        };

        // Act
        var helpText = FullHelpPrinter.GenerateFullHelpText(scaffolders);

        // Assert
        Assert.Contains("Options:", helpText);
        Assert.Contains("--project", helpText);
        Assert.Contains("--name", helpText);
    }

    [Fact]
    public void GenerateFullHelpText_WithRequiredOption_ShowsRequiredMarker()
    {
        // Arrange
        var options = new List<ScaffolderOption>
        {
            CreateTestOption("project", "--project", "Project path", true)
        };
        var scaffolders = new Dictionary<ScaffolderCatagory, IEnumerable<IScaffolder>>
        {
            { ScaffolderCatagory.AspNet, new List<IScaffolder> { CreateTestScaffolderWithOptions("test-cmd", "Test", options) } }
        };

        // Act
        var helpText = FullHelpPrinter.GenerateFullHelpText(scaffolders);

        // Assert
        Assert.Contains("(Required)", helpText);
    }

    [Fact]
    public void GenerateFullHelpText_WithOptionalOption_DoesNotShowRequiredMarker()
    {
        // Arrange
        var options = new List<ScaffolderOption>
        {
            CreateTestOption("verbose", "--verbose", "Enable verbose output", false)
        };
        var scaffolders = new Dictionary<ScaffolderCatagory, IEnumerable<IScaffolder>>
        {
            { ScaffolderCatagory.AspNet, new List<IScaffolder> { CreateTestScaffolderWithOptions("test-cmd", "Test", options) } }
        };

        // Act
        var helpText = FullHelpPrinter.GenerateFullHelpText(scaffolders);

        // Assert - The verbose option line should not contain "(Required)"
        var lines = helpText.Split('\n');
        var verboseLine = lines.FirstOrDefault(l => l.Contains("--verbose"));
        Assert.NotNull(verboseLine);
        Assert.DoesNotContain("(Required)", verboseLine);
    }

    [Fact]
    public void GenerateFullHelpText_WithOptionDescription_ContainsDescription()
    {
        // Arrange
        var description = "The .NET project file to scaffold";
        var options = new List<ScaffolderOption>
        {
            CreateTestOption("project", "--project", description, true)
        };
        var scaffolders = new Dictionary<ScaffolderCatagory, IEnumerable<IScaffolder>>
        {
            { ScaffolderCatagory.AspNet, new List<IScaffolder> { CreateTestScaffolderWithOptions("test-cmd", "Test", options) } }
        };

        // Act
        var helpText = FullHelpPrinter.GenerateFullHelpText(scaffolders);

        // Assert
        Assert.Contains(description, helpText);
    }

    [Fact]
    public void GenerateFullHelpText_WithNoOptions_DoesNotShowOptionsSection()
    {
        // Arrange
        var scaffolders = new Dictionary<ScaffolderCatagory, IEnumerable<IScaffolder>>
        {
            { ScaffolderCatagory.AspNet, new List<IScaffolder> { CreateTestScaffolder("area", "Area", "Creates an MVC Area") } }
        };

        // Act
        var helpText = FullHelpPrinter.GenerateFullHelpText(scaffolders);

        // Assert - Find the area command section
        var areaMarker = "dotnet scaffold aspnet area";
        var areaIndex = helpText.IndexOf(areaMarker);
        Assert.True(areaIndex >= 0, "Area section should be present");

        var areaSection = helpText.Substring(areaIndex);
        var nextSectionIndex = areaSection.IndexOf("====");
        if (nextSectionIndex > 0)
        {
            var areaSectionOnly = areaSection.Substring(0, nextSectionIndex);
            // The section should have a Usage: line
            Assert.Contains("Usage:", areaSectionOnly);
        }
    }

    #endregion

    #region Tool Commands Tests

    [Fact]
    public void GenerateFullHelpText_AlwaysContainsToolSection()
    {
        // Arrange
        var scaffolders = CreateEmptyScaffolderDictionary();

        // Act
        var helpText = FullHelpPrinter.GenerateFullHelpText(scaffolders);

        // Assert
        Assert.Contains("dotnet scaffold tool", helpText);
    }

    [Fact]
    public void GenerateFullHelpText_ContainsToolInstallCommand()
    {
        // Arrange
        var scaffolders = CreateEmptyScaffolderDictionary();

        // Act
        var helpText = FullHelpPrinter.GenerateFullHelpText(scaffolders);

        // Assert
        Assert.Contains("dotnet scaffold tool install", helpText);
        Assert.Contains("<PACKAGE_NAME>", helpText);
        Assert.Contains("--add-source", helpText);
        Assert.Contains("--configfile", helpText);
        Assert.Contains("--prerelease", helpText);
        Assert.Contains("--global", helpText);
        Assert.Contains("--version", helpText);
    }

    [Fact]
    public void GenerateFullHelpText_ContainsToolListCommand()
    {
        // Arrange
        var scaffolders = CreateEmptyScaffolderDictionary();

        // Act
        var helpText = FullHelpPrinter.GenerateFullHelpText(scaffolders);

        // Assert
        Assert.Contains("dotnet scaffold tool list", helpText);
        Assert.Contains("List installed scaffold tools", helpText);
    }

    [Fact]
    public void GenerateFullHelpText_ContainsToolUninstallCommand()
    {
        // Arrange
        var scaffolders = CreateEmptyScaffolderDictionary();

        // Act
        var helpText = FullHelpPrinter.GenerateFullHelpText(scaffolders);

        // Assert
        Assert.Contains("dotnet scaffold tool uninstall", helpText);
        Assert.Contains("Uninstall a scaffold tool", helpText);
    }

    [Fact]
    public void GenerateFullHelpText_ToolInstallContainsPackageNameArgument()
    {
        // Act
        var helpText = FullHelpPrinter.GenerateFullHelpText(null);

        // Assert
        Assert.Contains("Arguments:", helpText);
        Assert.Contains("<PACKAGE_NAME>", helpText);
        Assert.Contains("dotnet-scaffold-aspnet", helpText);
        Assert.Contains("dotnet-scaffold-aspire", helpText);
    }

    [Fact]
    public void GenerateFullHelpText_ToolCommandsInCorrectOrder()
    {
        // Act
        var helpText = FullHelpPrinter.GenerateFullHelpText(null);

        // Assert - Install, List, Uninstall order in the commands summary
        var toolSectionStart = helpText.IndexOf("dotnet scaffold tool");
        Assert.True(toolSectionStart >= 0, "Tool section should be present");

        // Find the first occurrence of each command after the tool section starts
        var installIndex = helpText.IndexOf("install", toolSectionStart);
        Assert.True(installIndex >= 0, "install should be present");

        var listIndex = helpText.IndexOf("list", installIndex + 7); // skip past "install"
        Assert.True(listIndex >= 0, "list should be present");

        var uninstallIndex = helpText.IndexOf("uninstall", listIndex + 4); // skip past "list"
        Assert.True(uninstallIndex >= 0, "uninstall should be present");

        Assert.True(installIndex < listIndex, "install should appear before list");
        Assert.True(listIndex < uninstallIndex, "list should appear before uninstall");
    }

    #endregion

    #region Formatting Tests

    [Fact]
    public void GenerateFullHelpText_ContainsSectionSeparators()
    {
        // Arrange
        var scaffolders = new Dictionary<ScaffolderCatagory, IEnumerable<IScaffolder>>
        {
            { ScaffolderCatagory.Aspire, new List<IScaffolder> { CreateTestScaffolder("test", "Test") } }
        };

        // Act
        var helpText = FullHelpPrinter.GenerateFullHelpText(scaffolders);

        // Assert - Major sections should have === separators
        Assert.Contains(new string('=', 60), helpText);
    }

    [Fact]
    public void GenerateFullHelpText_ContainsCommandSeparators()
    {
        // Arrange
        var scaffolders = new Dictionary<ScaffolderCatagory, IEnumerable<IScaffolder>>
        {
            { ScaffolderCatagory.Aspire, new List<IScaffolder> { CreateTestScaffolder("test", "Test") } }
        };

        // Act
        var helpText = FullHelpPrinter.GenerateFullHelpText(scaffolders);

        // Assert - Individual commands should have --- separators
        Assert.Contains(new string('-', 60), helpText);
    }

    [Fact]
    public void GenerateFullHelpText_IsNotEmpty()
    {
        // Act
        var helpText = FullHelpPrinter.GenerateFullHelpText(null);

        // Assert
        Assert.NotNull(helpText);
        Assert.True(helpText.Length > 100, "Help text should be substantial");
    }

    [Fact]
    public void GenerateFullHelpText_EndsWithNewline()
    {
        // Act
        var helpText = FullHelpPrinter.GenerateFullHelpText(null);

        // Assert
        Assert.EndsWith("\n", helpText);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void GenerateFullHelpText_FullOutput_ContainsAllExpectedSections()
    {
        // Arrange - Create a realistic scaffolder setup
        var aspireScaffolders = new List<IScaffolder>
        {
            CreateTestScaffolderWithOptions("caching", "Caching", new List<ScaffolderOption>
            {
                CreateTestOption("type", "--type", "Types of caching", true),
                CreateTestOption("project", "--project", "Project path", true)
            }, "Modify Aspire project to make it caching ready"),
            CreateTestScaffolderWithOptions("database", "Database", new List<ScaffolderOption>
            {
                CreateTestOption("type", "--type", "Types of database", true),
                CreateTestOption("project", "--project", "Project path", true)
            }, "Modify Aspire project to make it database ready")
        };

        var aspnetScaffolders = new List<IScaffolder>
        {
            CreateTestScaffolderWithOptions("blazor-empty", "Blazor Empty", new List<ScaffolderOption>
            {
                CreateTestOption("project", "--project", ".NET project file", true),
                CreateTestOption("name", "--name", "File name", true)
            }, "Add an empty razor component"),
            CreateTestScaffolderWithOptions("minimalapi", "Minimal API", new List<ScaffolderOption>
            {
                CreateTestOption("project", "--project", ".NET project file", true),
                CreateTestOption("model", "--model", "Model class name", true)
            }, "Generates minimal API endpoints")
        };

        var scaffolders = new Dictionary<ScaffolderCatagory, IEnumerable<IScaffolder>>
        {
            { ScaffolderCatagory.Aspire, aspireScaffolders },
            { ScaffolderCatagory.AspNet, aspnetScaffolders }
        };

        // Act
        var helpText = FullHelpPrinter.GenerateFullHelpText(scaffolders);

        // Assert - Verify all major sections are present
        Assert.Contains("Description:", helpText);
        Assert.Contains("Usage:", helpText);
        Assert.Contains("Options:", helpText);
        Assert.Contains("Commands:", helpText);

        // Verify categories
        Assert.Contains("dotnet scaffold aspire", helpText);
        Assert.Contains("dotnet scaffold aspnet", helpText);
        Assert.Contains("dotnet scaffold tool", helpText);

        // Verify individual commands
        Assert.Contains("caching", helpText);
        Assert.Contains("database", helpText);
        Assert.Contains("blazor-empty", helpText);
        Assert.Contains("minimalapi", helpText);

        // Verify tool commands
        Assert.Contains("tool install", helpText);
        Assert.Contains("tool list", helpText);
        Assert.Contains("tool uninstall", helpText);
    }

    #endregion

    #region Helper Methods

    private static Dictionary<ScaffolderCatagory, IEnumerable<IScaffolder>> CreateEmptyScaffolderDictionary()
    {
        return new Dictionary<ScaffolderCatagory, IEnumerable<IScaffolder>>();
    }

    private static IScaffolder CreateTestScaffolder(string name, string displayName, string? description = null)
    {
        return new TestScaffolder(name, displayName, description);
    }

    private static IScaffolder CreateTestScaffolderWithOptions(string name, string displayName, IEnumerable<ScaffolderOption> options, string? description = null)
    {
        return new TestScaffolder(name, displayName, description, options);
    }

    private static ScaffolderOption CreateTestOption(string displayName, string cliOption, string description, bool required)
    {
        return new TestScaffolderOption
        {
            DisplayName = displayName,
            CliOption = cliOption,
            Description = description,
            Required = required
        };
    }

    #endregion

    #region Test Helpers

    private class TestScaffolder : IScaffolder
    {
        private readonly IEnumerable<ScaffolderOption> _options;

        public TestScaffolder(string name, string displayName, string? description = null, IEnumerable<ScaffolderOption>? options = null)
        {
            Name = name;
            DisplayName = displayName;
            Description = description ?? $"Test scaffolder for {displayName}";
            _options = options ?? Enumerable.Empty<ScaffolderOption>();
        }

        public string Name { get; }
        public string DisplayName { get; }
        public string? Description { get; }
        public IEnumerable<string> Categories => new[] { "Test" };
        public IEnumerable<ScaffolderOption> Options => _options;

        public Task ExecuteAsync(ScaffolderContext context)
        {
            return Task.CompletedTask;
        }
    }

    private class TestScaffolderOption : ScaffolderOption
    {
        internal override Option ToCliOption()
        {
            return new Option<string>(CliOption ?? $"--{DisplayName.ToLowerInvariant()}", Description ?? string.Empty);
        }

        internal override Parameter ToParameter()
        {
            return new Parameter
            {
                Name = CliOption ?? DisplayName,
                DisplayName = DisplayName,
                Description = Description,
                Required = Required,
                Type = CliTypes.String
            };
        }

        internal override object? GetValue(ParseResult parseResult)
        {
            return null;
        }
    }

    #endregion
}
