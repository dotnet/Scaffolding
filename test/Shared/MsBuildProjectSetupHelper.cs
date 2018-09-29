// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
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

        private static object _syncObj = new object();

        private static string _globalJsonText;
        private static string GlobalJsonText
        {
            get
            {
                if (string.IsNullOrEmpty(_globalJsonText))
                {
                    lock (_syncObj)
                    {
                        if (string.IsNullOrEmpty(_globalJsonText))
                        {
                            string globalJsonFilePath = null;
                            var current = new DirectoryInfo(AppContext.BaseDirectory);
                            while (current != null)
                            {
                                globalJsonFilePath = Path.Combine(current.FullName, "global.json");
                                if (File.Exists(globalJsonFilePath))
                                {
                                    break;
                                }
                                current = current.Parent;
                            }

                            _globalJsonText = File.ReadAllText(globalJsonFilePath);
                        }
                    }
                }

                return _globalJsonText;
            }
        }

        public void SetupProjects(TemporaryFileProvider fileProvider, ITestOutputHelper output, bool fullFramework = false)
        {
            Directory.CreateDirectory(Path.Combine(fileProvider.Root, "Root"));
            Directory.CreateDirectory(Path.Combine(fileProvider.Root, "Library1"));
            Directory.CreateDirectory(Path.Combine(fileProvider.Root, "toolAssets", "netcoreapp2.0"));
            fileProvider.Add("global.json", GlobalJsonText);

            var rootProjectTxt = fullFramework ? MsBuildProjectStrings.RootNet45ProjectTxt : MsBuildProjectStrings.RootProjectTxt;
            fileProvider.Add($"Root/{MsBuildProjectStrings.RootProjectName}", rootProjectTxt);
            fileProvider.Add($"Root/TestCodeGeneration.targets", MsBuildProjectStrings.TestCodeGenerationTargetFileText);

            // Copy Msbuild task dlls.
            var msbuildTaskDllPath = Path.Combine(Path.GetDirectoryName(typeof(MsBuildProjectSetupHelper).Assembly.Location), "Microsoft.VisualStudio.Web.CodeGeneration.Msbuild.dll");
            fileProvider.Copy(msbuildTaskDllPath, "toolAssets/netcoreapp2.0/Microsoft.VisualStudio.Web.CodeGeneration.Msbuild.dll");
            fileProvider.Add($"Root/Startup.cs", MsBuildProjectStrings.StartupTxt);
            fileProvider.Add($"Root/{MsBuildProjectStrings.ProgramFileName}", MsBuildProjectStrings.ProgramFileText);

            fileProvider.Add($"Library1/{MsBuildProjectStrings.LibraryProjectName}", MsBuildProjectStrings.LibraryProjectTxt);
            fileProvider.Add($"Library1/ModelWithMatchingShortName.cs", "namespace Library1.Models { public class ModelWithMatchingShortName { } }");
            fileProvider.Add($"Library1/Car.cs", MsBuildProjectStrings.CarTxt);
            fileProvider.Add($"Library1/Product.cs", MsBuildProjectStrings.ProductTxt);
            RestoreAndBuild(Path.Combine(fileProvider.Root, "Root"), output);
        }

        internal void SetupProjectsForIdentityScaffolder(TemporaryFileProvider fileProvider, ITestOutputHelper output)
        {
            Directory.CreateDirectory(Path.Combine(fileProvider.Root, "Root"));
            Directory.CreateDirectory(Path.Combine(fileProvider.Root, "Library1"));
            Directory.CreateDirectory(Path.Combine(fileProvider.Root, "toolAssets", "netcoreapp2.0"));

            fileProvider.Add("global.json", GlobalJsonText);
            fileProvider.Add($"Root/{MsBuildProjectStrings.RootProjectName}", MsBuildProjectStrings.RootProjectTxt);
            fileProvider.Add($"Root/TestCodeGeneration.targets", MsBuildProjectStrings.TestCodeGenerationTargetFileText);

            // Copy Msbuild task dlls.
            var msbuildTaskDllPath = Path.Combine(Path.GetDirectoryName(typeof(MsBuildProjectSetupHelper).Assembly.Location), "Microsoft.VisualStudio.Web.CodeGeneration.Msbuild.dll");
            fileProvider.Copy(msbuildTaskDllPath, "toolAssets/netcoreapp2.0/Microsoft.VisualStudio.Web.CodeGeneration.Msbuild.dll");
            fileProvider.Add($"Root/Startup.cs", MsBuildProjectStrings.StartupForIdentityTxt);
            fileProvider.Add($"Root/{MsBuildProjectStrings.ProgramFileName}", MsBuildProjectStrings.ProgramFileText);
            fileProvider.Add($"Root/{MsBuildProjectStrings.IdentityContextName}", MsBuildProjectStrings.IdentityContextText);
            fileProvider.Add($"Root/{MsBuildProjectStrings.IdentityUserName}", MsBuildProjectStrings.IdentityUserText);
            fileProvider.Add($"Library1/{MsBuildProjectStrings.LibraryProjectName}", MsBuildProjectStrings.LibraryProjectTxt);
            RestoreAndBuild(Path.Combine(fileProvider.Root, "Root"), output);
        }

        private void RestoreAndBuild(string path, ITestOutputHelper output)
        {
            var result = Command.CreateDotNet("restore",
                new string[] { })
                .WithEnvironmentVariable("DOTNET_SKIP_FIRST_TIME_EXPERIENCE", "true")
                .InWorkingDirectory(path)
                .OnErrorLine(l => output.WriteLine(l))
                .OnOutputLine(l => output.WriteLine(l))
                .Execute();

            if (result.ExitCode != 0)
            {
                throw new InvalidOperationException($"Restore failed with exit code: {result.ExitCode} :: Dotnet path: {DotNetMuxer.MuxerPathOrDefault()}");
            }

            result = Command.CreateDotNet("build", new string[] { "-c", Configuration })
                .WithEnvironmentVariable("DOTNET_SKIP_FIRST_TIME_EXPERIENCE", "true")
                .InWorkingDirectory(path)
                .OnErrorLine(l => output.WriteLine(l))
                .OnOutputLine(l => output.WriteLine(l))
                .Execute();

            if (result.ExitCode != 0)
            {
                throw new InvalidOperationException($"Build failed with exit code: {result.ExitCode}");
            }
        }

        public void SetupProjectsWithoutEF(TemporaryFileProvider fileProvider, ITestOutputHelper output)
        {
            Directory.CreateDirectory(Path.Combine(fileProvider.Root, "Root"));
            Directory.CreateDirectory(Path.Combine(fileProvider.Root, "Library1"));
            Directory.CreateDirectory(Path.Combine(fileProvider.Root, "toolAssets", "netcoreapp2.0"));

            fileProvider.Add("global.json", GlobalJsonText);
            fileProvider.Add($"Root/TestCodeGeneration.targets", MsBuildProjectStrings.TestCodeGenerationTargetFileText);

            // Copy Msbuild task dlls.
            var msbuildTaskDllPath = Path.Combine(Path.GetDirectoryName(typeof(MsBuildProjectSetupHelper).Assembly.Location), "Microsoft.VisualStudio.Web.CodeGeneration.Msbuild.dll");
            fileProvider.Copy(msbuildTaskDllPath, "toolAssets/netcoreapp2.0/Microsoft.VisualStudio.Web.CodeGeneration.Msbuild.dll");
            var rootProjectTxt = MsBuildProjectStrings.RootProjectTxtWithoutEF;
            fileProvider.Add($"Root/{MsBuildProjectStrings.RootProjectName}", rootProjectTxt);
            fileProvider.Add($"Root/Startup.cs", MsBuildProjectStrings.StartupTxtWithoutEf);
            fileProvider.Add($"Root/{MsBuildProjectStrings.ProgramFileName}", MsBuildProjectStrings.ProgramFileText);

            fileProvider.Add($"Library1/{MsBuildProjectStrings.LibraryProjectName}", MsBuildProjectStrings.LibraryProjectTxt);
            fileProvider.Add($"Library1/ModelWithMatchingShortName.cs", "namespace Library1.Models { public class ModelWithMatchingShortName { } }");
            fileProvider.Add($"Library1/Car.cs", MsBuildProjectStrings.CarTxt);
            fileProvider.Add($"Library1/Product.cs", MsBuildProjectStrings.ProductTxt);

            RestoreAndBuild(Path.Combine(fileProvider.Root, "Root"), output);
        }

        public void SetupProjectsWithDbContextInDependency(TemporaryFileProvider fileProvider, ITestOutputHelper output)
        {
            fileProvider.Add("global.json", GlobalJsonText);
            Directory.CreateDirectory(Path.Combine(fileProvider.Root, "Root"));
            Directory.CreateDirectory(Path.Combine(fileProvider.Root, "DAL"));
            Directory.CreateDirectory(Path.Combine(fileProvider.Root, "Library1"));
            Directory.CreateDirectory(Path.Combine(fileProvider.Root, "toolAssets", "netcoreapp2.0"));

            fileProvider.Add($"Root/TestCodeGeneration.targets", MsBuildProjectStrings.TestCodeGenerationTargetFileText);

            // Copy Msbuild task dlls.
            var msbuildTaskDllPath = Path.Combine(Path.GetDirectoryName(typeof(MsBuildProjectSetupHelper).Assembly.Location), "Microsoft.VisualStudio.Web.CodeGeneration.Msbuild.dll");
            fileProvider.Copy(msbuildTaskDllPath, "toolAssets/netcoreapp2.0/Microsoft.VisualStudio.Web.CodeGeneration.Msbuild.dll");

            var rootProjectTxt = MsBuildProjectStrings.WebProjectTxt;
            fileProvider.Add($"Root/{MsBuildProjectStrings.RootProjectName}", rootProjectTxt);
            fileProvider.Add($"Root/Startup.cs", MsBuildProjectStrings.StartupWithDbContext);
            fileProvider.Add($"Root/{MsBuildProjectStrings.ProgramFileName}", MsBuildProjectStrings.ProgramFileText);
            fileProvider.Add($"Root/{MsBuildProjectStrings.AppSettingsFileName}", MsBuildProjectStrings.AppSettingsFileTxt);

            fileProvider.Add($"Library1/{MsBuildProjectStrings.LibraryProjectName}", MsBuildProjectStrings.LibraryProjectTxt);
            fileProvider.Add($"Library1/ModelWithMatchingShortName.cs", "namespace Library1.Models { public class ModelWithMatchingShortName { } }");
            fileProvider.Add($"Library1/Car.cs", MsBuildProjectStrings.CarTxt);
            fileProvider.Add($"Library1/Product.cs", MsBuildProjectStrings.ProductTxt);

            fileProvider.Add($"DAL/{MsBuildProjectStrings.DALProjectName}", MsBuildProjectStrings.DAL);
            fileProvider.Add($"DAL/{MsBuildProjectStrings.CarContextFileName}", MsBuildProjectStrings.CarContextTxt);
            

            RestoreAndBuild(Path.Combine(fileProvider.Root, "Root"), output);
        }

        public void SetupProjectWithModelPropertyInParentDbContextClass(TemporaryFileProvider fileProvider, ITestOutputHelper output)
        {
            fileProvider.Add("global.json", GlobalJsonText);
            Directory.CreateDirectory(Path.Combine(fileProvider.Root, "Models"));
            Directory.CreateDirectory(Path.Combine(fileProvider.Root, "Data"));
            Directory.CreateDirectory(Path.Combine(fileProvider.Root, "Controllers"));
            Directory.CreateDirectory(Path.Combine(fileProvider.Root, "toolAssets", "netcoreapp2.0"));

            fileProvider.Add($"TestCodeGeneration.targets", MsBuildProjectStrings.DbContextInheritanceTestCodeGenerationTargetFileText);

            var msbuildTaskDllPath = Path.Combine(Path.GetDirectoryName(typeof(MsBuildProjectSetupHelper).Assembly.Location), "Microsoft.VisualStudio.Web.CodeGeneration.Msbuild.dll");
            fileProvider.Copy(msbuildTaskDllPath, "toolAssets/netcoreapp2.0/Microsoft.VisualStudio.Web.CodeGeneration.Msbuild.dll");

            var rootProjectTxt = MsBuildProjectStrings.DbContextInheritanceProjectTxt;
            fileProvider.Add(MsBuildProjectStrings.RootProjectName, rootProjectTxt);
            fileProvider.Add("Startup.cs", MsBuildProjectStrings.DerivedContextTestStartupText);
            fileProvider.Add(MsBuildProjectStrings.DbContextInheritanceProgramName, MsBuildProjectStrings.DbContextInheritanceProjectProgramText);
            fileProvider.Add(MsBuildProjectStrings.AppSettingsFileName, MsBuildProjectStrings.AppSettingsFileTxt);

            fileProvider.Add("Models/Blog.cs", MsBuildProjectStrings.BlogModelText);
            fileProvider.Add("Data/BaseDbContext.cs", MsBuildProjectStrings.BaseDbContextText);
            fileProvider.Add($"Data/{MsBuildProjectStrings.DerivedDbContextFileName}", MsBuildProjectStrings.DerivedDbContextText);

            RestoreAndBuild(fileProvider.Root, output);
        }
    }
}
