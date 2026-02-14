// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.Scaffolding.Core.ComponentModel;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.Aspire;

/// <summary>
/// Tests for filtering Aspire scaffolders from display when not available (e.g., .NET 8 projects).
/// </summary>
public class AspireAvailabilityFilterTests
{
    [Fact]
    public void IsCommandAnAspireCommand_WithAspireCategory_ReturnsTrue()
    {
        // Arrange
        var commandInfo = new CommandInfo
        {
            Name = "database",
            DisplayName = "Database",
            DisplayCategories = new List<string> { "Aspire", "All" },
            Parameters = []
        };

        // Act
        bool result = commandInfo.IsCommandAnAspireCommand();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsCommandAnAspireCommand_WithoutAspireCategory_ReturnsFalse()
    {
        // Arrange
        var commandInfo = new CommandInfo
        {
            Name = "minimal-api",
            DisplayName = "Minimal API",
            DisplayCategories = new List<string> { "API", "All" },
            Parameters = []
        };

        // Act
        bool result = commandInfo.IsCommandAnAspireCommand();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsCommandAnAspireCommand_WithBlazorCategory_ReturnsFalse()
    {
        // Arrange
        var commandInfo = new CommandInfo
        {
            Name = "blazor-crud",
            DisplayName = "Blazor CRUD",
            DisplayCategories = new List<string> { "Blazor", "All" },
            Parameters = []
        };

        // Act
        bool result = commandInfo.IsCommandAnAspireCommand();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void FilterCommands_WhenAspireNotAvailable_ExcludesAspireCommands()
    {
        // Arrange
        var allCommands = new List<KeyValuePair<string, CommandInfo>>
        {
            new("dotnet-scaffold-aspire", new CommandInfo
            {
                Name = "database",
                DisplayName = "Database",
                DisplayCategories = new List<string> { "Aspire", "All" },
                Parameters = []
            }),
            new("dotnet-scaffold-aspire", new CommandInfo
            {
                Name = "caching",
                DisplayName = "Caching",
                DisplayCategories = new List<string> { "Aspire", "All" },
                Parameters = []
            }),
            new("dotnet-scaffold-aspnet", new CommandInfo
            {
                Name = "minimal-api",
                DisplayName = "Minimal API",
                DisplayCategories = new List<string> { "API", "All" },
                Parameters = []
            }),
            new("dotnet-scaffold-aspnet", new CommandInfo
            {
                Name = "blazor-crud",
                DisplayName = "Blazor CRUD",
                DisplayCategories = new List<string> { "Blazor", "All" },
                Parameters = []
            })
        };

        // Act - simulate filtering when Aspire is not available
        bool isAspireAvailable = false;
        var filteredCommands = isAspireAvailable
            ? allCommands
            : allCommands.Where(x => !x.Value.IsCommandAnAspireCommand()).ToList();

        // Assert
        Assert.Equal(2, filteredCommands.Count);
        Assert.All(filteredCommands, c => Assert.False(c.Value.IsCommandAnAspireCommand()));
        Assert.Contains(filteredCommands, c => c.Value.Name == "minimal-api");
        Assert.Contains(filteredCommands, c => c.Value.Name == "blazor-crud");
        Assert.DoesNotContain(filteredCommands, c => c.Value.Name == "database");
        Assert.DoesNotContain(filteredCommands, c => c.Value.Name == "caching");
    }

    [Fact]
    public void FilterCommands_WhenAspireAvailable_IncludesAllCommands()
    {
        // Arrange
        var allCommands = new List<KeyValuePair<string, CommandInfo>>
        {
            new("dotnet-scaffold-aspire", new CommandInfo
            {
                Name = "database",
                DisplayName = "Database",
                DisplayCategories = new List<string> { "Aspire", "All" },
                Parameters = []
            }),
            new("dotnet-scaffold-aspnet", new CommandInfo
            {
                Name = "minimal-api",
                DisplayName = "Minimal API",
                DisplayCategories = new List<string> { "API", "All" },
                Parameters = []
            })
        };

        // Act - simulate no filtering when Aspire is available
        bool isAspireAvailable = true;
        var filteredCommands = isAspireAvailable
            ? allCommands
            : allCommands.Where(x => !x.Value.IsCommandAnAspireCommand()).ToList();

        // Assert
        Assert.Equal(2, filteredCommands.Count);
        Assert.Contains(filteredCommands, c => c.Value.Name == "database");
        Assert.Contains(filteredCommands, c => c.Value.Name == "minimal-api");
    }

    [Fact]
    public void FilterCategories_WhenAspireNotAvailable_ExcludesAspireCategory()
    {
        // Arrange
        var displayCategories = new List<string> { "API", "Aspire", "Blazor", "Identity", "MVC", "Razor Pages", "All" };

        // Act - simulate filtering when Aspire is not available
        bool isAspireAvailable = false;
        if (!isAspireAvailable)
        {
            displayCategories.Remove("Aspire");
        }

        // Assert
        Assert.DoesNotContain("Aspire", displayCategories);
        Assert.Contains("API", displayCategories);
        Assert.Contains("Blazor", displayCategories);
        Assert.Contains("All", displayCategories);
    }

    [Fact]
    public void FilterCategories_WhenAspireAvailable_IncludesAspireCategory()
    {
        // Arrange
        var displayCategories = new List<string> { "API", "Aspire", "Blazor", "Identity", "MVC", "Razor Pages", "All" };

        // Act - no filtering when Aspire is available
        bool isAspireAvailable = true;
        if (!isAspireAvailable)
        {
            displayCategories.Remove("Aspire");
        }

        // Assert
        Assert.Contains("Aspire", displayCategories);
        Assert.Contains("API", displayCategories);
        Assert.Contains("All", displayCategories);
    }
}
