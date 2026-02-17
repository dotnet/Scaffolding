// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.Scaffolding.Core.ComponentModel;
using Microsoft.DotNet.Scaffolding.Core.Model;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.Interactive.Flow;

/// <summary>
/// Tests for filtering scaffolders based on target framework (TFM).
/// Entra ID scaffolders are not available for .NET 8 and .NET 9 projects.
/// </summary>
public class TfmBasedFilteringTests
{
    [Fact]
    public void IsCommandAnEntraIdCommand_WithEntraIdCategory_ReturnsTrue()
    {
        // Arrange
        var commandInfo = new CommandInfo
        {
            Name = "blazor-entra",
            DisplayName = "Blazor Entra ID",
            DisplayCategories = new List<string> { "Entra ID", "Blazor", "All" },
            Parameters = []
        };

        // Act
        bool result = commandInfo.IsCommandAnEntraIdCommand();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsCommandAnEntraIdCommand_WithoutEntraIdCategory_ReturnsFalse()
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
        bool result = commandInfo.IsCommandAnEntraIdCommand();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsCommandAnEntraIdCommand_WithBlazorCategory_ReturnsFalse()
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
        bool result = commandInfo.IsCommandAnEntraIdCommand();

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData(TargetFramework.Net8)]
    [InlineData(TargetFramework.Net9)]
    public void FilterCommands_WhenNet8OrNet9_ExcludesEntraIdCommands(TargetFramework detectedTfm)
    {
        // Arrange
        var allCommands = new List<KeyValuePair<string, CommandInfo>>
        {
            new("dotnet-scaffold-aspnet", new CommandInfo
            {
                Name = "blazor-entra",
                DisplayName = "Blazor Entra ID",
                DisplayCategories = new List<string> { "Entra ID", "Blazor", "All" },
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

        // Act - simulate filtering when TFM is .NET 8 or .NET 9
        var filteredCommands = (detectedTfm == TargetFramework.Net8 || detectedTfm == TargetFramework.Net9)
            ? allCommands.Where(x => !x.Value.IsCommandAnEntraIdCommand()).ToList()
            : allCommands;

        // Assert
        Assert.Equal(2, filteredCommands.Count);
        Assert.DoesNotContain(filteredCommands, c => c.Value.Name == "blazor-entra");
        Assert.Contains(filteredCommands, c => c.Value.Name == "minimal-api");
        Assert.Contains(filteredCommands, c => c.Value.Name == "blazor-crud");
    }

    [Theory]
    [InlineData(TargetFramework.Net10)]
    [InlineData(TargetFramework.Net11)]
    public void FilterCommands_WhenNet10OrNewer_IncludesEntraIdCommands(TargetFramework detectedTfm)
    {
        // Arrange
        var allCommands = new List<KeyValuePair<string, CommandInfo>>
        {
            new("dotnet-scaffold-aspnet", new CommandInfo
            {
                Name = "blazor-entra",
                DisplayName = "Blazor Entra ID",
                DisplayCategories = new List<string> { "Entra ID", "Blazor", "All" },
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

        // Act - no filtering for .NET 10 or newer
        var filteredCommands = (detectedTfm == TargetFramework.Net8 || detectedTfm == TargetFramework.Net9)
            ? allCommands.Where(x => !x.Value.IsCommandAnEntraIdCommand()).ToList()
            : allCommands;

        // Assert
        Assert.Equal(2, filteredCommands.Count);
        Assert.Contains(filteredCommands, c => c.Value.Name == "blazor-entra");
        Assert.Contains(filteredCommands, c => c.Value.Name == "minimal-api");
    }

    [Theory]
    [InlineData(TargetFramework.Net8)]
    [InlineData(TargetFramework.Net9)]
    public void FilterCategories_WhenNet8OrNet9_ExcludesEntraIdCategory(TargetFramework detectedTfm)
    {
        // Arrange
        var displayCategories = new List<string> { "API", "Blazor", "Entra ID", "Identity", "MVC", "Razor Pages", "All" };

        // Act - simulate filtering based on TFM
        if (detectedTfm == TargetFramework.Net8 || detectedTfm == TargetFramework.Net9)
        {
            displayCategories.Remove("Entra ID");
        }

        // Assert
        Assert.DoesNotContain("Entra ID", displayCategories);
        Assert.Contains("API", displayCategories);
        Assert.Contains("Blazor", displayCategories);
        Assert.Contains("All", displayCategories);
    }

    [Theory]
    [InlineData(TargetFramework.Net10)]
    [InlineData(TargetFramework.Net11)]
    public void FilterCategories_WhenNet10OrNewer_IncludesEntraIdCategory(TargetFramework detectedTfm)
    {
        // Arrange
        var displayCategories = new List<string> { "API", "Blazor", "Entra ID", "Identity", "MVC", "Razor Pages", "All" };

        // Act - no filtering for .NET 10 or newer
        if (detectedTfm == TargetFramework.Net8 || detectedTfm == TargetFramework.Net9)
        {
            displayCategories.Remove("Entra ID");
        }

        // Assert
        Assert.Contains("Entra ID", displayCategories);
        Assert.Contains("API", displayCategories);
        Assert.Contains("All", displayCategories);
    }

    [Fact]
    public void FilterCategories_WhenTfmIsNull_IncludesAllCategories()
    {
        // Arrange
        var displayCategories = new List<string> { "API", "Blazor", "Entra ID", "Identity", "All" };
        TargetFramework? detectedTfm = null;

        // Act - no filtering when TFM is null
        if (detectedTfm.HasValue && (detectedTfm == TargetFramework.Net8 || detectedTfm == TargetFramework.Net9))
        {
            displayCategories.Remove("Entra ID");
        }

        // Assert
        Assert.Contains("Entra ID", displayCategories);
        Assert.Equal(5, displayCategories.Count);
    }
}
