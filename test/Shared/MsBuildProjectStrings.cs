// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Xml;

namespace Microsoft.VisualStudio.Web.CodeGeneration
{
    internal class MsBuildProjectStrings
    {
        public static string GetNugetConfigTxt(string artifactsDir, string nugetConfigPath)
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.Load(nugetConfigPath);

            var root = xmlDoc.DocumentElement;
            var packageSources = root.SelectSingleNode("packageSources");
            var newSource = xmlDoc.CreateElement("add");
            newSource.SetAttribute("key", "local");
            newSource.SetAttribute("value", artifactsDir);
            packageSources.AppendChild(newSource);

            using (var sw = new StringWriter())
            {
                xmlDoc.WriteContentTo(new XmlTextWriter(sw));
                return sw.ToString();
            }
        }

        public const string RootProjectName = "Test.csproj";
        public const string RootProjectTxt = @"
<Project ToolsVersion=""15.0"" Sdk=""Microsoft.NET.Sdk.Web"">

  <PropertyGroup>
    <TargetFramework>netcoreapp2.0</TargetFramework>
    <RootNamespace>Microsoft.TestProject</RootNamespace>
    <ProjectName>TestProject</ProjectName>
    <NoWarn>NU1605</NoWarn>
    <RuntimeFrameworkVersion>{2}</RuntimeFrameworkVersion>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""Microsoft.ApplicationInsights.AspNetCore"" Version=""2.0.0-beta1"" />
    <PackageReference Include=""Microsoft.AspNetCore"" Version=""{0}"" />
    <PackageReference Include=""Microsoft.AspNetCore.Mvc"" Version=""{0}"" />
    <PackageReference Include=""Microsoft.AspNetCore.StaticFiles"" Version=""{0}"" />
    <PackageReference Include=""Microsoft.Extensions.Logging.Debug"" Version=""{0}"" />
    <PackageReference Include=""Microsoft.VisualStudio.Web.BrowserLink"" Version=""{0}"" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include=""Microsoft.AspNetCore.Authentication.Cookies"" Version=""{0}"" />
    <PackageReference Include=""Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore"" Version=""{0}"" />
    <PackageReference Include=""Microsoft.AspNetCore.Identity.EntityFrameworkCore"" Version=""{0}"" />
    <PackageReference Include=""Microsoft.EntityFrameworkCore.Design"" Version=""{0}"" />
    <PackageReference Include=""Microsoft.EntityFrameworkCore.SqlServer"" Version=""{0}"" />
    <PackageReference Include=""Microsoft.Extensions.Configuration.UserSecrets"" Version=""{0}"" />
    <PackageReference Include=""Microsoft.VisualStudio.Web.CodeGeneration.Design"" Version=""{1}"" />
    <PackageReference Include=""Microsoft.VisualStudio.Web.CodeGeneration.Tools"" Version=""{1}"" />
  </ItemGroup>
  <ItemGroup>
    <DotNetCliToolReference Include=""Microsoft.VisualStudio.Web.CodeGeneration.Tools"" Version=""{1}"" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include=""..\Library1\Library1.csproj"" />
  </ItemGroup>
  <ItemGroup>
    <Reference Include = ""xyz.dll"" />
  </ItemGroup>
</Project>
";

public const string RootProjectTxtWithoutEF = @"
<Project ToolsVersion=""15.0"" Sdk=""Microsoft.NET.Sdk.Web"">

  <PropertyGroup>
    <TargetFramework>netcoreapp2.0</TargetFramework>
    <RootNamespace>Microsoft.TestProject</RootNamespace>
    <ProjectName>TestProject</ProjectName>
    <NoWarn>NU1605</NoWarn>
    <RuntimeFrameworkVersion>{2}</RuntimeFrameworkVersion>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""Microsoft.ApplicationInsights.AspNetCore"" Version=""2.0.0-beta1"" />
    <PackageReference Include=""Microsoft.AspNetCore"" Version=""{0}"" />
    <PackageReference Include=""Microsoft.AspNetCore.Mvc"" Version=""{0}"" />
    <PackageReference Include=""Microsoft.AspNetCore.StaticFiles"" Version=""{0}"" />
    <PackageReference Include=""Microsoft.Extensions.Logging.Debug"" Version=""{0}"" />
    <PackageReference Include=""Microsoft.VisualStudio.Web.BrowserLink"" Version=""{0}"" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include=""Microsoft.AspNetCore.Authentication.Cookies"" Version=""{0}"" />
    <PackageReference Include=""Microsoft.Extensions.Configuration.UserSecrets"" Version=""{0}"" />
    <PackageReference Include=""Microsoft.VisualStudio.Web.CodeGeneration.Design"" Version=""{1}"" />
    <PackageReference Include=""Microsoft.VisualStudio.Web.CodeGeneration.Tools"" Version=""{1}"" />
  </ItemGroup>
  <ItemGroup>
    <DotNetCliToolReference Include=""Microsoft.VisualStudio.Web.CodeGeneration.Tools"" Version=""{1}"" />
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
    <TargetFrameworks>net461</TargetFrameworks>
    <OutputPath>bin\$(Configuration)</OutputPath>
    <RuntimeIdentifier>win7-x64</RuntimeIdentifier>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <NoWarn>NU1605</NoWarn>
    <RuntimeFrameworkVersion>{2}</RuntimeFrameworkVersion>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include=""**\*.cs"" Exclude=""Excluded.cs;$(DefaultItemExcludes)"" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include=""Microsoft.AspNetCore.Diagnostics"" Version=""{0}"" />
    <PackageReference Include=""Microsoft.AspNetCore.Mvc"" Version=""{0}"" />
    <PackageReference Include=""Microsoft.AspNetCore.Server.IISIntegration"" Version=""{0}"" />
    <PackageReference Include=""Microsoft.AspNetCore.Server.Kestrel"" Version=""{0}"" />
    <PackageReference Include=""Microsoft.AspNetCore.StaticFiles"" Version=""{0}"" />
    <PackageReference Include=""Microsoft.Extensions.Configuration.EnvironmentVariables"" Version=""{0}"" />
    <PackageReference Include=""Microsoft.Extensions.Configuration.Json"" Version=""{0}"" />
    <PackageReference Include=""Microsoft.Extensions.Logging"" Version=""{0}"" />
    <PackageReference Include=""Microsoft.Extensions.Logging.Console"" Version=""{0}"" />
    <PackageReference Include=""Microsoft.Extensions.Logging.Debug"" Version=""{0}"" />
    <PackageReference Include=""Microsoft.Extensions.Options.ConfigurationExtensions"" Version=""{0}"" />
    <PackageReference Include=""Microsoft.VisualStudio.Web.CodeGeneration.Design"" Version=""{1}"" />
    <DotNetCliToolReference Include=""Microsoft.VisualStudio.Web.CodeGeneration.Tools"" Version=""{1}"" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include=""..\Library1\Library1.csproj"" />
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
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseStaticFiles();
            CS7_method(out var i);
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: ""default"",
                    template: ""{controller=Home}/{action=Index}/{id?}"");
            });
        }

        private void CS7_method(out int i)
        {
            i = 1;
        }
    }
}
";

public const string StartupTxtWithoutEf = @"using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace WebApplication1
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost
                .CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .Build();
    }
}
";


        public const string LibraryProjectName = "Library1.csproj";
        public const string LibraryProjectTxt = @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <RootNamespace>Microsoft.Library</RootNamespace>
    <ProjectName>Library1</ProjectName>
    <OutputType>Library</OutputType>
    <TargetFramework>netstandard2.0</TargetFramework>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include=""**\*.cs"" Exclude=""Excluded.cs;$(DefaultItemExcludes)"" />
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
        public string ID { get; set; }
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
