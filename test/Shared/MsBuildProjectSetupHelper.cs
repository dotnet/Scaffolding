// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.DependencyModel;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Web.CodeGeneration
{
    internal class MsBuildProjectSetupHelper
    {
        #if RELEASE
        public const string Configuration = "Release";
        #else
        public const string Configuration = "Debug";
        #endif

        private string AspNetCoreVersion
        {
            get 
            {
                var aspnetCoreLib = DependencyContext
                    .Default
                    .CompileLibraries
                    .FirstOrDefault(l => l.Name.StartsWith("Microsoft.Extensions.FileProviders.Abstractions", StringComparison.OrdinalIgnoreCase));

                return aspnetCoreLib?.Version;
            }
        }

        private string CodeGenerationVersion
        {
            get 
            {
                var informationalVersionAttr = this.GetType().Assembly.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false).First();
                return ((AssemblyInformationalVersionAttribute)informationalVersionAttr).InformationalVersion;
            }
        }

        public void SetupProjects(TemporaryFileProvider fileProvider, ITestOutputHelper output, bool fullFramework = false)
        {
            string artifactsDir = null;
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current != null)
            {
                if (File.Exists(Path.Combine(current.FullName, "Scaffolding.sln")))
                {
                    artifactsDir = Path.Combine(current.FullName, "artifacts", "build");
                    break;
                }
                current = current.Parent;
            }

            Directory.CreateDirectory(Path.Combine(fileProvider.Root, "Root"));
            Directory.CreateDirectory(Path.Combine(fileProvider.Root, "Library1"));
            fileProvider.Add("NuGet.config", MsBuildProjectStrings.GetNugetConfigTxt(artifactsDir));

            var rootProjectTxt = fullFramework ? MsBuildProjectStrings.RootNet45ProjectTxt : MsBuildProjectStrings.RootProjectTxt;
            fileProvider.Add($"Root/{MsBuildProjectStrings.RootProjectName}", string.Format(rootProjectTxt, AspNetCoreVersion, CodeGenerationVersion));
            fileProvider.Add($"Root/Startup.cs", MsBuildProjectStrings.StartupTxt);
            fileProvider.Add($"Root/{MsBuildProjectStrings.ProgramFileName}", MsBuildProjectStrings.ProgramFileText);

            fileProvider.Add($"Library1/{MsBuildProjectStrings.LibraryProjectName}", MsBuildProjectStrings.LibraryProjectTxt);
            fileProvider.Add($"Library1/ModelWithMatchingShortName.cs", "namespace Library1.Models { public class ModelWithMatchingShortName { } }");
            fileProvider.Add($"Library1/Car.cs", MsBuildProjectStrings.CarTxt);
            fileProvider.Add($"Library1/Product.cs", MsBuildProjectStrings.ProductTxt);
            RestoreAndBuild(Path.Combine(fileProvider.Root, "Root"), output);
        }

        private void RestoreAndBuild(string path, ITestOutputHelper output)
        {

            var result = Command.CreateDotNet("restore",
                new string[] {})
                .WithEnvironmentVariable("DOTNET_SKIP_FIRST_TIME_EXPERIENCE", "true")
                .InWorkingDirectory(path)
                .OnErrorLine(l => output.WriteLine(l))
                .OnOutputLine(l => output.WriteLine(l))
                .Execute();

            if (result.ExitCode !=0)
            {
                throw new InvalidOperationException($"Restore failed with exit code: {result.ExitCode}");
            }

            result = Command.CreateDotNet("build", new string[] {"-c", Configuration})
                .WithEnvironmentVariable("DOTNET_SKIP_FIRST_TIME_EXPERIENCE", "true")
                .InWorkingDirectory(path)
                .OnErrorLine(l => output.WriteLine(l))
                .OnOutputLine(l => output.WriteLine(l))
                .Execute();

           if (result.ExitCode !=0)
            {
                throw new InvalidOperationException($"Build failed with exit code: {result.ExitCode}");
            }
        }

        public void SetupProjectsWithoutEF(TemporaryFileProvider fileProvider, ITestOutputHelper output)
        {
            string artifactsDir = null;
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current != null)
            {
                if (File.Exists(Path.Combine(current.FullName, "Scaffolding.sln")))
                {
                    artifactsDir = Path.Combine(current.FullName, "artifacts", "build");
                    break;
                }
                current = current.Parent;
            }

            Directory.CreateDirectory(Path.Combine(fileProvider.Root, "Root"));
            Directory.CreateDirectory(Path.Combine(fileProvider.Root, "Library1"));
            fileProvider.Add("NuGet.config", MsBuildProjectStrings.GetNugetConfigTxt(artifactsDir));

            var rootProjectTxt = MsBuildProjectStrings.RootProjectTxtWithoutEF;
            fileProvider.Add($"Root/{MsBuildProjectStrings.RootProjectName}", string.Format(rootProjectTxt, AspNetCoreVersion, CodeGenerationVersion));
            fileProvider.Add($"Root/Startup.cs", MsBuildProjectStrings.StartupTxtWithoutEf);
            fileProvider.Add($"Root/{MsBuildProjectStrings.ProgramFileName}", MsBuildProjectStrings.ProgramFileText);

            fileProvider.Add($"Library1/{MsBuildProjectStrings.LibraryProjectName}", MsBuildProjectStrings.LibraryProjectTxt);
            fileProvider.Add($"Library1/ModelWithMatchingShortName.cs", "namespace Library1.Models { public class ModelWithMatchingShortName { } }");
            fileProvider.Add($"Library1/Car.cs", MsBuildProjectStrings.CarTxt);
            fileProvider.Add($"Library1/Product.cs", MsBuildProjectStrings.ProductTxt);

            RestoreAndBuild(Path.Combine(fileProvider.Root, "Root"), output);
        }
    }
}