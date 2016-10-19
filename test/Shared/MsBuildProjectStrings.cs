// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.Web.CodeGeneration
{
    internal class MsBuildProjectStrings
    {
        public const string NugetConfigTxt = @"
<configuration>
    <packageSources>
        <clear />
        <add key=""NuGet"" value=""https://api.nuget.org/v3/index.json"" />
        <add key=""dotnet-core"" value=""https://dotnet.myget.org/F/dotnet-core/api/v3/index.json"" />
        <add key=""dotnet-buildtools"" value=""https://dotnet.myget.org/F/dotnet-buildtools/api/v3/index.json"" />
        <add key=""nugetbuild"" value=""https://www.myget.org/F/nugetbuild/api/v3/index.json"" />
        <add key=""local"" value=""C:\git\scaffolding\artifacts\build\"" />
    </packageSources>
</configuration>";

        public const string RootProjectName = "Test.csproj";
        public const string RootProjectTxt = @"
<Project ToolsVersion=""14.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <Import Project=""$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props"" />

  <PropertyGroup>
    <RootNamespace>Microsoft.TestProject</RootNamespace>
    <ProjectName>TestProject</ProjectName>
    <OutputType>EXE</OutputType>
    <TargetFrameworks>netcoreapp1.0</TargetFrameworks>
    <OutputPath>bin\$(Configuration)</OutputPath>
    <PackageTargetFallback>portable-net45+win8</PackageTargetFallback>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include=""**\*.cs"" Exclude=""Excluded.cs"" />
  </ItemGroup>

  <ItemGroup>
     <PackageReference Include=""Microsoft.AspNetCore.Diagnostics"">
        <Version> 1.0.0</Version>
      </PackageReference>
      <PackageReference Include=""Microsoft.AspNetCore.Mvc"">
        <Version> 1.0.0</Version>
      </PackageReference>
      <PackageReference Include=""Microsoft.AspNetCore.Server.IISIntegration"">
        <Version> 1.0.0</Version>
      </PackageReference>
      <PackageReference Include=""Microsoft.AspNetCore.Server.Kestrel"">
        <Version> 1.0.0-*</Version>
      </PackageReference>
      <PackageReference Include=""Microsoft.AspNetCore.StaticFiles"">
        <Version> 1.0.0</Version>
      </PackageReference>
      <PackageReference Include=""Microsoft.Extensions.Configuration.EnvironmentVariables"">
        <Version> 1.0.0-*</Version>
      </PackageReference>
      <PackageReference Include=""Microsoft.Extensions.Configuration.Json"">
        <Version> 1.0.0</Version>
      </PackageReference>
      <PackageReference Include=""Microsoft.Extensions.Logging"">
        <Version> 1.0.0-*</Version>
      </PackageReference>
      <PackageReference Include=""Microsoft.Extensions.Logging.Console"">
        <Version> 1.0.0</Version>
      </PackageReference>
      <PackageReference Include=""Microsoft.Extensions.Logging.Debug"">
        <Version> 1.0.0</Version>
      </PackageReference>
      <PackageReference Include=""Microsoft.Extensions.Options.ConfigurationExtensions"">
        <Version> 1.0.0</Version>
      </PackageReference>
    <PackageReference Include=""Microsoft.VisualStudio.Web.CodeGeneration.Design"">
      <Version>1.0.0-*</Version>
    </PackageReference>
    <PackageReference Include=""Microsoft.NETCore.Sdk"">
      <Version>1.0.0-*</Version>
    </PackageReference>
    <PackageReference Include=""Microsoft.NETCore.App"">
      <Version>1.0.1</Version>
    </PackageReference>
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include=""..\Library1\Library1.csproj"" />
  </ItemGroup>
  <ItemGroup>
    <Reference Include = ""xyz.dll"" />
  </ItemGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
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
                .SetBasePath(env.ContentRootPath);
                //.AddJsonFile(""appsettings.json"", optional: true, reloadOnChange: true)
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

        public const string LibraryProjectName = "Library1.csproj";
        public const string LibraryProjectTxt = @"
<Project ToolsVersion=""14.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <Import Project=""$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props"" />
  <PropertyGroup>
    <RootNamespace>Microsoft.Library</RootNamespace>
    <ProjectName>Library1</ProjectName>
    <OutputType>Library</OutputType>
    <TargetFrameworks>netstandard1.6</TargetFrameworks>
    <OutputPath>bin\$(Configuration)</OutputPath>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include=""**\*.cs"" Exclude=""Excluded.cs"" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include=""Microsoft.NETCore.Sdk"">
      <Version>1.0.0-*</Version>
    </PackageReference>
    <PackageReference Include=""NETStandard.Library"">
      <Version>1.6.0-*</Version>
    </PackageReference>
  </ItemGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
</Project>
";
    }
}
