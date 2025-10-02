// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Commands
{
    internal class AspnetStrings
    {
        internal class Blazor
        {
            internal const string Empty = "blazor-empty";
            internal const string EmptyDisplayName = "Razor Component";
            internal const string EmptyDescription = "Add an empty razor component to a given project";

            internal const string Identity = "blazor-identity";
            internal const string IdentityDisplayName = "Blazor Identity";
            internal const string IdentityDescription = "Add blazor identity to a project.";

            internal const string Crud = "blazor-crud";
            internal const string CrudDisplayName = "Razor Components with EntityFrameworkCore (CRUD)";
            internal const string CrudDescription = "Generates Razor Components using Entity Framework for Create, Delete, Details, Edit and List operations for the given model";
        }

        internal class RazorView
        {
            internal const string Empty = "razorview-empty";
            internal const string EmptyDisplayName = "Razor View - Empty";
            internal const string EmptyDescription = "Add an empty razor view to a given project";

            internal const string Views = "views";
            internal const string ViewsDisplayName = "Razor Views";
            internal const string ViewsDescription = "Generates Razor views for Create, Delete, Details, Edit and List operations for the given model";
        }

        internal class RazorPage
        {
            internal const string Empty = "razorpage-empty";
            internal const string EmptyDisplayName = "Razor Page - Empty";
            internal const string EmptyDescription = "Add an empty razor page to a given project";

            internal const string Crud = "razorpages-crud";
            internal const string CrudDisplayName = "Razor Pages with Entity Framework (CRUD)";
            internal const string CrudDescription = "Generates Razor pages using Entity Framework for Create, Delete, Details, Edit and List operations for the given model";
        }

        internal class Api
        {
            internal const string MinimalApi = "minimalapi";
            internal const string MinimalApiDisplayName = "Minimal API";
            internal const string MinimalApiDescription = "Generates an endpoints file (with CRUD API endpoints) given a model and optional DbContext.";

            internal const string ApiController = "apicontroller";
            internal const string ApiControllerDisplayName = "API Controller";
            internal const string ApiControllerDescription = "Add an empty API Controller to a given project";

            internal const string ApiControllerCrud = "apicontroller-crud";
            internal const string ApiControllerCrudDisplayName = "API Controller with actions, using Entity Framework (CRUD)";
            internal const string ApiControllerCrudDescription = "Create an API controller with REST actions to create, read, update, delete, and list entities";
        }

        internal class MVC
        {
            internal const string Controller = "mvccontroller";
            internal const string DisplayName = "MVC Controller";
            internal const string Description = "Add an empty MVC Controller to a given project";

            internal const string ControllerCrud = "mvccontroller-crud";
            internal const string CrudDisplayName = "MVC Controller with views, using Entity Framework (CRUD)";
            internal const string CrudDescription = "Create a MVC controller with read/write actions and views using Entity Framework";
        }

        internal class Area
        {
            internal const string Name = "area";
            internal const string DisplayName = "Area";
            internal const string Description = "Creates a MVC Area folder structure.";
        }

        internal class Identity
        {
            internal const string Name = "identity";
            internal const string DisplayName = "ASP.NET Core Identity";
            internal const string Description = "Add ASP.NET Core identity to a project.";
        }

        internal class EntraId
        {
            internal const string Name = "entra-id";
            internal const string DisplayName = "Entra ID";
            internal const string Description = "Add Entra auth";
        }

        internal class Catagories
        {
            internal const string Blazor = "Blazor";
            internal const string MVC = "MVC";
            internal const string RazorPages = "Razor Pages";
            internal const string API = "API";
            internal const string Identity = "Identity";
            internal const string EntraId = "Entra ID";
        }

        internal static class Options
        {
            internal static class Project
            {
                internal const string DisplayName = ".NET project file";
                internal const string Description = ".NET project to be used for scaffolding (.csproj file)";
            }

            internal static class Prerelease
            {
                internal const string DisplayName = "Include Prerelease packages?";
                internal const string Description = "Include prerelease package versions when installing latest Aspire components";
            }

            internal static class FileName
            {
                internal const string DisplayName = "File name";
                internal const string Description = "File name for new file being created with 'dotnet new'";
            }

            internal static class Actions
            {
                internal const string DisplayName = "Read/Write Actions?";
                internal const string Description = "Create controller with read/write actions?";
            }

            internal static class ControllerName
            {
                internal const string DisplayName = "Controller Name";
                internal const string Description = "Name for the controller being created";
            }

            internal static class AreaName
            {
                internal const string DisplayName = "Area Name";
                internal const string Description = "Name for the area being created";
            }

            internal static class ModelName
            {
                internal const string DisplayName = "Model Name";
                internal const string Description = "Name for the model class to be used for scaffolding";
            }

            internal const string EndpointClassDisplayName = "Endpoints File Name";

            internal const string DataContextClassDisplayName = "Data Context Class";

            internal const string OpenApiDisplayName = "Open API Enabled";

            internal const string DbProviderDisplayName = "Database Provider";

            internal static class PageType
            {
                internal const string DisplayName = "Page Type";
                internal const string Description = "The CRUD page(s) to scaffold";
            }

            internal static class View
            {
                internal const string DisplayName = "With Views?";
                internal const string Description = "Add CRUD razpr views (.cshtml)";
            }

            internal static class Overwrite
            {
                internal const string DisplayName = "Overwrite existing files?";
                internal const string Description = "Option to enable overwriting existing files";
            }

            internal static class Username
            {
                internal const string DisplayName = "Select username";
                internal const string Description = "User name for the itentity user";
            }

            internal static class TenantId
            {
                internal const string DisplayName = "Tenant Id";
                internal const string Description = "Tenant Id for the identity user";
            }

            internal static class Application
            {
                internal const string DisplayName = "Create or Select Application";
                internal const string Description = "Create or select existing application";
                internal static string[] Values = ["Select an existing Azure application object", "Create a new Azure application object"];
            }

            internal static class SelectApplication
            {
                internal const string DisplayName = "Select Application";
                internal const string Description = "Select existing application";
            }
        }
    }
}
