// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Xml;

namespace Microsoft.VisualStudio.Web.CodeGeneration
{
    internal class MsBuildProjectStrings
    {
        public const string RootProjectName = "Test.csproj";
        public const string RootProjectTxt = @"
<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <Import Project=""$(MSBuildThisFileDirectory)\TestCodeGeneration.targets"" Condition=""Exists('$(MSBuildThisFileDirectory)\TestCodeGeneration.targets')"" />

  <PropertyGroup>
    <RestoreSources>;
      https://dotnet.myget.org/F/dotnet-core/api/v3/index.json;
      https://dotnet.myget.org/F/aspnetcore-dev/api/v3/index.json;
      https://dotnet.myget.org/F/aspnetcore-tools/api/v3/index.json;
      https://pkgs.dev.azure.com/dnceng/public/_packaging/3.0.100-rc2-014277/nuget/v3/index.json
    ;
      https://api.nuget.org/v3/index.json;
    ;
        C:\ScaffoldingArcade\Scaffolding\test\..\artifacts\build;</RestoreSources>
    <TargetFramework>netcoreapp3.0</TargetFramework>
    <RootNamespace>Microsoft.TestProject</RootNamespace>
    <ProjectName>TestProject</ProjectName>
    <RuntimeFrameworkVersion Condition=""'$(TargetFramework)'=='netcoreapp3.0'"">3.0.0-preview1-26907-05</RuntimeFrameworkVersion>
    <!-- aspnet/BuildTools#662 Don't police what version of NetCoreApp we use -->
    <NETCoreAppMaximumVersion>99.9</NETCoreAppMaximumVersion>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""Microsoft.ApplicationInsights.AspNetCore"" Version=""2.6.1"" />
    <PackageReference Include=""Microsoft.AspNetCore"" Version=""3.0.0-alpha1-10584"" />
    <PackageReference Include=""Microsoft.AspNetCore.Identity.UI"" Version=""3.0.0"" />
    <PackageReference Include=""Microsoft.AspNetCore.Mvc"" Version=""3.0.0-alpha1-10584"" />
    <PackageReference Include=""Microsoft.AspNetCore.StaticFiles"" Version=""3.0.0-alpha1-10584"" />
    <PackageReference Include=""Microsoft.Extensions.Identity.Stores"" Version=""3.0.0"" />
    <PackageReference Include=""Microsoft.Extensions.Logging.Debug"" Version=""3.0.0"" />
    <PackageReference Include=""Microsoft.NET.Sdk.Razor"" Version=""3.0.0-preview5.19227.1"" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include=""Microsoft.AspNetCore.Authentication.Cookies"" Version=""3.0.0-alpha1-10584"" />
    <PackageReference Include=""Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore"" Version=""3.0.0-preview5-19227-01"" />
    <PackageReference Include=""Microsoft.AspNetCore.Identity.EntityFrameworkCore"" Version=""3.0.0-preview5-19227-01"" />
    <PackageReference Include=""Microsoft.EntityFrameworkCore.Design"" Version=""3.0.0-preview6.19304.10"" />
    <PackageReference Include=""Microsoft.EntityFrameworkCore.SqlServer"" Version=""3.0.0-preview5.19227.1"" />
    <PackageReference Include=""Microsoft.Extensions.Configuration.UserSecrets"" Version=""3.0.0"" />
    <PackageReference Include=""Microsoft.VisualStudio.Web.CodeGeneration.Design"" Version=""5.0.0-alpha1-t000"" />
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
  <Import Project=""$(MSBuildThisFileDirectory)\TestCodeGeneration.targets"" Condition=""Exists('$(MSBuildThisFileDirectory)\TestCodeGeneration.targets')"" />

  <PropertyGroup>
    <RestoreSources>;
      https://dotnet.myget.org/F/dotnet-core/api/v3/index.json;
      https://dotnet.myget.org/F/aspnetcore-dev/api/v3/index.json;
      https://dotnet.myget.org/F/aspnetcore-tools/api/v3/index.json;
      https://pkgs.dev.azure.com/dnceng/public/_packaging/3.0.100-rc2-014277/nuget/v3/index.json
    ;
      https://api.nuget.org/v3/index.json;
    ;
        C:\ScaffoldingArcade\Scaffolding\test\..\artifacts\build;</RestoreSources>
    <TargetFramework>netcoreapp3.0</TargetFramework>
    <RootNamespace>Microsoft.TestProject</RootNamespace>
    <ProjectName>TestProject</ProjectName>
    <RuntimeFrameworkVersion Condition=""'$(TargetFramework)'=='netcoreapp3.0'"">3.0.0-preview1-26907-05</RuntimeFrameworkVersion>
    <!-- aspnet/BuildTools#662 Don't police what version of NetCoreApp we use -->
    <NETCoreAppMaximumVersion>99.9</NETCoreAppMaximumVersion>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""Microsoft.ApplicationInsights.AspNetCore"" Version=""2.6.1"" />
    <PackageReference Include=""Microsoft.AspNetCore"" Version=""3.0.0-alpha1-10584"" />
    <PackageReference Include=""Microsoft.AspNetCore.Mvc"" Version=""3.0.0-alpha1-10584"" />
    <PackageReference Include=""Microsoft.AspNetCore.StaticFiles"" Version=""3.0.0-alpha1-10584"" />
    <PackageReference Include=""Microsoft.Extensions.Identity.Stores"" Version=""3.0.0"" />
    <PackageReference Include=""Microsoft.Extensions.Logging.Debug"" Version=""3.0.0"" />
    <PackageReference Include=""Microsoft.NET.Sdk.Razor"" Version=""3.0.0-preview5.19227.1"" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include=""Microsoft.AspNetCore.Authentication.Cookies"" Version=""3.0.0-alpha1-10584"" />
    <PackageReference Include=""Microsoft.Extensions.Configuration.UserSecrets"" Version=""3.0.0"" />
    <PackageReference Include=""Microsoft.VisualStudio.Web.CodeGeneration.Design"" Version=""5.0.0-alpha1-t000"" />
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
  <Import Project=""$(MSBuildThisFileDirectory)\TestCodeGeneration.targets"" Condition=""Exists('$(MSBuildThisFileDirectory)\TestCodeGeneration.targets')"" />

  <PropertyGroup>
    <RestoreSources>;
      https://dotnet.myget.org/F/dotnet-core/api/v3/index.json;
      https://dotnet.myget.org/F/aspnetcore-dev/api/v3/index.json;
      https://dotnet.myget.org/F/aspnetcore-tools/api/v3/index.json;
      https://pkgs.dev.azure.com/dnceng/public/_packaging/3.0.100-rc2-014277/nuget/v3/index.json
    ;
      https://api.nuget.org/v3/index.json;
    ;
        C:\ScaffoldingArcade\Scaffolding\test\..\artifacts\build;</RestoreSources>
    <RootNamespace>Microsoft.TestProject</RootNamespace>
    <ProjectName>TestProject</ProjectName>
    <OutputType>EXE</OutputType>
    <TargetFrameworks>net5.0</TargetFrameworks>
    <OutputPath>bin\$(Configuration)</OutputPath>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include=""**\*.cs"" Exclude=""Excluded.cs;$(DefaultItemExcludes)"" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include=""Microsoft.AspNetCore.Diagnostics"" Version="""" />
    <PackageReference Include=""Microsoft.AspNetCore.Mvc"" Version=""3.0.0-alpha1-10584"" />
    <PackageReference Include=""Microsoft.AspNetCore.Server.IISIntegration"" Version=""3.0.0-preview4-19123-01"" />
    <PackageReference Include=""Microsoft.AspNetCore.Server.Kestrel"" Version=""3.0.0-preview4-19123-01"" />
    <PackageReference Include=""Microsoft.AspNetCore.StaticFiles"" Version=""3.0.0-alpha1-10584"" />
    <PackageReference Include=""Microsoft.Extensions.Configuration.EnvironmentVariables"" Version=""3.0.0"" />
    <PackageReference Include=""Microsoft.Extensions.Configuration.Json"" Version=""3.0.0"" />
    <PackageReference Include=""Microsoft.Extensions.Identity.Stores"" Version=""3.0.0"" />
    <PackageReference Include=""Microsoft.Extensions.Logging"" Version=""3.0.0"" />
    <PackageReference Include=""Microsoft.Extensions.Logging.Console"" Version=""3.0.0"" />
    <PackageReference Include=""Microsoft.Extensions.Logging.Debug"" Version=""3.0.0"" />
    <PackageReference Include=""Microsoft.Extensions.Options.ConfigurationExtensions"" Version=""3.0.0"" />
    <PackageReference Include=""Microsoft.VisualStudio.Web.CodeGeneration.Design"" Version=""5.0.0-alpha1-t000"" />
    <DotNetCliToolReference Include=""Microsoft.VisualStudio.Web.CodeGeneration.Tools"" Version=""5.0.0-alpha1-t000"" />
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
    <RestoreSources>;
      https://dotnet.myget.org/F/dotnet-core/api/v3/index.json;
      https://dotnet.myget.org/F/aspnetcore-dev/api/v3/index.json;
      https://dotnet.myget.org/F/aspnetcore-tools/api/v3/index.json;
      https://pkgs.dev.azure.com/dnceng/public/_packaging/3.0.100-rc2-014277/nuget/v3/index.json
    ;
      https://api.nuget.org/v3/index.json;
    ;
        C:\ScaffoldingArcade\Scaffolding\test\..\artifacts\build;</RestoreSources>
    <RootNamespace>Microsoft.Library</RootNamespace>
    <ProjectName>Library1</ProjectName>
    <OutputType>Library</OutputType>
    <TargetFramework>netstandard2.0</TargetFramework>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include=""**\*.cs"" Exclude=""Excluded.cs;$(DefaultItemExcludes)"" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include=""System.ComponentModel.Annotations"" Version=""4.4.1"" />
  </ItemGroup>

</Project>
";


        public const string ProductTxt = @"
using System;
using System.Collections.Generic;
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
using System.ComponentModel.DataAnnotations;

namespace Library1.Models
{
    public class Car
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public int ManufacturerID { get; set; }
        public Manufacturer Manufacturer { get; set; }
        [DataType(DataType.MultilineText)]
        public string Notes { get; set; }
    }

    public class Manufacturer
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public virtual ICollection<Car> Cars { get; set; }
    }
}";

// Strings for 3 layered project
    public const string WebProjectTxt = @"
<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <Import Project=""$(MSBuildThisFileDirectory)\TestCodeGeneration.targets"" Condition=""Exists('$(MSBuildThisFileDirectory)\TestCodeGeneration.targets')"" />

  <PropertyGroup>
    <RestoreSources>;
      https://dotnet.myget.org/F/dotnet-core/api/v3/index.json;
      https://dotnet.myget.org/F/aspnetcore-dev/api/v3/index.json;
      https://dotnet.myget.org/F/aspnetcore-tools/api/v3/index.json;
      https://pkgs.dev.azure.com/dnceng/public/_packaging/3.0.100-rc2-014277/nuget/v3/index.json
    ;
      https://api.nuget.org/v3/index.json;
    ;
        C:\ScaffoldingArcade\Scaffolding\test\..\artifacts\build;</RestoreSources>
    <TargetFramework>netcoreapp3.0</TargetFramework>
    <RootNamespace>Microsoft.TestProject</RootNamespace>
    <ProjectName>TestProject</ProjectName>
    <NoWarn>NU1605</NoWarn>
    <RuntimeFrameworkVersion Condition=""'$(TargetFramework)'=='netcoreapp3.0'"">3.0.0-preview1-26907-05</RuntimeFrameworkVersion>
    <!-- aspnet/BuildTools#662 Don't police what version of NetCoreApp we use -->
    <NETCoreAppMaximumVersion>99.9</NETCoreAppMaximumVersion>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""Microsoft.ApplicationInsights.AspNetCore"" Version=""2.6.1"" />
    <PackageReference Include=""Microsoft.AspNetCore"" Version=""3.0.0-alpha1-10584"" />
    <PackageReference Include=""Microsoft.AspNetCore.Mvc"" Version=""3.0.0-alpha1-10584"" />
    <PackageReference Include=""Microsoft.AspNetCore.StaticFiles"" Version=""3.0.0-alpha1-10584"" />
    <PackageReference Include=""Microsoft.Extensions.Identity.Stores"" Version=""3.0.0"" />
    <PackageReference Include=""Microsoft.Extensions.Logging.Debug"" Version=""3.0.0"" />
    <PackageReference Include=""Microsoft.NET.Sdk.Razor"" Version=""3.0.0-preview5.19227.1"" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include=""Microsoft.AspNetCore.Authentication.Cookies"" Version=""3.0.0-alpha1-10584"" />
    <PackageReference Include=""Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore"" Version=""3.0.0-preview5-19227-01"" />
    <PackageReference Include=""Microsoft.AspNetCore.Identity.EntityFrameworkCore"" Version=""3.0.0-preview5-19227-01"" />
    <PackageReference Include=""Microsoft.EntityFrameworkCore.Design"" Version=""3.0.0-preview6.19304.10"" />
    <PackageReference Include=""Microsoft.EntityFrameworkCore.SqlServer"" Version=""3.0.0-preview5.19227.1"" />
    <PackageReference Include=""Microsoft.Extensions.Configuration.UserSecrets"" Version=""3.0.0"" />
    <PackageReference Include=""Microsoft.VisualStudio.Web.CodeGeneration.Design"" Version=""5.0.0-alpha1-t000"" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include=""..\DAL\DAL.csproj"" />
  </ItemGroup>
  <ItemGroup>
    <Reference Include = ""xyz.dll"" />
  </ItemGroup>
</Project>
";

        public const string ModelsLibraryProjectName = "ModelsLibrary.csproj";
        public const string ModelsLibrary = @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <RestoreSources>;
      https://dotnet.myget.org/F/dotnet-core/api/v3/index.json;
      https://dotnet.myget.org/F/aspnetcore-dev/api/v3/index.json;
      https://dotnet.myget.org/F/aspnetcore-tools/api/v3/index.json;
      https://pkgs.dev.azure.com/dnceng/public/_packaging/3.0.100-rc2-014277/nuget/v3/index.json
    ;
      https://api.nuget.org/v3/index.json;
    ;
        C:\ScaffoldingArcade\Scaffolding\test\..\artifacts\build;</RestoreSources>

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

        public const string StartupWithDbContext = @"using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
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
            services.AddDbContext<DAL.CarContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString(""DefaultConnection"")));
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

        public const string AppSettingsFileName = "appsettings.json";
        public const string AppSettingsFileTxt = @"{
  ""ConnectionStrings"": {
    ""DefaultConnection"": ""Server=(localdb)\\mssqllocaldb;Database=aspnet-WebApplication1-78A9B4F9-2AA6-4744-B84C-38DEE41813F8;Trusted_Connection=True;MultipleActiveResultSets=true""
  },
  ""Logging"": {
    ""IncludeScopes"": false,
    ""LogLevel"": {
      ""Default"": ""Warning""
    }
  }
}
";

        public const string DALProjectName = "DAL.csproj";
        public const string DAL = @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <RestoreSources>;
      https://dotnet.myget.org/F/dotnet-core/api/v3/index.json;
      https://dotnet.myget.org/F/aspnetcore-dev/api/v3/index.json;
      https://dotnet.myget.org/F/aspnetcore-tools/api/v3/index.json;
      https://pkgs.dev.azure.com/dnceng/public/_packaging/3.0.100-rc2-014277/nuget/v3/index.json
    ;
      https://api.nuget.org/v3/index.json;
    ;
        C:\ScaffoldingArcade\Scaffolding\test\..\artifacts\build;</RestoreSources>

    <RootNamespace>Microsoft.Library</RootNamespace>
    <ProjectName>Library1</ProjectName>
    <OutputType>Library</OutputType>
    <TargetFramework>netstandard2.0</TargetFramework>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include=""**\*.cs"" Exclude=""Excluded.cs;$(DefaultItemExcludes)"" />
    <PackageReference Include=""Microsoft.EntityFrameworkCore"" Version=""3.0.0-preview6.19304.10"" />
    <ProjectReference Include=""..\Library1\Library1.csproj"" />
  </ItemGroup>
</Project>
";

        public const string CarContextFileName = "CarContext.cs";
        public const string CarContextTxt = @"
using System;
using Microsoft.EntityFrameworkCore;
using Library1.Models;

namespace DAL
{
    public class CarContext : DbContext
    {
        public CarContext(DbContextOptions<CarContext> options)
          : base(options)
          {

          }

          public DbSet<Car> Cars { get; set; }
    }
}
";

        public const string RootProjectForIdentityScaffolderText = @"
<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <Import Project=""$(MSBuildThisFileDirectory)\TestCodeGeneration.targets"" Condition=""Exists('$(MSBuildThisFileDirectory)\TestCodeGeneration.targets')"" />

  <PropertyGroup>
    <RestoreSources>;
      https://dotnet.myget.org/F/dotnet-core/api/v3/index.json;
      https://dotnet.myget.org/F/aspnetcore-dev/api/v3/index.json;
      https://dotnet.myget.org/F/aspnetcore-tools/api/v3/index.json;
      https://pkgs.dev.azure.com/dnceng/public/_packaging/3.0.100-rc2-014277/nuget/v3/index.json
    ;
      https://api.nuget.org/v3/index.json;
    ;
        C:\ScaffoldingArcade\Scaffolding\test\..\artifacts\build;</RestoreSources>
    <TargetFramework>netcoreapp3.0</TargetFramework>
    <RootNamespace>Microsoft.TestProject</RootNamespace>
    <ProjectName>TestProject</ProjectName>
    <NoWarn>NU1605</NoWarn>
    <RuntimeFrameworkVersion Condition=""'$(TargetFramework)'=='netcoreapp3.0'"">3.0.0-preview1-26907-05</RuntimeFrameworkVersion>
    <!-- aspnet/BuildTools#662 Don't police what version of NetCoreApp we use -->
    <NETCoreAppMaximumVersion>99.9</NETCoreAppMaximumVersion>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""Microsoft.ApplicationInsights.AspNetCore"" Version=""2.6.1"" />
    <PackageReference Include=""Microsoft.AspNetCore"" Version=""3.0.0-alpha1-10584"" />
    <PackageReference Include=""Microsoft.AspNetCore.Mvc"" Version=""3.0.0-alpha1-10584"" />
    <PackageReference Include=""Microsoft.AspNetCore.StaticFiles"" Version=""3.0.0-alpha1-10584"" />
    <PackageReference Include=""Microsoft.Extensions.Identity.Stores"" Version=""3.0.0"" />
    <PackageReference Include=""Microsoft.Extensions.Logging.Debug"" Version=""3.0.0"" />
    <PackageReference Include=""Microsoft.NET.Sdk.Razor"" Version=""3.0.0-preview5.19227.1"" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include=""Microsoft.AspNetCore.Authentication.Cookies"" Version=""3.0.0-alpha1-10584"" />
    <PackageReference Include=""Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore"" Version=""3.0.0-preview5-19227-01"" />
    <PackageReference Include=""Microsoft.AspNetCore.Identity.EntityFrameworkCore"" Version=""3.0.0-preview5-19227-01"" />
    <PackageReference Include=""Microsoft.EntityFrameworkCore.Design"" Version=""3.0.0-preview6.19304.10"" />
    <PackageReference Include=""Microsoft.EntityFrameworkCore.SqlServer"" Version=""3.0.0-preview5.19227.1"" />
    <PackageReference Include=""Microsoft.Extensions.Configuration.UserSecrets"" Version=""3.0.0"" />
    <PackageReference Include=""Microsoft.VisualStudio.Web.CodeGeneration.Design"" Version=""5.0.0-alpha1-t000"" />
  </ItemGroup>

</Project>
";

        public const string IdentityContextName = "MyApplicationDbContext.cs";
        public const string IdentityContextText = @"
using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Test.Data
{
    public class MyApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public MyApplicationDbContext(DbContextOptions<MyApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Customize the ASP.NET Identity model and override the defaults if needed.
            // For example, you can rename the ASP.NET Identity table names and more.
            // Add your customizations after calling base.OnModelCreating(builder);
        }
    }
}
";

      public const string TestCodeGenerationTargetFileText = @"<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
<!--
**********************************************************************************
Target: EvaluateProjectInfoForCodeGeneration

Outputs the Project Information needed for CodeGeneration to a file.

**********************************************************************************
-->

  <PropertyGroup>
    <EvaluateProjectInfoForCodeGenerationDependsOn>
      $(EvaluateProjectInfoForCodeGenerationDependsOn);
      ResolveReferences;
      ResolvePackageDependenciesDesignTime;
    </EvaluateProjectInfoForCodeGenerationDependsOn>
  </PropertyGroup>
  <Choose>
    <!-- For Portable Msbuild, MSBuildRuntimeType = 'Core'. For Desktop MsBuild, MSBuildRuntimeType = 'Full'-->
    <When Condition=""'$(MSBuildRuntimeType)' == 'Core'"">
      <PropertyGroup>
        <EvaluateProjectInfoForCodeGenerationAssemblyPath>$(MSBuildThisFileDirectory)..\toolAssets\netstandard2.0\Microsoft.VisualStudio.Web.CodeGeneration.Msbuild.dll</EvaluateProjectInfoForCodeGenerationAssemblyPath>
      </PropertyGroup>
    </When>
    <Otherwise>
      <PropertyGroup >
        <EvaluateProjectInfoForCodeGenerationAssemblyPath>$(MSBuildThisFileDirectory)..\toolAssets\netcoreapp5.0\Microsoft.VisualStudio.Web.CodeGeneration.Msbuild.dll</EvaluateProjectInfoForCodeGenerationAssemblyPath>
      </PropertyGroup>
    </Otherwise>
  </Choose>

  <UsingTask TaskName=""ProjectContextWriter""
             AssemblyFile=""$(EvaluateProjectInfoForCodeGenerationAssemblyPath)"" />

  <Target Name=""EvaluateProjectInfoForCodeGeneration"" DependsOnTargets=""$(EvaluateProjectInfoForCodeGenerationDependsOn)"">

    <ProjectContextWriter TargetFramework =""$(TargetFramework)""
                          OutputFile =""$(OutputFile)""
                          Name =""$(ProjectName)""
                          ResolvedReferences =""@(ReferencePath)""
                          PackageDependencies =""@(_DependenciesDesignTime)""
                          ProjectReferences =""@(ProjectReference)""
                          AssemblyFullPath =""$(TargetPath)""
                          OutputType=""$(OutputType)""
                          Platform=""$(Platform)""
                          RootNameSpace =""$(RootNamespace)""
                          CompilationItems =""@(Compile)""
                          TargetDirectory=""$(TargetDir)""
                          EmbeddedItems=""@(EmbeddedResource)""
                          Configuration=""$(Configuration)""
                          ProjectFullPath=""$(MSBuildProjectFullPath)""
                          ProjectDepsFileName=""$(ProjectDepsFileName)""
                          ProjectRuntimeConfigFileName=""$(ProjectRuntimeConfigFileName)""/>

  </Target>
</Project>
";

        public const string StartupForIdentityTxt = @"using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Test.Data;

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
            services.AddDbContext<MyApplicationDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString(""DefaultConnection"")));
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
        public const string IdentityUserName = "MyIdentityUser.cs";
        public const string IdentityUserText = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace Test.Data
{
    public class MyIdentityUser : IdentityUser
    {

    }
}
";

// Strings for model property defined in db context base class
        public const string DbContextInheritanceProgramName = "Program.cs";
        public const string DbContextInheritanceProjectProgramText = @"using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace Test
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

    public const string DbContextInheritanceProjectTxt = @"
<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <Import Project=""$(MSBuildThisFileDirectory)\TestCodeGeneration.targets"" Condition=""Exists('$(MSBuildThisFileDirectory)\TestCodeGeneration.targets')"" />

  <PropertyGroup>
    <RestoreSources>;
      https://dotnet.myget.org/F/dotnet-core/api/v3/index.json;
      https://dotnet.myget.org/F/aspnetcore-dev/api/v3/index.json;
      https://dotnet.myget.org/F/aspnetcore-tools/api/v3/index.json;
      https://pkgs.dev.azure.com/dnceng/public/_packaging/3.0.100-rc2-014277/nuget/v3/index.json
    ;
      https://api.nuget.org/v3/index.json;
    ;
        C:\ScaffoldingArcade\Scaffolding\test\..\artifacts\build;</RestoreSources>
    <TargetFramework>netcoreapp3.0</TargetFramework>
    <RootNamespace>Microsoft.Test</RootNamespace>
    <ProjectName>Test</ProjectName>
    <NoWarn>NU1605</NoWarn>
    <RuntimeFrameworkVersion Condition=""'$(TargetFramework)'=='netcoreapp3.0'"">3.0.0-preview1-26907-05</RuntimeFrameworkVersion>
    <!-- aspnet/BuildTools#662 Don't police what version of NetCoreApp we use -->
    <NETCoreAppMaximumVersion>99.9</NETCoreAppMaximumVersion>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""Microsoft.AspNetCore"" Version=""3.0.0-alpha1-10584"" />
    <PackageReference Include=""Microsoft.AspNetCore.Mvc"" Version=""3.0.0-alpha1-10584"" />
    <PackageReference Include=""Microsoft.AspNetCore.StaticFiles"" Version=""3.0.0-alpha1-10584"" />
    <PackageReference Include=""Microsoft.Extensions.Identity.Stores"" Version=""3.0.0"" />
    <PackageReference Include=""Microsoft.EntityFrameworkCore.Design"" Version=""3.0.0-preview6.19304.10"" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include=""Microsoft.EntityFrameworkCore.SqlServer"" Version=""3.0.0-preview5.19227.1"" />
    <PackageReference Include=""Microsoft.EntityFrameworkCore.Tools"" Version=""3.0.0-preview6.19304.10"">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
    </PackageReference>

    <PackageReference Include=""Microsoft.Extensions.Configuration.UserSecrets"" Version=""3.0.0"" />
    <PackageReference Include=""Microsoft.VisualStudio.Web.CodeGeneration.Design"" Version=""5.0.0-alpha1-t000"" />
  </ItemGroup>
  <ItemGroup>
    <Folder Include=""Controllers\"" />
  </ItemGroup>
</Project>
";

        public const string BaseDbContextText = @"
using Microsoft.EntityFrameworkCore;
using Test.Models;

namespace Test.Data
{
    public class BaseDbContext : DbContext
    {
        public BaseDbContext(DbContextOptions<BaseDbContext> options)
            : base(options)
        {
        }

        public DbSet<Blog> Base_Blogs { get; set; }
    }
}
";
        public const string DerivedDbContextFileName = "DerivedDbContext.cs";
        public const string DerivedDbContextText = @"
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Test.Data
{
    public class DerivedDbContext : BaseDbContext
    {
        public DerivedDbContext(DbContextOptions<DerivedDbContext> options)
            : base(new DbContextOptions<BaseDbContext>(options.Extensions.ToDictionary(x => x.GetType(), x => x)))
        {
        }

        public DerivedDbContext(DbContextOptions<BaseDbContext> options)
            : base(options)
        {
        }
    }
}
";

        public const string DerivedContextTestStartupText = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Test.Models;
using Test.Data;

namespace Test
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
            services.AddMvc();

            services.AddDbContext<DerivedDbContext>(options =>
                    options.UseSqlServer(Configuration.GetConnectionString(""DefaultConnection"")));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseStaticFiles();
            app.UseMvc();
        }
    }
}
";

        public const string BlogModelText = @"
namespace Test.Models
{
    public class Blog
    {
        public int Id { get; set; }
        public string User { get; set; }
        public string Message { get; set; }
    }
}
";
      public const string DbContextInheritanceTestCodeGenerationTargetFileText = @"<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
<!--
**********************************************************************************
Target: EvaluateProjectInfoForCodeGeneration

Outputs the Project Information needed for CodeGeneration to a file.

**********************************************************************************
-->

  <PropertyGroup>
    <EvaluateProjectInfoForCodeGenerationDependsOn>
      $(EvaluateProjectInfoForCodeGenerationDependsOn);
      ResolveReferences;
      ResolvePackageDependenciesDesignTime;
    </EvaluateProjectInfoForCodeGenerationDependsOn>
  </PropertyGroup>
  <Choose>
    <!-- For Portable Msbuild, MSBuildRuntimeType = 'Core'. For Desktop MsBuild, MSBuildRuntimeType = 'Full'-->
    <When Condition=""'$(MSBuildRuntimeType)' == 'Core'"">
      <PropertyGroup>
        <EvaluateProjectInfoForCodeGenerationAssemblyPath>$(MSBuildThisFileDirectory)\toolAssets\netcoreapp2.0\Microsoft.VisualStudio.Web.CodeGeneration.Msbuild.dll</EvaluateProjectInfoForCodeGenerationAssemblyPath>
      </PropertyGroup>
    </When>
    <Otherwise>
      <PropertyGroup >
        <EvaluateProjectInfoForCodeGenerationAssemblyPath>$(MSBuildThisFileDirectory)\toolAssets\netcoreapp5.0\Microsoft.VisualStudio.Web.CodeGeneration.Msbuild.dll</EvaluateProjectInfoForCodeGenerationAssemblyPath>
      </PropertyGroup>
    </Otherwise>
  </Choose>

  <UsingTask TaskName=""ProjectContextWriter""
             AssemblyFile=""$(EvaluateProjectInfoForCodeGenerationAssemblyPath)"" />

  <Target Name=""EvaluateProjectInfoForCodeGeneration"" DependsOnTargets=""$(EvaluateProjectInfoForCodeGenerationDependsOn)"">

    <ProjectContextWriter TargetFramework =""$(TargetFramework)""
                          OutputFile =""$(OutputFile)""
                          Name =""$(ProjectName)""
                          ResolvedReferences =""@(ReferencePath)""
                          PackageDependencies =""@(_DependenciesDesignTime)""
                          ProjectReferences =""@(ProjectReference)""
                          AssemblyFullPath =""$(TargetPath)""
                          OutputType=""$(OutputType)""
                          Platform=""$(Platform)""
                          RootNameSpace =""$(RootNamespace)""
                          CompilationItems =""@(Compile)""
                          TargetDirectory=""$(TargetDir)""
                          EmbeddedItems=""@(EmbeddedResource)""
                          Configuration=""$(Configuration)""
                          ProjectFullPath=""$(MSBuildProjectFullPath)""
                          ProjectDepsFileName=""$(ProjectDepsFileName)""
                          ProjectRuntimeConfigFileName=""$(ProjectRuntimeConfigFileName)""/>

  </Target>
</Project>
";
    }
}
