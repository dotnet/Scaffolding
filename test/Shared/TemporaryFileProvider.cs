// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.VisualStudio.Web.CodeGeneration
{
    internal class TemporaryFileProvider : PhysicalFileProvider
    {
        private static string solutionFileName = "Scaffolding.sln";
        private static string TestFolder = GetTestFolder();
        private static string GetTestFolder()
        {
            string currDir = Directory.GetCurrentDirectory();
            while (!Directory.EnumerateFiles(currDir, solutionFileName).Any())
            {
                currDir = Path.GetDirectoryName(currDir);
                if (string.IsNullOrEmpty(currDir))
                {
                    throw new InvalidOperationException("Could not find Scaffolding.sln");
                }
            }

            return Path.Combine(currDir, ".test");
        }
        public TemporaryFileProvider()
            : base(Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "tmpfiles", Guid.NewGuid().ToString())).FullName)
        {
        }

        public void Add(string filename, string contents)
        {
            File.WriteAllText(Path.Combine(this.Root, filename), contents, Encoding.UTF8);
        }

        public new void Dispose()
        {
            base.Dispose();
            Directory.Delete(Root, recursive: true);
        }
    }
}
