// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        internal void SetupEmptyCodeGenerationProject(TemporaryFileProvider fileProvider, ITestOutputHelper outputHelper)
        {
            fileProvider.Add("global.json", GlobalJsonText);
            Directory.CreateDirectory(Path.Combine(fileProvider.Root, "toolAssets", "net7.0"));

            fileProvider.Add($"TestCodeGeneration.targets", MsBuildProjectStrings.ProjectContextWriterMsbuildHelperText);

            var msbuildTaskDllPath = Path.Combine(Path.GetDirectoryName(typeof(MsBuildProjectSetupHelper).Assembly.Location), "Microsoft.VisualStudio.Web.CodeGeneration.Msbuild.dll");
            fileProvider.Copy(msbuildTaskDllPath, "toolAssets/net7.0/Microsoft.VisualStudio.Web.CodeGeneration.Msbuild.dll");

            var rootProjectTxt = MsBuildProjectStrings.SimpleNet70ProjectText2;
            fileProvider.Add(MsBuildProjectStrings.RootProjectName, rootProjectTxt);
            fileProvider.Add("Startup.cs", MsBuildProjectStrings.EmptyTestStartupText);
            fileProvider.Add(MsBuildProjectStrings.DbContextInheritanceProgramName, MsBuildProjectStrings.DbContextInheritanceProjectProgramText);
            fileProvider.Add(MsBuildProjectStrings.AppSettingsFileName, MsBuildProjectStrings.AppSettingsFileTxt);
            RestoreAndBuild(fileProvider.Root, outputHelper);
        }

        internal void SetupReferencedCodeGenerationProject(TemporaryFileProvider fileProvider, ITestOutputHelper outputHelper)
        {
            fileProvider.Add("global.json", GlobalJsonText);
            var projectPath = Path.Combine(fileProvider.Root, MsBuildProjectStrings.RootProjectFolder);
            var libraryPath = Path.Combine(fileProvider.Root, MsBuildProjectStrings.LibraryProjectFolder);
            Directory.CreateDirectory(projectPath);
            Directory.CreateDirectory(libraryPath);
            Directory.CreateDirectory(Path.Combine(projectPath, "toolAssets", "net7.0"));

            fileProvider.Add($"{projectPath}//TestCodeGeneration.targets", MsBuildProjectStrings.ProjectContextWriterMsbuildHelperText);

            var msbuildTaskDllPath = Path.Combine(Path.GetDirectoryName(typeof(MsBuildProjectSetupHelper).Assembly.Location), "Microsoft.VisualStudio.Web.CodeGeneration.Msbuild.dll");
            fileProvider.Copy(msbuildTaskDllPath, $"{projectPath}/toolAssets/net7.0/Microsoft.VisualStudio.Web.CodeGeneration.Msbuild.dll");
            fileProvider.Add($"{MsBuildProjectStrings.RootProjectFolder}/{MsBuildProjectStrings.RootProjectName}", MsBuildProjectStrings.Net7ReferencingProjectText);
            fileProvider.Add($"{MsBuildProjectStrings.RootProjectFolder}/Program.cs", MsBuildProjectStrings.MinimalProgramcsFile);
            fileProvider.Add($"{MsBuildProjectStrings.LibraryProjectFolder}/{MsBuildProjectStrings.Library2ProjectName}", MsBuildProjectStrings.Net7Library);
            fileProvider.Add($"{MsBuildProjectStrings.LibraryProjectFolder}/Blog.cs", MsBuildProjectStrings.BlogModelText);

            RestoreAndBuild(fileProvider.Root, outputHelper, $"{MsBuildProjectStrings.RootProjectFolder}/{MsBuildProjectStrings.RootProjectName}");
        }

        internal void SetupCodeGenerationProjectNullableDisabled(TemporaryFileProvider fileProvider, ITestOutputHelper outputHelper)
        {
            fileProvider.Add("global.json", GlobalJsonText);
            Directory.CreateDirectory(Path.Combine(fileProvider.Root, "toolAssets", "net7.0"));

            fileProvider.Add($"TestCodeGeneration.targets", MsBuildProjectStrings.ProjectContextWriterMsbuildHelperText);

            var msbuildTaskDllPath = Path.Combine(Path.GetDirectoryName(typeof(MsBuildProjectSetupHelper).Assembly.Location), "Microsoft.VisualStudio.Web.CodeGeneration.Msbuild.dll");
            fileProvider.Copy(msbuildTaskDllPath, "toolAssets/net7.0/Microsoft.VisualStudio.Web.CodeGeneration.Msbuild.dll");

            var rootProjectTxt = MsBuildProjectStrings.Net6NullableDisabled;
            fileProvider.Add(MsBuildProjectStrings.RootProjectName2, rootProjectTxt);
            fileProvider.Add("Startup.cs", MsBuildProjectStrings.EmptyTestStartupText);
            fileProvider.Add(MsBuildProjectStrings.DbContextInheritanceProgramName, MsBuildProjectStrings.DbContextInheritanceProjectProgramText);
            fileProvider.Add(MsBuildProjectStrings.AppSettingsFileName, MsBuildProjectStrings.AppSettingsFileTxt);

            RestoreAndBuild(fileProvider.Root, outputHelper);
        }

        internal void SetupCodeGenerationProjectNullableMissing(TemporaryFileProvider fileProvider, ITestOutputHelper outputHelper)
        {
            fileProvider.Add("global.json", GlobalJsonText);
            Directory.CreateDirectory(Path.Combine(fileProvider.Root, "toolAssets", "net7.0"));

            fileProvider.Add($"TestCodeGeneration.targets", MsBuildProjectStrings.ProjectContextWriterMsbuildHelperText);

            var msbuildTaskDllPath = Path.Combine(Path.GetDirectoryName(typeof(MsBuildProjectSetupHelper).Assembly.Location), "Microsoft.VisualStudio.Web.CodeGeneration.Msbuild.dll");
            fileProvider.Copy(msbuildTaskDllPath, "toolAssets/net7.0/Microsoft.VisualStudio.Web.CodeGeneration.Msbuild.dll");

            var rootProjectTxt = MsBuildProjectStrings.Net6NullableMissing;
            fileProvider.Add(MsBuildProjectStrings.RootProjectName3, rootProjectTxt);
            fileProvider.Add("Startup.cs", MsBuildProjectStrings.EmptyTestStartupText);
            fileProvider.Add(MsBuildProjectStrings.DbContextInheritanceProgramName, MsBuildProjectStrings.DbContextInheritanceProjectProgramText);
            fileProvider.Add(MsBuildProjectStrings.AppSettingsFileName, MsBuildProjectStrings.AppSettingsFileTxt);

            RestoreAndBuild(fileProvider.Root, outputHelper);
        }

        internal void SetupCodeGenerationProjectNullableEnabled(TemporaryFileProvider fileProvider, ITestOutputHelper outputHelper)
        {
            fileProvider.Add("global.json", GlobalJsonText);
            Directory.CreateDirectory(Path.Combine(fileProvider.Root, "toolAssets", "net7.0"));

            fileProvider.Add($"TestCodeGeneration.targets", MsBuildProjectStrings.ProjectContextWriterMsbuildHelperText);

            var msbuildTaskDllPath = Path.Combine(Path.GetDirectoryName(typeof(MsBuildProjectSetupHelper).Assembly.Location), "Microsoft.VisualStudio.Web.CodeGeneration.Msbuild.dll");
            fileProvider.Copy(msbuildTaskDllPath, "toolAssets/net7.0/Microsoft.VisualStudio.Web.CodeGeneration.Msbuild.dll");

            var rootProjectTxt = MsBuildProjectStrings.Net6NullableEnabled;
            fileProvider.Add(MsBuildProjectStrings.RootProjectName3, rootProjectTxt);
            fileProvider.Add("Startup.cs", MsBuildProjectStrings.EmptyTestStartupText);
            fileProvider.Add(MsBuildProjectStrings.DbContextInheritanceProgramName, MsBuildProjectStrings.DbContextInheritanceProjectProgramText);
            fileProvider.Add(MsBuildProjectStrings.AppSettingsFileName, MsBuildProjectStrings.AppSettingsFileTxt);

            RestoreAndBuild(fileProvider.Root, outputHelper);
        }

        public void SetupProjects(TemporaryFileProvider fileProvider, ITestOutputHelper output)
        {
            Directory.CreateDirectory(Path.Combine(fileProvider.Root, "Root"));
            Directory.CreateDirectory(Path.Combine(fileProvider.Root, "Library1"));
            Directory.CreateDirectory(Path.Combine(fileProvider.Root, "toolAssets", "netstandard2.0"));
            fileProvider.Add("global.json", GlobalJsonText);

            var rootProjectTxt = MsBuildProjectStrings.RootProjectTxt;
            fileProvider.Add($"Root/{MsBuildProjectStrings.RootProjectName}", rootProjectTxt);
            fileProvider.Add($"Root/TestCodeGeneration.targets", MsBuildProjectStrings.TestCodeGenerationTargetFileText);

            // Copy Msbuild task dlls.
            var msbuildTaskDllPath = Path.Combine(Path.GetDirectoryName(typeof(MsBuildProjectSetupHelper).Assembly.Location), "Microsoft.VisualStudio.Web.CodeGeneration.Msbuild.dll");
            fileProvider.Copy(msbuildTaskDllPath, "toolAssets/netstandard2.0/Microsoft.VisualStudio.Web.CodeGeneration.Msbuild.dll");
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
            Directory.CreateDirectory(Path.Combine(fileProvider.Root, "toolAssets", "netstandard2.0"));

            fileProvider.Add("global.json", GlobalJsonText);
            fileProvider.Add($"Root/{MsBuildProjectStrings.RootProjectName}", MsBuildProjectStrings.RootProjectTxt);
            fileProvider.Add($"Root/TestCodeGeneration.targets", MsBuildProjectStrings.TestCodeGenerationTargetFileText);

            // Copy Msbuild task dlls.
            var msbuildTaskDllPath = Path.Combine(Path.GetDirectoryName(typeof(MsBuildProjectSetupHelper).Assembly.Location), "Microsoft.VisualStudio.Web.CodeGeneration.Msbuild.dll");
            fileProvider.Copy(msbuildTaskDllPath, "toolAssets/netstandard2.0/Microsoft.VisualStudio.Web.CodeGeneration.Msbuild.dll");
            fileProvider.Add($"Root/Startup.cs", MsBuildProjectStrings.StartupForIdentityTxt);
            fileProvider.Add($"Root/{MsBuildProjectStrings.ProgramFileName}", MsBuildProjectStrings.ProgramFileText);
            fileProvider.Add($"Root/{MsBuildProjectStrings.IdentityContextName}", MsBuildProjectStrings.IdentityContextText);
            fileProvider.Add($"Root/{MsBuildProjectStrings.IdentityUserName}", MsBuildProjectStrings.IdentityUserText);
            fileProvider.Add($"Library1/{MsBuildProjectStrings.LibraryProjectName}", MsBuildProjectStrings.LibraryProjectTxt);
            RestoreAndBuild(Path.Combine(fileProvider.Root, "Root"), output);
        }

        internal void SetupProjectsForMinimalApiScaffolder(TemporaryFileProvider fileProvider, ITestOutputHelper output)
        {
            Directory.CreateDirectory(Path.Combine(fileProvider.Root, "toolAssets", "net6.0"));
            Directory.CreateDirectory(Path.Combine(fileProvider.Root, "Root"));
            fileProvider.Add("global.json", GlobalJsonText);
            var msbuildTaskDllPath = Path.Combine(Path.GetDirectoryName(typeof(MsBuildProjectSetupHelper).Assembly.Location), "Microsoft.VisualStudio.Web.CodeGeneration.Msbuild.dll");
            fileProvider.Copy(msbuildTaskDllPath, "toolAssets/net6.0/Microsoft.VisualStudio.Web.CodeGeneration.Msbuild.dll");
            fileProvider.Add($"Root/{MsBuildProjectStrings.RootProjectName}", MsBuildProjectStrings.SimpleNet70ProjectText);
            fileProvider.Add($"Root/TestCodeGeneration.targets", MsBuildProjectStrings.TestCodeGenerationTargetFileText);

            fileProvider.Add($"Root/{MsBuildProjectStrings.ProgramFileName}", MsBuildProjectStrings.MinimalProgramcsFile);
            fileProvider.Add($"Root/{MsBuildProjectStrings.IdentityContextName}", MsBuildProjectStrings.CarContextTxt);
            fileProvider.Add($"Root/Car.cs", MsBuildProjectStrings.CarTxt);
            RestoreAndBuild(Path.Combine(fileProvider.Root, "Root"), output);
        }

        private void RestoreAndBuild(string path, ITestOutputHelper output, string projectName = null)
        {
            projectName = projectName ?? string.Empty;
            var result = Command.CreateDotNet("restore",
                new string[] { projectName })
                .WithEnvironmentVariable("DOTNET_SKIP_FIRST_TIME_EXPERIENCE", "true")
                .InWorkingDirectory(path)
                .OnErrorLine(l => output.WriteLine(l))
                .OnOutputLine(l => output.WriteLine(l))
                .Execute();

            if (result.ExitCode != 0)
            {
                throw new InvalidOperationException($"Restore failed with exit code: {result.ExitCode} :: Dotnet path: {DotNetMuxer.MuxerPathOrDefault()}");
            }

            result = Command.CreateDotNet("build", new string[] { projectName, "-c", Configuration })
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
            Directory.CreateDirectory(Path.Combine(fileProvider.Root, "toolAssets", "netstandard2.0"));

            fileProvider.Add("global.json", GlobalJsonText);
            fileProvider.Add($"Root/TestCodeGeneration.targets", MsBuildProjectStrings.TestCodeGenerationTargetFileText);

            // Copy Msbuild task dlls.
            var msbuildTaskDllPath = Path.Combine(Path.GetDirectoryName(typeof(MsBuildProjectSetupHelper).Assembly.Location), "Microsoft.VisualStudio.Web.CodeGeneration.Msbuild.dll");
            fileProvider.Copy(msbuildTaskDllPath, "toolAssets/netstandard2.0/Microsoft.VisualStudio.Web.CodeGeneration.Msbuild.dll");
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
            Directory.CreateDirectory(Path.Combine(fileProvider.Root, "toolAssets", "netstandard2.0"));

            fileProvider.Add($"Root/TestCodeGeneration.targets", MsBuildProjectStrings.TestCodeGenerationTargetFileText);

            // Copy Msbuild task dlls.
            var msbuildTaskDllPath = Path.Combine(Path.GetDirectoryName(typeof(MsBuildProjectSetupHelper).Assembly.Location), "Microsoft.VisualStudio.Web.CodeGeneration.Msbuild.dll");
            fileProvider.Copy(msbuildTaskDllPath, "toolAssets/netstandard2.0/Microsoft.VisualStudio.Web.CodeGeneration.Msbuild.dll");

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
