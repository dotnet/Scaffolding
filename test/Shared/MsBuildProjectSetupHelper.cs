// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.DotNet.Cli.Utils;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Web.CodeGeneration
{
    internal class MsBuildProjectSetupHelper
    {
        public void SetupProjects(TemporaryFileProvider fileProvider, ITestOutputHelper output, bool fullFramework = false)
        {
            string artifactsDir = null;
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current != null)
            {
                if (File.Exists(Path.Combine(current.FullName, "global.json")))
                {
                    artifactsDir = Path.Combine(current.FullName, "artifacts/build");
                    break;
                }
                current = current.Parent;
            }

            Directory.CreateDirectory(Path.Combine(fileProvider.Root, "Root"));
            Directory.CreateDirectory(Path.Combine(fileProvider.Root, "Library1"));
            fileProvider.Add("NuGet.config", MsBuildProjectStrings.GetNugetConfigTxt(artifactsDir));

            var rootProjectTxt = fullFramework ? MsBuildProjectStrings.RootNet45ProjectTxt : MsBuildProjectStrings.RootProjectTxt;
            fileProvider.Add($"Root/{MsBuildProjectStrings.RootProjectName}", rootProjectTxt);
            fileProvider.Add($"Root/Startup.cs", MsBuildProjectStrings.StartupTxt);
            fileProvider.Add($"Root/{MsBuildProjectStrings.ProgramFileName}", MsBuildProjectStrings.ProgramFileText);

            fileProvider.Add($"Library1/{MsBuildProjectStrings.LibraryProjectName}", MsBuildProjectStrings.LibraryProjectTxt);
            fileProvider.Add($"Library1/ModelWithMatchingShortName.cs", "namespace Library1.Models { public class ModelWithMatchingShortName { } }");
            fileProvider.Add($"Library1/Car.cs", MsBuildProjectStrings.CarTxt);
            fileProvider.Add($"Library1/Product.cs", MsBuildProjectStrings.ProductTxt);

            var result = Command.CreateDotNet("restore",
                new[] { Path.Combine(fileProvider.Root, "Root", "Test.csproj") })
                .OnErrorLine(l => output.WriteLine(l))
                .OnOutputLine(l => output.WriteLine(l))
                .Execute();
        }
    }
}