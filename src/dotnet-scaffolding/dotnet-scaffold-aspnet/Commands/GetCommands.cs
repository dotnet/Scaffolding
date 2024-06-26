// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Collections.Generic;
using Microsoft.DotNet.Scaffolding.ComponentModel;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Commands.BlazorCrud;
using Microsoft.DotNet.Tools.Scaffold.AspNet.Helpers;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Commands
{
    public class GetCmdsCommand : Command
    {
        public override int Execute(CommandContext context)
        {
            var commands = new List<CommandInfo>
            {
                new()
                {
                    Name = "minimalapi",
                    DisplayName = "Minimal API",
                    DisplayCategory = "API",
                    Description = "Generates an endpoints file (with CRUD API endpoints) given a model and optional DbContext.",
                    Parameters = GetCmdsHelper.MinimalApiParameters // Add parameters if any
                },
                new()
                {
                    Name = "area",
                    DisplayName = "Area",
                    DisplayCategory = "MVC",
                    Description = "Generates an Area folder structure.",
                    Parameters = GetCmdsHelper.AreaParameters // Add parameters if any
                },
                new()
                {
                    Name = "blazor-empty",
                    DisplayName = "Razor Component - Empty",
                    DisplayCategory = "Blazor",
                    Description = "Generates an empty Razor Component (.razor) file.",
                    Parameters = GetCmdsHelper.BlazorEmptyParameters
                },
                new()
                {
                    Name = "razorpage-empty",
                    DisplayName = "Razor Page - Empty",
                    DisplayCategory = "Web",
                    Description = "Generates an empty Razor Page (.cshtml) file.",
                    Parameters = GetCmdsHelper.RazorPageEmptyParameters
                },
                new()
                {
                    Name = "mvccontroller-empty",
                    DisplayName = "MVC Controller - Empty",
                    DisplayCategory = "MVC",
                    Description = "Generates an empty MVC Controller (.cs) file.",
                    Parameters = GetCmdsHelper.MvcControllerEmptyParameters
                },
                new()
                {
                    Name = "apicontroller-empty",
                    DisplayName = "API Controller - Empty",
                    DisplayCategory = "API",
                    Description = "Generates an empty API Controller (.cs) file.",
                    Parameters = GetCmdsHelper.ApiControllerEmptyParameters
                },
                new()
                {
                    Name = "razorview-empty",
                    DisplayName = "Razor View - Empty",
                    DisplayCategory = "MVC",
                    Description = "Generates an empty Razor View",
                    Parameters = GetCmdsHelper.RazorViewEmptyParameters
                },
                new()
                {
                    Name = "blazor-crud",
                    DisplayName = "Razor Components w/ EF (CRUD)",
                    DisplayCategory = "Blazor",
                    Description = "Generates Razor Components using Entity Framework for Create, Delete, Details, Edit and List operations for the given model",
                    Parameters = GetCmdsHelper.BlazorCrudParameters
                }
                // Add other commands here
            };

            var json = System.Text.Json.JsonSerializer.Serialize(commands);
            AnsiConsole.WriteLine(json);
            return 0;
        }
    }

    public static class GetCmdsHelper
    {
        internal static Parameter Project = new() { Name = "--project", DisplayName = "Project File", Description = "Project file for scaffolding's project context", Required = true, Type = BaseTypes.String, PickerType = InteractivePickerType.ProjectPicker };
        internal static Parameter ModelName = new()  { Name = "--model", DisplayName = "Model Name", Description = "Name for the model class to be used for scaffolding", Required = true, Type = BaseTypes.String, PickerType = InteractivePickerType.ClassPicker };
        internal static Parameter EndpointsClass = new()  { Name = "--endpoints", DisplayName = "Endpoints File Name", Description = "", Required = true, Type = BaseTypes.String };
        internal static Parameter DataContextClass = new() { Name = "--dataContext", DisplayName = "Data Context Class", Description = "", Required = false, Type = BaseTypes.String };
        internal static Parameter DataContextClassRequired = new() { Name = "--dataContext", DisplayName = "Data Context Class", Description = "", Required = true, Type = BaseTypes.String };
        internal static Parameter OpenApi = new() { Name = "--open", DisplayName = "Open API Enabled", Description = "", Required = false, Type = BaseTypes.Bool, PickerType = InteractivePickerType.YesNo };
        internal static Parameter DatabaseProvider = new() { Name = "--dbProvider", DisplayName = "Database Provider", Description = "", Required = false, Type = BaseTypes.String, PickerType = InteractivePickerType.CustomPicker, CustomPickerValues = [.. AspNetDbContextHelper.DatabaseTypeDefaults.Keys] };
        internal static Parameter DatabaseProviderRequired = new() { Name = "--dbProvider", DisplayName = "Database Provider", Description = "", Required = true, Type = BaseTypes.String, PickerType = InteractivePickerType.CustomPicker, CustomPickerValues = [.. AspNetDbContextHelper.DatabaseTypeDefaults.Keys] };
        internal static Parameter PrereleaseParameter = new() { Name = "--prerelease", DisplayName = "Include prerelease packages?", Description = "Include prerelease package versions when installing latest Aspire components", Required = true, Type = BaseTypes.Bool, PickerType = InteractivePickerType.YesNo };
        internal static Parameter PageType = new() { Name = "--page", DisplayName = "Page Type", Description = "The CRUD page(s) to scaffold", Required = true, Type = BaseTypes.String, PickerType = InteractivePickerType.CustomPicker, CustomPickerValues = BlazorCrudHelper.CRUDPages };
        internal static Parameter AreaName = new() { Name = "--name", DisplayName = "Area Name", Description = "Name for the area being created", Required = true, Type = BaseTypes.String };
        internal static Parameter FileName = new() { Name = "--name", DisplayName = "File Name", Description = "Name for the item being created", Required = true, Type = BaseTypes.String };
        internal static Parameter Actions = new() { Name = "--actions", DisplayName = "Read/Write Actions", Description = "Create controller with read/write actions?", Required = true, Type = BaseTypes.Bool, PickerType = InteractivePickerType.YesNo };
        internal static Parameter[] AreaParameters = [Project, AreaName];
        internal static Parameter[] MinimalApiParameters = [Project, ModelName, EndpointsClass, DataContextClass, DatabaseProvider, OpenApi, PrereleaseParameter];
        internal static Parameter[] BlazorEmptyParameters = [Project, FileName];
        internal static Parameter[] MvcControllerEmptyParameters = [Project, FileName, Actions];
        internal static Parameter[] ApiControllerEmptyParameters = [Project, FileName, Actions];
        internal static Parameter[] RazorPageEmptyParameters = [Project, FileName];
        internal static Parameter[] RazorViewEmptyParameters = [Project, FileName];
        internal static Parameter[] BlazorCrudParameters = [Project, ModelName, PageType, DataContextClassRequired, DatabaseProviderRequired, PrereleaseParameter];
    }
}
