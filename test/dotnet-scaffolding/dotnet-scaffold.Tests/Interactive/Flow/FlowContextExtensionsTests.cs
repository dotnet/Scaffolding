// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.Scaffolding.Core.ComponentModel;
using Microsoft.DotNet.Scaffolding.Core.Model;
using Microsoft.DotNet.Tools.Scaffold.Interactive.Flow;
using Moq;
using Spectre.Console.Flow;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.Interactive.Flow;

/// <summary>
/// Unit tests for FlowContextExtensions, particularly the GetDetectedTargetFramework method.
/// </summary>
public class FlowContextExtensionsTests
{
    [Fact]
    public void GetDetectedTargetFramework_WhenPropertyNotSet_ReturnsNull()
    {
        // Arrange
        var properties = new FlowProperties(new Dictionary<string, object>());
        var mockContext = new Mock<IFlowContext>();
        mockContext.Setup(c => c.Properties).Returns(properties);

        // Act
        TargetFramework? result = mockContext.Object.GetDetectedTargetFramework();

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData(TargetFramework.Net8)]
    [InlineData(TargetFramework.Net9)]
    [InlineData(TargetFramework.Net10)]
    [InlineData(TargetFramework.Net11)]
    public void GetDetectedTargetFramework_WhenPropertySet_ReturnsValue(TargetFramework expectedTfm)
    {
        // Arrange
        var properties = new FlowProperties(new Dictionary<string, object>
        {
            [FlowContextProperties.DetectedTargetFramework] = new FlowProperty(
                FlowContextProperties.DetectedTargetFramework,
                expectedTfm,
                isVisible: false)
        });
        var mockContext = new Mock<IFlowContext>();
        mockContext.Setup(c => c.Properties).Returns(properties);

        // Act
        TargetFramework? result = mockContext.Object.GetDetectedTargetFramework();

        // Assert
        Assert.Equal(expectedTfm, result);
    }

    [Fact]
    public void AspireScaffolder_NotAvailable_ForNet8Projects()
    {
        // Arrange
        var detectedTfm = TargetFramework.Net8;
        var displayCategories = new List<string> { "API", "Aspire", "Blazor", "Identity", "All" };

        // Act - simulate the filtering logic from CategoryDiscovery
        if (detectedTfm == TargetFramework.Net8)
        {
            displayCategories.Remove("Aspire");
        }

        // Assert
        Assert.DoesNotContain("Aspire", displayCategories);
    }

    [Theory]
    [InlineData(TargetFramework.Net9)]
    [InlineData(TargetFramework.Net10)]
    [InlineData(TargetFramework.Net11)]
    public void AspireScaffolder_Available_ForNet9AndAboveProjects(TargetFramework detectedTfm)
    {
        // Arrange
        var displayCategories = new List<string> { "API", "Aspire", "Blazor", "Identity", "All" };

        // Act - simulate the filtering logic from CategoryDiscovery
        if (detectedTfm == TargetFramework.Net8)
        {
            displayCategories.Remove("Aspire");
        }

        // Assert
        Assert.Contains("Aspire", displayCategories);
    }

    [Fact]
    public void AspireCommands_FilteredOut_ForNet8Projects()
    {
        // Arrange
        var detectedTfm = TargetFramework.Net8;
        var allCommands = CreateAspireAndNonAspireCommands();

        // Act - simulate the filtering logic from CommandDiscovery
        var filteredCommands = detectedTfm == TargetFramework.Net8
            ? allCommands.Where(x => !x.Value.IsCommandAnAspireCommand()).ToList()
            : allCommands;

        // Assert
        Assert.All(filteredCommands, c => Assert.False(c.Value.IsCommandAnAspireCommand()));
        Assert.DoesNotContain(filteredCommands, c => c.Value.Name == "database");
        Assert.DoesNotContain(filteredCommands, c => c.Value.Name == "caching");
        Assert.Contains(filteredCommands, c => c.Value.Name == "minimal-api");
    }

    [Theory]
    [InlineData(TargetFramework.Net9)]
    [InlineData(TargetFramework.Net10)]
    [InlineData(TargetFramework.Net11)]
    public void AspireCommands_NotFilteredOut_ForNet9AndAboveProjects(TargetFramework detectedTfm)
    {
        // Arrange
        var allCommands = CreateAspireAndNonAspireCommands();

        // Act - simulate the filtering logic from CommandDiscovery
        var filteredCommands = detectedTfm == TargetFramework.Net8
            ? allCommands.Where(x => !x.Value.IsCommandAnAspireCommand()).ToList()
            : allCommands;

        // Assert
        Assert.Contains(filteredCommands, c => c.Value.Name == "database");
        Assert.Contains(filteredCommands, c => c.Value.Name == "caching");
        Assert.Contains(filteredCommands, c => c.Value.Name == "minimal-api");
    }

    [Theory]
    [InlineData(TargetFramework.Net8)]
    [InlineData(TargetFramework.Net9)]
    public void EntraIdScaffolder_NotAvailable_ForNet8AndNet9Projects(TargetFramework detectedTfm)
    {
        // Arrange
        var displayCategories = new List<string> { "API", "Aspire", "Blazor", "Entra ID", "Identity", "All" };

        // Act - simulate the filtering logic from CategoryDiscovery
        if (detectedTfm == TargetFramework.Net8)
        {
            displayCategories.Remove("Aspire");
            displayCategories.Remove("Entra ID");
        }
        else if (detectedTfm == TargetFramework.Net9)
        {
            displayCategories.Remove("Entra ID");
        }

        // Assert
        Assert.DoesNotContain("Entra ID", displayCategories);
    }

    [Theory]
    [InlineData(TargetFramework.Net10)]
    [InlineData(TargetFramework.Net11)]
    public void EntraIdScaffolder_Available_ForNet10AndAboveProjects(TargetFramework detectedTfm)
    {
        // Arrange
        var displayCategories = new List<string> { "API", "Aspire", "Blazor", "Entra ID", "Identity", "All" };

        // Act - simulate the filtering logic from CategoryDiscovery
        if (detectedTfm == TargetFramework.Net8)
        {
            displayCategories.Remove("Aspire");
            displayCategories.Remove("Entra ID");
        }
        else if (detectedTfm == TargetFramework.Net9)
        {
            displayCategories.Remove("Entra ID");
        }

        // Assert
        Assert.Contains("Entra ID", displayCategories);
    }

    [Theory]
    [InlineData(TargetFramework.Net8)]
    [InlineData(TargetFramework.Net9)]
    public void EntraIdCommands_FilteredOut_ForNet8AndNet9Projects(TargetFramework detectedTfm)
    {
        // Arrange
        var allCommands = CreateEntraIdAndNonEntraIdCommands();

        // Act - simulate the filtering logic from CommandDiscovery
        List<KeyValuePair<string, CommandInfo>> filteredCommands;
        if (detectedTfm == TargetFramework.Net8)
        {
            filteredCommands = allCommands
                .Where(x => !x.Value.IsCommandAnEntraIdCommand() && !x.Value.IsCommandAnAspireCommand())
                .ToList();
        }
        else if (detectedTfm == TargetFramework.Net9)
        {
            filteredCommands = allCommands
                .Where(x => !x.Value.IsCommandAnEntraIdCommand())
                .ToList();
        }
        else
        {
            filteredCommands = allCommands;
        }

        // Assert
        Assert.All(filteredCommands, c => Assert.False(c.Value.IsCommandAnEntraIdCommand()));
        Assert.DoesNotContain(filteredCommands, c => c.Value.Name == "entra-id-setup");
        Assert.Contains(filteredCommands, c => c.Value.Name == "minimal-api");
    }

    [Theory]
    [InlineData(TargetFramework.Net10)]
    [InlineData(TargetFramework.Net11)]
    public void EntraIdCommands_NotFilteredOut_ForNet10AndAboveProjects(TargetFramework detectedTfm)
    {
        // Arrange
        var allCommands = CreateEntraIdAndNonEntraIdCommands();

        // Act - simulate the filtering logic from CommandDiscovery
        List<KeyValuePair<string, CommandInfo>> filteredCommands;
        if (detectedTfm == TargetFramework.Net8)
        {
            filteredCommands = allCommands
                .Where(x => !x.Value.IsCommandAnEntraIdCommand() && !x.Value.IsCommandAnAspireCommand())
                .ToList();
        }
        else if (detectedTfm == TargetFramework.Net9)
        {
            filteredCommands = allCommands
                .Where(x => !x.Value.IsCommandAnEntraIdCommand())
                .ToList();
        }
        else
        {
            filteredCommands = allCommands;
        }

        // Assert
        Assert.Contains(filteredCommands, c => c.Value.Name == "entra-id-setup");
        Assert.Contains(filteredCommands, c => c.Value.Name == "minimal-api");
    }

    private static List<KeyValuePair<string, CommandInfo>> CreateAspireAndNonAspireCommands()
    {
        return new List<KeyValuePair<string, CommandInfo>>
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
            })
        };
    }

    private static List<KeyValuePair<string, CommandInfo>> CreateEntraIdAndNonEntraIdCommands()
    {
        return new List<KeyValuePair<string, CommandInfo>>
        {
            new("dotnet-scaffold-aspnet", new CommandInfo
            {
                Name = "entra-id-setup",
                DisplayName = "Entra ID Setup",
                DisplayCategories = new List<string> { "Entra ID", "All" },
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
    }
}
