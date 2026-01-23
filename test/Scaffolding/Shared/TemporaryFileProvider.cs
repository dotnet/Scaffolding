// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.VisualStudio.Web.CodeGeneration
{
    internal class TemporaryFileProvider : PhysicalFileProvider
    {
        private static readonly string TmpFilesRoot = Path.Combine(Path.GetTempPath(), "tmpfiles");

        static TemporaryFileProvider()
        {
            // Ensure the tmpfiles directory exists and has a NuGet.config.
            // 
            // This is required for CFSClean (Centralized Feed Service) policy compliance in CI pipelines.
            // The CFSClean policy restricts pipeline access to public NuGet endpoints (like nuget.org)
            // to prevent supply chain attacks. Tests that create temporary project files and run
            // dotnet restore/build will fail without a NuGet.config that points to approved Azure
            // DevOps artifact feeds instead of nuget.org.
            //
            // By copying the repo's NuGet.config to the tmpfiles parent directory, NuGet's config
            // file discovery (which walks up the directory tree) will find it and use the approved feeds.
            Directory.CreateDirectory(TmpFilesRoot);
            CopyNuGetConfigIfNeeded();
        }

        /// <summary>
        /// Copies the repository's NuGet.config to the temp files root directory if not already present.
        /// This ensures tests running in temp folders can restore packages using approved feeds
        /// rather than being blocked by CFSClean policy restrictions on public endpoints.
        /// </summary>
        private static void CopyNuGetConfigIfNeeded()
        {
            var targetNuGetConfig = Path.Combine(TmpFilesRoot, "NuGet.config");
            if (File.Exists(targetNuGetConfig))
            {
                return;
            }

            // Find the NuGet.config from the repository root by walking up from the assembly location
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            var directory = Path.GetDirectoryName(assemblyLocation);

            while (directory != null)
            {
                var nugetConfigPath = Path.Combine(directory, "NuGet.config");
                if (File.Exists(nugetConfigPath))
                {
                    File.Copy(nugetConfigPath, targetNuGetConfig);
                    return;
                }
                directory = Path.GetDirectoryName(directory);
            }
        }

        public TemporaryFileProvider()
            : base(Directory.CreateDirectory(Path.Combine(TmpFilesRoot, Guid.NewGuid().ToString())).FullName)
        {
        }

        public void Add(string filename, string contents)
        {
            File.WriteAllText(Path.Combine(this.Root, filename), contents, Encoding.UTF8);
        }

        public void Copy(string source, string destination)
        {
            File.Copy(source, Path.Combine(this.Root, destination));
        }

        public new void Dispose()
        {
            base.Dispose();
            Directory.Delete(Root, recursive: true);
        }
    }
}
