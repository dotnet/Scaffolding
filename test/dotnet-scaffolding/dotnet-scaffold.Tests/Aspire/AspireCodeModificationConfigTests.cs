// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.DotNet.Scaffolding.Shared.CodeModifier;
using Microsoft.DotNet.Scaffolding.Shared.CodeModifier.CodeChange;
using Xunit;

namespace Microsoft.DotNet.Tools.Scaffold.Tests.Aspire;

public class AspireCodeModificationConfigTests
{
    private readonly string _sourceRoot;
    private readonly string _aspireConfigsPath;

    public AspireCodeModificationConfigTests()
    {
        // Get the path to the Aspire CodeModificationConfigs
        _sourceRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "dotnet-scaffolding", "dotnet-scaffold", "Aspire", "CodeModificationConfigs"));
        _aspireConfigsPath = _sourceRoot;
    }

    [Theory]
    [InlineData("net8.0")]
    [InlineData("net9.0")]
    [InlineData("net10.0")]
    [InlineData("net11.0")]
    public void AspireConfigDirectory_Exists(string tfm)
    {
        // Arrange & Act
        var configPath = Path.Combine(_aspireConfigsPath, tfm);

        // Assert
        Assert.True(Directory.Exists(configPath), $"Configuration directory for {tfm} should exist at {configPath}");
    }

    [Theory]
    [InlineData("net8.0", "Caching", "redis-apphost.json")]
    [InlineData("net8.0", "Caching", "redis-webapp.json")]
    [InlineData("net8.0", "Caching", "redis-webapp-oc.json")]
    [InlineData("net8.0", "Database", "db-apphost.json")]
    [InlineData("net8.0", "Database", "db-webapi.json")]
    [InlineData("net8.0", "Storage", "storage-apphost.json")]
    [InlineData("net8.0", "Storage", "storage-webapi.json")]
    [InlineData("net9.0", "Caching", "redis-apphost.json")]
    [InlineData("net9.0", "Caching", "redis-webapp.json")]
    [InlineData("net9.0", "Caching", "redis-webapp-oc.json")]
    [InlineData("net9.0", "Database", "db-apphost.json")]
    [InlineData("net9.0", "Database", "db-webapi.json")]
    [InlineData("net9.0", "Storage", "storage-apphost.json")]
    [InlineData("net9.0", "Storage", "storage-webapi.json")]
    [InlineData("net10.0", "Caching", "redis-apphost.json")]
    [InlineData("net10.0", "Caching", "redis-webapp.json")]
    [InlineData("net10.0", "Caching", "redis-webapp-oc.json")]
    [InlineData("net10.0", "Database", "db-apphost.json")]
    [InlineData("net10.0", "Database", "db-webapi.json")]
    [InlineData("net10.0", "Storage", "storage-apphost.json")]
    [InlineData("net10.0", "Storage", "storage-webapi.json")]
    [InlineData("net11.0", "Caching", "redis-apphost.json")]
    [InlineData("net11.0", "Caching", "redis-webapp.json")]
    [InlineData("net11.0", "Caching", "redis-webapp-oc.json")]
    [InlineData("net11.0", "Database", "db-apphost.json")]
    [InlineData("net11.0", "Database", "db-webapi.json")]
    [InlineData("net11.0", "Storage", "storage-apphost.json")]
    [InlineData("net11.0", "Storage", "storage-webapi.json")]
    public void ConfigFile_Exists(string tfm, string category, string fileName)
    {
        // Arrange & Act
        var filePath = Path.Combine(_aspireConfigsPath, tfm, category, fileName);

        // Assert
        Assert.True(File.Exists(filePath), $"Configuration file {fileName} should exist at {filePath}");
    }

    [Theory]
    [InlineData("net8.0", "Caching", "redis-apphost.json")]
    [InlineData("net9.0", "Database", "db-webapi.json")]
    [InlineData("net10.0", "Storage", "storage-apphost.json")]
    [InlineData("net11.0", "Caching", "redis-webapp-oc.json")]
    public void ConfigFile_IsValidJson(string tfm, string category, string fileName)
    {
        // Arrange
        var filePath = Path.Combine(_aspireConfigsPath, tfm, category, fileName);
        var jsonContent = File.ReadAllText(filePath);

        // Act & Assert
        var exception = Record.Exception(() => JsonDocument.Parse(jsonContent));
        Assert.Null(exception);
    }

    [Theory]
    [InlineData("net8.0", "Caching", "redis-apphost.json")]
    [InlineData("net9.0", "Database", "db-webapi.json")]
    [InlineData("net10.0", "Storage", "storage-apphost.json")]
    [InlineData("net11.0", "Caching", "redis-webapp.json")]
    public void ConfigFile_CanBeDeserialized(string tfm, string category, string fileName)
    {
        // Arrange
        var filePath = Path.Combine(_aspireConfigsPath, tfm, category, fileName);
        var jsonContent = File.ReadAllText(filePath);

        // Act
        var config = JsonSerializer.Deserialize<CodeModifierConfig>(jsonContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        Assert.NotNull(config);
        Assert.NotNull(config.Files);
        Assert.NotEmpty(config.Files);
    }

    [Theory]
    [InlineData("net8.0", "Caching", "redis-apphost.json", "Program.cs")]
    [InlineData("net9.0", "Database", "db-apphost.json", "Program.cs")]
    [InlineData("net10.0", "Storage", "storage-webapi.json", "Program.cs")]
    [InlineData("net11.0", "Caching", "redis-webapp-oc.json", "Program.cs")]
    public void ConfigFile_HasCorrectFileName(string tfm, string category, string fileName, string expectedFileName)
    {
        // Arrange
        var filePath = Path.Combine(_aspireConfigsPath, tfm, category, fileName);
        var jsonContent = File.ReadAllText(filePath);
        var config = JsonSerializer.Deserialize<CodeModifierConfig>(jsonContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Act
        var codeFile = config?.Files?.FirstOrDefault();

        // Assert
        Assert.NotNull(codeFile);
        Assert.Equal(expectedFileName, codeFile.FileName);
    }

    [Fact]
    public void RedisAppHost_Config_HasCorrectStructure()
    {
        // Arrange
        var filePath = Path.Combine(_aspireConfigsPath, "net9.0", "Caching", "redis-apphost.json");
        var jsonContent = File.ReadAllText(filePath);
        var config = JsonSerializer.Deserialize<CodeModifierConfig>(jsonContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        Assert.NotNull(config);
        Assert.NotNull(config.Files);
        var codeFile = config.Files.First();
        Assert.Equal("Program.cs", codeFile.FileName);
        Assert.NotNull(codeFile.Methods);
        Assert.True(codeFile.Methods.ContainsKey("Global"));
        
        var method = codeFile.Methods["Global"];
        Assert.NotNull(method.CodeChanges);
        Assert.Equal(2, method.CodeChanges.Length);
        
        // First change: AddRedis
        var firstChange = method.CodeChanges[0];
        Assert.Contains("AddRedis", firstChange.Block);
        Assert.Contains("var redis = builder.AddRedis(\"redis\")", firstChange.Block);
        
        // Second change: WithReference
        var secondChange = method.CodeChanges[1];
        Assert.Contains("WithReference", secondChange.Block);
        Assert.Equal(CodeChangeType.MemberAccess, secondChange.CodeChangeType);
    }

    [Fact]
    public void StorageAppHost_Config_HasCorrectStructure()
    {
        // Arrange
        var filePath = Path.Combine(_aspireConfigsPath, "net9.0", "Storage", "storage-apphost.json");
        var jsonContent = File.ReadAllText(filePath);
        var config = JsonSerializer.Deserialize<CodeModifierConfig>(jsonContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        Assert.NotNull(config);
        Assert.NotNull(config.Files);
        var codeFile = config.Files.First();
        Assert.Equal("Program.cs", codeFile.FileName);
        Assert.NotNull(codeFile.Methods);
        Assert.True(codeFile.Methods.ContainsKey("Global"));
        
        var method = codeFile.Methods["Global"];
        Assert.NotNull(method.CodeChanges);
        Assert.Equal(3, method.CodeChanges.Length);
        
        // First change: AddAzureStorage
        var firstChange = method.CodeChanges[0];
        Assert.Contains("AddAzureStorage", firstChange.Block);
        Assert.Contains("RunAsEmulator", firstChange.Block);
        
        // Second change: AddStorageMethodName
        var secondChange = method.CodeChanges[1];
        Assert.Contains("$(StorageVariableName)", secondChange.Block);
        Assert.Contains("$(AddStorageMethodName)", secondChange.Block);
        
        // Third change: WithReference
        var thirdChange = method.CodeChanges[2];
        Assert.Contains("WithReference", thirdChange.Block);
        Assert.Equal(CodeChangeType.MemberAccess, thirdChange.CodeChangeType);
    }

    [Fact]
    public void DatabaseAppHost_Config_HasCorrectStructure()
    {
        // Arrange
        var filePath = Path.Combine(_aspireConfigsPath, "net9.0", "Database", "db-apphost.json");
        var jsonContent = File.ReadAllText(filePath);
        var config = JsonSerializer.Deserialize<CodeModifierConfig>(jsonContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        Assert.NotNull(config);
        Assert.NotNull(config.Files);
        var codeFile = config.Files.First();
        Assert.Equal("Program.cs", codeFile.FileName);
        Assert.NotNull(codeFile.Methods);
        Assert.True(codeFile.Methods.ContainsKey("Global"));
        
        var method = codeFile.Methods["Global"];
        Assert.NotNull(method.CodeChanges);
        Assert.Equal(2, method.CodeChanges.Length);
        
        // First change: AddDbMethod and AddDatabase
        var firstChange = method.CodeChanges[0];
        Assert.Contains("$(AddDbMethod)", firstChange.Block);
        Assert.Contains("AddDatabase", firstChange.Block);
        Assert.Contains("$(DbType)", firstChange.Block);
        Assert.Contains("$(DbName)", firstChange.Block);
        
        // Second change: WithReference
        var secondChange = method.CodeChanges[1];
        Assert.Contains("WithReference", secondChange.Block);
        Assert.Equal(CodeChangeType.MemberAccess, secondChange.CodeChangeType);
    }

    [Fact]
    public void RedisWebApp_Config_HasCorrectStructure()
    {
        // Arrange
        var filePath = Path.Combine(_aspireConfigsPath, "net9.0", "Caching", "redis-webapp.json");
        var jsonContent = File.ReadAllText(filePath);
        var config = JsonSerializer.Deserialize<CodeModifierConfig>(jsonContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        Assert.NotNull(config);
        Assert.NotNull(config.Files);
        var codeFile = config.Files.First();
        Assert.Equal("Program.cs", codeFile.FileName);
        Assert.NotNull(codeFile.Methods);
        Assert.True(codeFile.Methods.ContainsKey("Global"));
        
        var method = codeFile.Methods["Global"];
        Assert.NotNull(method.CodeChanges);
        Assert.Single(method.CodeChanges);
        
        var change = method.CodeChanges[0];
        Assert.Contains("AddRedisClient", change.Block);
        Assert.Equal("builder.AddServiceDefaults()", change.InsertAfter);
    }

    [Fact]
    public void RedisOutputCache_Config_HasCorrectStructure()
    {
        // Arrange
        var filePath = Path.Combine(_aspireConfigsPath, "net9.0", "Caching", "redis-webapp-oc.json");
        var jsonContent = File.ReadAllText(filePath);
        var config = JsonSerializer.Deserialize<CodeModifierConfig>(jsonContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        Assert.NotNull(config);
        Assert.NotNull(config.Files);
        var codeFile = config.Files.First();
        Assert.Equal("Program.cs", codeFile.FileName);
        Assert.NotNull(codeFile.Replacements);
        Assert.Single(codeFile.Replacements);
        
        var replacement = codeFile.Replacements[0];
        Assert.Contains("AddRedisOutputCache", replacement.Block);
        Assert.NotNull(replacement.ReplaceSnippet);
        Assert.Contains("builder.Services.AddOutputCache()", replacement.ReplaceSnippet);
    }

    [Fact]
    public void DatabaseWebApi_Config_HasCorrectStructure()
    {
        // Arrange
        var filePath = Path.Combine(_aspireConfigsPath, "net9.0", "Database", "db-webapi.json");
        var jsonContent = File.ReadAllText(filePath);
        var config = JsonSerializer.Deserialize<CodeModifierConfig>(jsonContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        Assert.NotNull(config);
        Assert.NotNull(config.Files);
        var codeFile = config.Files.First();
        Assert.Equal("Program.cs", codeFile.FileName);
        Assert.NotNull(codeFile.Methods);
        Assert.True(codeFile.Methods.ContainsKey("Global"));
        
        var method = codeFile.Methods["Global"];
        Assert.NotNull(method.CodeChanges);
        Assert.Single(method.CodeChanges);
        
        var change = method.CodeChanges[0];
        Assert.Contains("$(AddDbContextMethod)", change.Block);
        Assert.Contains("$(DbContextName)", change.Block);
        Assert.Contains("$(DbName)", change.Block);
        Assert.Equal("builder.AddServiceDefaults()", change.InsertAfter);
    }

    [Fact]
    public void StorageWebApi_Config_HasCorrectStructure()
    {
        // Arrange
        var filePath = Path.Combine(_aspireConfigsPath, "net9.0", "Storage", "storage-webapi.json");
        var jsonContent = File.ReadAllText(filePath);
        var config = JsonSerializer.Deserialize<CodeModifierConfig>(jsonContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        Assert.NotNull(config);
        Assert.NotNull(config.Files);
        var codeFile = config.Files.First();
        Assert.Equal("Program.cs", codeFile.FileName);
        Assert.NotNull(codeFile.Methods);
        Assert.True(codeFile.Methods.ContainsKey("Global"));
        
        var method = codeFile.Methods["Global"];
        Assert.NotNull(method.CodeChanges);
        Assert.Single(method.CodeChanges);
        
        var change = method.CodeChanges[0];
        Assert.Contains("$(AddClientMethodName)", change.Block);
        Assert.Contains("$(StorageVariableName)", change.Block);
        Assert.NotNull(change.InsertBefore);
        Assert.Contains("builder.Build()", change.InsertBefore);
    }

    [Theory]
    [InlineData("net8.0", "net9.0")]
    [InlineData("net9.0", "net10.0")]
    [InlineData("net10.0", "net11.0")]
    public void ConfigFiles_AreConsistentAcrossVersions(string tfm1, string tfm2)
    {
        // Arrange
        var categories = new[] { "Caching", "Database", "Storage" };

        foreach (var category in categories)
        {
            var dir1 = Path.Combine(_aspireConfigsPath, tfm1, category);
            var dir2 = Path.Combine(_aspireConfigsPath, tfm2, category);

            if (!Directory.Exists(dir1) || !Directory.Exists(dir2))
                continue;

            var files1 = Directory.GetFiles(dir1, "*.json").Select(Path.GetFileName).OrderBy(x => x).ToList();
            var files2 = Directory.GetFiles(dir2, "*.json").Select(Path.GetFileName).OrderBy(x => x).ToList();

            // Assert
            Assert.Equal(files1.Count, files2.Count);
            Assert.Equal(files1, files2);

            // Compare content structure
            foreach (var fileName in files1)
            {
                var content1 = File.ReadAllText(Path.Combine(dir1, fileName!));
                var content2 = File.ReadAllText(Path.Combine(dir2, fileName!));

                var config1 = JsonSerializer.Deserialize<CodeModifierConfig>(content1, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                var config2 = JsonSerializer.Deserialize<CodeModifierConfig>(content2, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                Assert.NotNull(config1);
                Assert.NotNull(config2);
                Assert.Equal(config1.Files?.Length ?? 0, config2.Files?.Length ?? 0);
            }
        }
    }

    [Theory]
    [InlineData("net8.0", "Caching", "redis-apphost.json")]
    [InlineData("net9.0", "Database", "db-webapi.json")]
    [InlineData("net10.0", "Storage", "storage-apphost.json")]
    public void ConfigFile_LeadingTrivia_IsCorrectlyFormatted(string tfm, string category, string fileName)
    {
        // Arrange
        var filePath = Path.Combine(_aspireConfigsPath, tfm, category, fileName);
        var jsonContent = File.ReadAllText(filePath);
        var config = JsonSerializer.Deserialize<CodeModifierConfig>(jsonContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Act
        var codeFile = config?.Files?.FirstOrDefault();
        var method = codeFile?.Methods?.Values.FirstOrDefault();
        var codeChanges = method?.CodeChanges;

        // Assert
        Assert.NotNull(codeChanges);
        foreach (var change in codeChanges)
        {
            if (change.LeadingTrivia != null)
            {
                // Verify LeadingTrivia has expected properties
                Assert.True(change.LeadingTrivia.Newline || change.LeadingTrivia.NumberOfSpaces >= 0);
            }
        }
    }

    [Theory]
    [InlineData("net8.0")]
    [InlineData("net9.0")]
    [InlineData("net10.0")]
    [InlineData("net11.0")]
    public void AllConfigFiles_InDirectory_AreValid(string tfm)
    {
        // Arrange
        var tfmPath = Path.Combine(_aspireConfigsPath, tfm);
        if (!Directory.Exists(tfmPath))
        {
            Assert.Fail($"Directory {tfmPath} does not exist");
        }

        var allJsonFiles = Directory.GetFiles(tfmPath, "*.json", SearchOption.AllDirectories);

        // Act & Assert
        foreach (var jsonFile in allJsonFiles)
        {
            var jsonContent = File.ReadAllText(jsonFile);
            var exception = Record.Exception(() =>
            {
                var config = JsonSerializer.Deserialize<CodeModifierConfig>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                Assert.NotNull(config);
                Assert.NotNull(config.Files);
            });

            Assert.Null(exception);
        }
    }
}
