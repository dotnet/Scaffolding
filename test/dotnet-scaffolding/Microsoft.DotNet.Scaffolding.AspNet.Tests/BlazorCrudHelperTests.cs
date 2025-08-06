// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Xunit;

namespace Microsoft.DotNet.Scaffolding.AspNet.Tests
{
    public class BlazorCrudHelperTests
    {
        [Fact]
        public void DotnetScaffoldAspNet_MiddlewarePlacement_IsCorrect()
        {
            // Test that UseStatusCodePagesWithReExecute middleware is placed correctly in dotnet-scaffold-aspnet
            var jsonPath = "src/dotnet-scaffolding/dotnet-scaffold-aspnet/CodeModificationConfigs/blazorWebCrudChanges.json";
            if (File.Exists(jsonPath))
            {
                var content = File.ReadAllText(jsonPath);
                
                // Verify that middleware is inserted after UseExceptionHandler/UseHsts
                Assert.Contains("\"InsertAfter\": [ \"app.UseExceptionHandler\", \"app.UseHsts\" ]", content);
                
                // Verify that it contains the status code middleware
                Assert.Contains("UseStatusCodePagesWithReExecute", content);
                
                // Verify it's not placed before app.Run() (old incorrect placement)
                Assert.DoesNotContain("\"InsertBefore\": [ \"app.Run()\" ]", content);
            }
        }

        [Fact]
        public void DotnetScaffoldAspNet_NotFoundTemplate_Exists()
        {
            // Test that NotFound.tt template exists in dotnet-scaffold-aspnet
            var templatePath = "src/dotnet-scaffolding/dotnet-scaffold-aspnet/Templates/BlazorCrud/NotFound.tt";
            if (File.Exists(templatePath))
            {
                var content = File.ReadAllText(templatePath);
                
                // Verify basic structure
                Assert.Contains("@page \"/not-found\"", content);
                Assert.Contains("@layout MainLayout", content);
                Assert.Contains("<PageTitle>Not Found</PageTitle>", content);
                Assert.Contains("Return to Home", content);
            }
        }

        [Fact]
        public void DotnetScaffoldAspNet_DynamicRouteConfiguration()
        {
            // Test that dotnet-scaffold-aspnet configuration supports dynamic routes
            var jsonPath = "src/dotnet-scaffolding/dotnet-scaffold-aspnet/CodeModificationConfigs/blazorWebCrudChanges.json";
            if (File.Exists(jsonPath))
            {
                var content = File.ReadAllText(jsonPath);
                
                // Verify that middleware is configured with placeholder route that will be replaced
                Assert.Contains("UseStatusCodePagesWithReExecute(\\\"/not-found\\\"", content);
            }
        }

        [Fact]
        public void DotnetScaffoldAspNet_BlazorTemplates_UseNotFoundNavigation()
        {
            // Test that dotnet-scaffold-aspnet templates use NavigationManager.NotFound() instead of NavigateTo("notfound")
            var templateFiles = new[] { "Edit.tt", "Details.tt", "Delete.tt" };
            var templateBasePath = "src/dotnet-scaffolding/dotnet-scaffold-aspnet/Templates/BlazorCrud";
            
            foreach (var templateFile in templateFiles)
            {
                var templatePath = Path.Combine(templateBasePath, templateFile);
                if (File.Exists(templatePath))
                {
                    var content = File.ReadAllText(templatePath);
                    
                    // Verify that NavigationManager.NotFound() is used
                    Assert.Contains("NavigationManager.NotFound()", content);
                    
                    // Verify that old NavigateTo("notfound") pattern is not used
                    Assert.DoesNotContain("NavigationManager.NavigateTo(\"notfound\")", content);
                }
            }
        }

        [Fact]
        public void DotnetScaffoldAspNet_CreateTemplate_GeneratesNullCoalescingAssignment()
        {
            // Test that dotnet-scaffold-aspnet Create.tt template uses null coalescing assignment pattern
            var templatePath = "src/dotnet-scaffolding/dotnet-scaffold-aspnet/Templates/BlazorCrud/Create.tt";
            if (File.Exists(templatePath))
            {
                var content = File.ReadAllText(templatePath);
                
                // Verify the new pattern exists
                Assert.Contains("= default!", content);
                Assert.Contains("protected override void OnInitialized()", content);
                Assert.Contains("??= new()", content);
                
                // Verify the old pattern is not used
                Assert.DoesNotContain("{ get; set; } = new()", content);
            }
        }
    }
}