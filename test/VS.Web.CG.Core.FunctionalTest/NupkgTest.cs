// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using Xunit;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Core.FunctionalTest
{
    public class NupkgTest
    {
        private string tfm20 = "lib/netstandard2.0";
        //private string tfm31 = "lib/netcoreapp3.1";
        private string tfm30 = "lib/netcoreapp3.0";

        [Fact]
        public void CheckFolderStructure()
        {
            string stableNugetPackagePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\.nuget\\packages\\microsoft.visualstudio.web.codegeneration.design\\3.0.0\\microsoft.visualstudio.web.codegeneration.design.3.0.0.nupkg";
            string artifactsPath = "../../../../packages/Debug/Shipping/";
            DirectoryInfo taskDirectory = new DirectoryInfo(artifactsPath);
            string buildPkg = "Microsoft.VisualStudio.Web.CodeGeneration.Design*.nupkg";

            string fileToUse = "";

            foreach (var file in taskDirectory.GetFiles(buildPkg))
            {
                if (!file.Name.Contains("symbols"))
                {
                    fileToUse = artifactsPath + file.Name;
                }
            }

            Assert.False(string.IsNullOrEmpty(fileToUse));

            ZipArchive zipOne, zipTwo;
            Dictionary<string, string> artifactFiles = new Dictionary<string, string>();
            Dictionary<string, string> nuget301files = new Dictionary<string, string>();

            using (zipOne = ZipFile.OpenRead(fileToUse))
            {
                foreach (ZipArchiveEntry entry in zipOne.Entries)
                {
                    //use full path name as key due to duplicate exes, xmls.
                    artifactFiles.Add(entry.FullName, entry.Name);
                }
            }

            using (zipTwo = ZipFile.OpenRead(stableNugetPackagePath))
            {
                foreach (ZipArchiveEntry entry in zipTwo.Entries)
                {
                    //make sure new pkg has atleast all the old pkg files
                    if (artifactFiles.ContainsKey(entry.FullName))
                    {
                        artifactFiles.TryGetValue(entry.FullName, out string nuget300value);
                        Assert.Equal(entry.Name, nuget300value);
                    }
                    else
                    {
                        if(entry.FullName.Contains(tfm20))
                        {
                            string newKey = entry.FullName.Replace(tfm20, tfm30);
                            artifactFiles.TryGetValue(newKey, out string artifactsValue);
                            Assert.Equal(entry.Name, artifactsValue);
                        }

                    }
                }
            }
        }
    }
}

