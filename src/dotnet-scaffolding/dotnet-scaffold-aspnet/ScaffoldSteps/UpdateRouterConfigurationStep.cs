// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Scaffolding.Core.Scaffolders;
using Microsoft.DotNet.Scaffolding.Core.Steps;
using Microsoft.DotNet.Scaffolding.Internal.Services;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Models;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.ScaffoldSteps;

internal class UpdateRouterConfigurationStep : ScaffoldStep
{
    public required string ProjectPath { get; set; }
    public required BlazorCrudAppProperties AppProperties { get; set; }
    private readonly IFileSystem _fileSystem;

    public UpdateRouterConfigurationStep(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public override async Task<bool> ExecuteAsync(ScaffolderContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(ProjectPath) || AppProperties == null)
        {
            return false;
        }

        try
        {
            string projectBasePath = Path.GetDirectoryName(ProjectPath) ?? Directory.GetCurrentDirectory();
            
            // Prioritize Routes.razor over App.razor as mentioned in the comment
            var routesRazorPath = Path.Combine(projectBasePath, "Components", "Routes.razor");
            var appRazorPath = Path.Combine(projectBasePath, "Components", "App.razor");
            
            string? targetFilePath = null;
            string notFoundPageType = "";
            
            if (_fileSystem.FileExists(routesRazorPath))
            {
                targetFilePath = routesRazorPath;
                notFoundPageType = "typeof(Pages.NotFound)";
            }
            else if (_fileSystem.FileExists(appRazorPath))
            {
                targetFilePath = appRazorPath;
                notFoundPageType = "typeof(Components.Pages.NotFound)";
            }
            
            if (!string.IsNullOrEmpty(targetFilePath))
            {
                var content = _fileSystem.ReadAllText(targetFilePath);
                
                // Check if NotFoundPage parameter already exists
                if (!content.Contains("NotFoundPage"))
                {
                    // Replace Router opening tag to include NotFoundPage parameter
                    var updatedContent = content.Replace(
                        "<Router AppAssembly=\"@typeof(App).Assembly\">",
                        $"<Router AppAssembly=\"@typeof(App).Assembly\" NotFoundPage=\"{notFoundPageType}\">");
                    
                    _fileSystem.WriteAllText(targetFilePath, updatedContent);
                    Console.WriteLine($"Updated {Path.GetFileName(targetFilePath)} with NotFoundPage configuration.");
                }
                else
                {
                    Console.WriteLine($"{Path.GetFileName(targetFilePath)} already has NotFoundPage configuration.");
                }
            }
            
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating Router configuration: {ex.Message}");
            return false;
        }
    }
}