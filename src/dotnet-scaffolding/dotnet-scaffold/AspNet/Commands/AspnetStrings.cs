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
            internal const string EmptyExample = "dotnet scaffold aspnet blazor-empty --project C:/MyBlazorApp/MyBlazorApp.csproj --file-name ProductCard";
            internal const string EmptyExampleDescription = "Create an empty Razor component named ProductCard:";

            internal const string Identity = "blazor-identity";
            internal const string IdentityDisplayName = "Blazor Identity";
            internal const string IdentityDescription = "Add blazor identity to a project.";
            internal const string IdentityExample = "dotnet scaffold aspnet blazor-identity --project C:/MyBlazorApp/MyBlazorApp.csproj --database-provider SqlServer";
            internal const string IdentityExampleDescription = "Add Identity with SQL Server to a Blazor app:";

            internal const string Crud = "blazor-crud";
            internal const string CrudDisplayName = "Razor Components with EntityFrameworkCore (CRUD)";
            internal const string CrudDescription = "Generates Razor Components using Entity Framework for Create, Delete, Details, Edit and List operations for the given model";
            internal const string CrudExample1 = "dotnet scaffold aspnet blazor-crud --project C:/MyBlazorApp/MyBlazorApp.csproj --model Product --data-context AppDbContext --database-provider SqlServer --page All";
            internal const string CrudExample1Description = "Generate all CRUD pages for Product model:";
            internal const string CrudExample2 = "dotnet scaffold aspnet blazor-crud --project C:/MyApp/MyApp.csproj --model Customer --data-context ShopContext --database-provider PostgreSQL --page List,Edit";
            internal const string CrudExample2Description = "Generate only List and Edit pages with PostgreSQL:";
        }

        internal class RazorView
        {
            internal const string Empty = "razorview-empty";
            internal const string EmptyDisplayName = "Razor View - Empty";
            internal const string EmptyDescription = "Add an empty razor view to a given project";
            internal const string EmptyExample = "dotnet scaffold aspnet razorview-empty --project C:/MyMvcApp/MyMvcApp.csproj --file-name Dashboard";
            internal const string EmptyExampleDescription = "Create an empty view named Dashboard:";

            internal const string Views = "views";
            internal const string ViewsDisplayName = "Razor Views";
            internal const string ViewsDescription = "Generates Razor views for Create, Delete, Details, Edit and List operations for the given model";
            internal const string ViewsExample1 = "dotnet scaffold aspnet views --project C:/MyMvcApp/MyMvcApp.csproj --model Product --page All";
            internal const string ViewsExample1Description = "Generate all CRUD views for Product model:";
            internal const string ViewsExample2 = "dotnet scaffold aspnet views --project C:/MyApp/MyApp.csproj --model Customer --page Create,Edit";
            internal const string ViewsExample2Description = "Generate only Create and Edit views:";
        }

        internal class RazorPage
        {
            internal const string Empty = "razorpage-empty";
            internal const string EmptyDisplayName = "Razor Page - Empty";
            internal const string EmptyDescription = "Add an empty razor page to a given project";
            internal const string EmptyExample = "dotnet scaffold aspnet razorpage-empty --project C:/MyRazorApp/MyRazorApp.csproj --file-name Contact";
            internal const string EmptyExampleDescription = "Create an empty Razor page named Contact:";

            internal const string Crud = "razorpages-crud";
            internal const string CrudDisplayName = "Razor Pages with Entity Framework (CRUD)";
            internal const string CrudDescription = "Generates Razor pages using Entity Framework for Create, Delete, Details, Edit and List operations for the given model";
            internal const string CrudExample1 = "dotnet scaffold aspnet razorpages-crud --project C:/MyRazorApp/MyRazorApp.csproj --model Customer --data-context ShopDbContext --database-provider SqlServer --page All";
            internal const string CrudExample1Description = "Generate all CRUD pages for Customer model:";
            internal const string CrudExample2 = "dotnet scaffold aspnet razorpages-crud --project C:/MyApp/MyApp.csproj --model Order --data-context AppContext --database-provider SQLite --page List,Details --prerelease";
            internal const string CrudExample2Description = "Generate List and Details pages with SQLite using prerelease packages:";
        }

        internal class Api
        {
            internal const string MinimalApi = "minimalapi";
            internal const string MinimalApiDisplayName = "Minimal API";
            internal const string MinimalApiDescription = "Generates an endpoints file (with CRUD API endpoints) given a model and optional DbContext.";
            internal const string MinimalApiExample1 = "dotnet scaffold aspnet minimalapi --project C:/MyApiApp/MyApiApp.csproj --model Product --endpoints-class ProductEndpoints --data-context AppDbContext --database-provider SqlServer --openapi";
            internal const string MinimalApiExample1Description = "Generate minimal API endpoints with OpenAPI support:";
            internal const string MinimalApiExample2 = "dotnet scaffold aspnet minimalapi --project C:/MyApi/MyApi.csproj --model Order --endpoints-class OrderApi --typed-results --openapi";
            internal const string MinimalApiExample2Description = "Generate endpoints with TypedResults and OpenAPI:";

            internal const string ApiController = "apicontroller";
            internal const string ApiControllerDisplayName = "API Controller";
            internal const string ApiControllerDescription = "Add an empty API Controller to a given project";
            internal const string ApiControllerExample1 = "dotnet scaffold aspnet apicontroller --project C:/MyApiApp/MyApiApp.csproj --file-name ProductsController";
            internal const string ApiControllerExample1Description = "Create an empty API controller:";
            internal const string ApiControllerExample2 = "dotnet scaffold aspnet apicontroller --project C:/MyApp/MyApp.csproj --file-name UsersController --actions";
            internal const string ApiControllerExample2Description = "Create API controller with read/write actions:";

            internal const string ApiControllerCrud = "apicontroller-crud";
            internal const string ApiControllerCrudDisplayName = "API Controller with actions, using Entity Framework (CRUD)";
            internal const string ApiControllerCrudDescription = "Create an API controller with REST actions to create, read, update, delete, and list entities";
            internal const string ApiControllerCrudExample1 = "dotnet scaffold aspnet apicontroller-crud --project C:/MyApiApp/MyApiApp.csproj --model Product --controller-name ProductsController --data-context AppDbContext --database-provider SqlServer";
            internal const string ApiControllerCrudExample1Description = "Generate API controller with full CRUD operations:";
            internal const string ApiControllerCrudExample2 = "dotnet scaffold aspnet apicontroller-crud --project C:/MyApi/MyApi.csproj --model Customer --controller-name CustomersController --data-context ShopContext --database-provider PostgreSQL --prerelease";
            internal const string ApiControllerCrudExample2Description = "Generate controller with PostgreSQL using prerelease packages:";
        }

        internal class MVC
        {
            internal const string Controller = "mvccontroller";
            internal const string DisplayName = "MVC Controller";
            internal const string Description = "Add an empty MVC Controller to a given project";
            internal const string ControllerExample1 = "dotnet scaffold aspnet mvccontroller --project C:/MyMvcApp/MyMvcApp.csproj --file-name HomeController";
            internal const string ControllerExample1Description = "Create an empty MVC controller:";
            internal const string ControllerExample2 = "dotnet scaffold aspnet mvccontroller --project C:/MyApp/MyApp.csproj --file-name ProductController --actions";
            internal const string ControllerExample2Description = "Create MVC controller with read/write actions:";

            internal const string ControllerCrud = "mvccontroller-crud";
            internal const string CrudDisplayName = "MVC Controller with views, using Entity Framework (CRUD)";
            internal const string CrudDescription = "Create a MVC controller with read/write actions and views using Entity Framework";
            internal const string ControllerCrudExample1 = "dotnet scaffold aspnet mvccontroller-crud --project C:/MyMvcApp/MyMvcApp.csproj --model Product --controller-name ProductsController --data-context AppDbContext --database-provider SqlServer --views";
            internal const string ControllerCrudExample1Description = "Generate MVC controller with views and full CRUD:";
            internal const string ControllerCrudExample2 = "dotnet scaffold aspnet mvccontroller-crud --project C:/MyApp/MyApp.csproj --model Book --controller-name BooksController --data-context LibraryContext --database-provider SQLite --views --prerelease";
            internal const string ControllerCrudExample2Description = "Generate controller with SQLite and prerelease packages:";
        }

        internal class Area
        {
            internal const string Name = "area";
            internal const string DisplayName = "Area";
            internal const string Description = "Creates a MVC Area folder structure.";
            internal const string AreaExample = "dotnet scaffold aspnet area --project C:/MyMvcApp/MyMvcApp.csproj --area-name Admin";
            internal const string AreaExampleDescription = "Create an Admin area with folder structure:";
        }

        internal class Identity
        {
            internal const string Name = "identity";
            internal const string DisplayName = "ASP.NET Core Identity";
            internal const string Description = "Add ASP.NET Core identity to a project.";
            internal const string IdentityExample1 = "dotnet scaffold aspnet identity --project C:/MyWebApp/MyWebApp.csproj --database-provider SqlServer";
            internal const string IdentityExample1Description = "Add Identity with SQL Server:";
            internal const string IdentityExample2 = "dotnet scaffold aspnet identity --project C:/MyApp/MyApp.csproj --database-provider SQLite --overwrite";
            internal const string IdentityExample2Description = "Add Identity with SQLite, overwriting existing files:";
        }

        internal class EntraId
        {
            internal const string Name = "entra-id";
            internal const string DisplayName = "Entra ID";
            internal const string Description = "Add Entra auth";
            internal const string EntraIdExample1 = "dotnet scaffold aspnet entra-id --project C:/MyWebApp/MyWebApp.csproj --tenant-id your-tenant-id --use-existing-application true --application-id your-app-id";
            internal const string EntraIdExample1Description = "Add Microsoft Entra ID authentication using an existing Azure application:";
            internal const string EntraIdExample2 = "dotnet scaffold aspnet entra-id --project C:/MyWebApp/MyWebApp.csproj --tenant-id your-tenant-id --use-existing-application false";
            internal const string EntraIdExample2Description = "Add Microsoft Entra ID authentication by creating a new Azure application:";
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

            internal static class EndpointsClass
            {
                internal const string DisplayName = "Endpoints File Name";
                internal const string Description = "Name of the file containing the minimal API endpoints (without extension). The generated file will contain CRUD operations for the specified model.";
            }

            internal static class DataContextClass
            {
                internal const string DisplayName = "Data Context Class";
                internal const string Description = "Name of the DbContext class to use for data access. If the specified DbContext doesn't exist, a new one will be created with the necessary DbSet for the model.";
            }

            internal static class OpenApi
            {
                internal const string DisplayName = "Open API Enabled";
                internal const string Description = "Add OpenAPI/Swagger support to the generated endpoints, including endpoint descriptions and response types for API documentation.";
            }

            internal static class DatabaseProvider
            {
                internal const string DisplayName = "Database Provider";
                internal const string Description = "The Entity Framework database provider to use (e.g., SqlServer, PostgreSQL, SQLite, InMemory). Determines which EF Core provider packages will be added to the project.";
            }

            internal static class TypedResults
            {
                internal const string DisplayName = "Use Typed Results?";
                internal const string Description = "Use TypedResults for minimal API endpoints";
            }

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
                internal const string Description = "Overwrite existing Identity files if they already exist. Use --overwrite to enable, omit the flag to preserve existing files (default: false).";
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
                internal const string DisplayName = "Use Existing Application? (No = Create New)";
                internal const string Description = "Set to true to select an existing Azure application object, or false to create a new one.";
            }

            internal static class SelectApplication
            {
                internal const string DisplayName = "Select Existing Application";
                internal const string Description = "Select existing application";
            }
        }
    }
}
