// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.Web.CodeGeneration
{
    internal class MsBuildProjectStrings
    {
        public const string SkipReason = "CI doesn't have CLI version required for the MSBuild stuff to work";

        public static string GetNugetConfigTxt(string artifactsDir)
        {
            return @"
<configuration>
    <packageSources>
        <clear />
        <add key=""local"" value=""" + artifactsDir +  @""" />
        <add key=""AspNetCoreCiDev"" value=""https://dotnet.myget.org/F/aspnetcore-ci-dev/api/v3/index.json"" />
        <add key=""dotnet-core"" value=""https://dotnet.myget.org/F/dotnet-core/api/v3/index.json"" />
        <add key=""NuGet"" value=""https://api.nuget.org/v3/index.json"" />
    </packageSources>
</configuration>";
        }

        public const string RootProjectName = "Test.csproj";
        public const string RootProjectTxt = @"
<Project ToolsVersion=""15.0"" Sdk=""Microsoft.NET.Sdk.Web"">

  <PropertyGroup>
    <TargetFramework>netcoreapp1.1</TargetFramework>
    <RootNamespace>Microsoft.TestProject</RootNamespace>
    <ProjectName>TestProject</ProjectName>
  </PropertyGroup>

  <PropertyGroup>
    <PackageTargetFallback>$(PackageTargetFallback);portable-net45+win8+wp8+wpa81;</PackageTargetFallback>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""Microsoft.ApplicationInsights.AspNetCore"" Version=""2.0.0-beta1"" />
    <PackageReference Include=""Microsoft.AspNetCore"" Version=""1.2.0-preview1-*"" />
    <PackageReference Include=""Microsoft.AspNetCore.Mvc"" Version=""1.2.0-preview1-*"" />
    <PackageReference Include=""Microsoft.AspNetCore.StaticFiles"" Version=""1.2.0-preview1-*"" />
    <PackageReference Include=""Microsoft.Extensions.Logging.Debug"" Version=""1.2.0-preview1-*"" />
    <PackageReference Include=""Microsoft.VisualStudio.Web.BrowserLink"" Version=""1.2.0-preview1-*"" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include=""Microsoft.AspNetCore.Authentication.Cookies"" Version=""1.2.0-preview1-*"" />
    <PackageReference Include=""Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore"" Version=""1.2.0-preview1-*"" />
    <PackageReference Include=""Microsoft.AspNetCore.Identity.EntityFrameworkCore"" Version=""1.2.0-preview1-*"" />
    <PackageReference Include=""Microsoft.EntityFrameworkCore.Design"" Version=""1.2.0-preview1-*"" />
    <PackageReference Include=""Microsoft.EntityFrameworkCore.SqlServer"" Version=""1.2.0-preview1-*"" />
    <PackageReference Include=""Microsoft.EntityFrameworkCore.SqlServer.Design"" Version=""1.2.0-preview1-*"" />
    <PackageReference Include=""Microsoft.Extensions.Configuration.UserSecrets"" Version=""1.2.0-preview1-*"" />
    <PackageReference Include=""Microsoft.VisualStudio.Web.CodeGeneration.Design"" Version=""1.0.1-*"" />
    <PackageReference Include=""Microsoft.VisualStudio.Web.CodeGeneration.Tools"" Version=""1.0.1-*"" />
  </ItemGroup>
  <ItemGroup>
    <DotNetCliToolReference Include=""Microsoft.VisualStudio.Web.CodeGeneration.Tools"" Version=""1.0.1-*"" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include=""..\Library1\Library1.csproj"" />
  </ItemGroup>
  <ItemGroup>
    <Reference Include = ""xyz.dll"" />
  </ItemGroup>
</Project>
";

        public const string RootNet45ProjectTxt = @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <RootNamespace>Microsoft.TestProject</RootNamespace>
    <ProjectName>TestProject</ProjectName>
    <OutputType>EXE</OutputType>
    <TargetFrameworks>net452</TargetFrameworks>
    <OutputPath>bin\$(Configuration)</OutputPath>
    <RuntimeIdentifier>win7-x64</RuntimeIdentifier>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include=""**\*.cs"" Exclude=""Excluded.cs;$(DefaultItemExcludes)"" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include=""Microsoft.AspNetCore.Diagnostics"" Version=""1.2.0-preview1-*"" />
    <PackageReference Include=""Microsoft.AspNetCore.Mvc"" Version=""1.2.0-preview1-*"" />
    <PackageReference Include=""Microsoft.AspNetCore.Server.IISIntegration"" Version=""1.2.0-preview1-*"" />
    <PackageReference Include=""Microsoft.AspNetCore.Server.Kestrel"" Version=""1.2.0-preview1-*"" />
    <PackageReference Include=""Microsoft.AspNetCore.StaticFiles"" Version=""1.2.0-preview1-*"" />
    <PackageReference Include=""Microsoft.Extensions.Configuration.EnvironmentVariables"" Version=""1.2.0-preview1-*"" />
    <PackageReference Include=""Microsoft.Extensions.Configuration.Json"" Version=""1.2.0-preview1-*"" />
    <PackageReference Include=""Microsoft.Extensions.Logging"" Version=""1.2.0-preview1-*"" />
    <PackageReference Include=""Microsoft.Extensions.Logging.Console"" Version=""1.2.0-preview1-*"" />
    <PackageReference Include=""Microsoft.Extensions.Logging.Debug"" Version=""1.2.0-preview1-*"" />
    <PackageReference Include=""Microsoft.Extensions.Options.ConfigurationExtensions"" Version=""1.2.0-preview1-*"" />
    <PackageReference Include=""Microsoft.VisualStudio.Web.CodeGeneration.Design"" Version=""1.0.1-*"" />
    <DotNetCliToolReference Include=""Microsoft.VisualStudio.Web.CodeGeneration.Tools"" Version=""1.0.1-*"" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include=""..\Library1\Library1.csproj"" />
    <Reference Include=""System"" />
    <Reference Include=""System.Data"" />
    <Reference Include=""System.ComponentModel.DataAnnotations"" />
    <Reference Include=""Microsoft.CSharp"" />
  </ItemGroup>
  <ItemGroup>
    <Reference Include = ""xyz.dll"" />
  </ItemGroup>
</Project>
";

        public const string StartupTxt = @"using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace WebApplication1
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile(""appsettings.json"", optional: true, reloadOnChange: true);
                //.AddJsonFile($""appsettings.{env.EnvironmentName}.json"", optional: true);
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: ""default"",
                    template: ""{controller=Home}/{action=Index}/{id?}"");
            });
        }
    }
}
";
        public const string ProgramFileName = "Program.cs";
        public const string ProgramFileText = @"using System;
namespace Test
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine(""Hello"");
        }
    }
}";


        public const string LibraryProjectName = "Library1.csproj";
        public const string LibraryProjectTxt = @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <RootNamespace>Microsoft.Library</RootNamespace>
    <ProjectName>Library1</ProjectName>
    <OutputType>Library</OutputType>
    <TargetFrameworks>netstandard1.6;net451</TargetFrameworks>
    <OutputPath>bin\$(Configuration)</OutputPath>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include=""**\*.cs"" Exclude=""Excluded.cs;$(DefaultItemExcludes)"" />
  </ItemGroup>

  <ItemGroup Condition=""'$(TargetFramework)' == 'net451' "">
    <Reference Include=""System"" />
    <Reference Include=""System.Data"" />
  </ItemGroup>
</Project>
";


        public const string ProductTxt = @"
using System;
using System.Collections.Generic;
//using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication1.Models
{
    public class Product
    {
        public int ProductID { get; set; }
        public string Name { get; set; }
        public double Price { get; set; }
        public int ManufacturerID { get; set; }
        public Manufacturer Manufacturer { get; set; }
    }

    public class Manufacturer
    {
        public int ManufacturerID { get; set; }
//        [Required]
        public string Name { get; set; }
    }
}
";
        public const string CarTxt = @"
using System.Collections.Generic;

namespace Library1.Models
{
    public class Car
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public int ManufacturerID { get; set; }
        public Manufacturer Manufacturer { get; set; }
    }

    public class Manufacturer
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public virtual ICollection<Car> Cars { get; set; }
    }
}";
    }
}
