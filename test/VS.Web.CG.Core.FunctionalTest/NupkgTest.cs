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
        private string tfm31 = "lib/netcoreapp3.1";

        [Fact]
        public void CheckFolderStructure()
        {

            string stableVersion = "3.0.0";
            string previousVersion = "3.1.0-preview2.19553.1";

            string localCodeGenPackage = "\\.nuget\\packages\\microsoft.visualstudio.web.codegeneration.design\\";
            string remoteCodeGenPackage = "\\.packages\\";

            string nugetPackageStable = localCodeGenPackage + string.Format("{0}\\microsoft.visualstudio.web.codegeneration.design.{0}.nupkg", stableVersion);
            string nugetPackagePrevious = localCodeGenPackage + string.Format("{0}\\microsoft.visualstudio.web.codegeneration.design.{0}.nupkg", previousVersion);

            string nugetPackageStablePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + nugetPackageStable;
            string nugetPackagePreviousPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + nugetPackagePrevious;

            string remotePackageStable = remoteCodeGenPackage + string.Format("{0}\\microsoft.visualstudio.web.codegeneration.design.{0}.nupkg", stableVersion);
            string remotePackagePrevious = remoteCodeGenPackage + string.Format("{0}\\microsoft.visualstudio.web.codegeneration.design.{0}.nupkg", previousVersion);

            ZipArchive zipStable, zipPrevious;
            Dictionary<string, string> artifactFiles = new Dictionary<string, string>();
            try 
            {
                zipPrevious = ZipFile.OpenRead(nugetPackagePreviousPath);
            }

            catch(DirectoryNotFoundException) 
            {
                zipPrevious = ZipFile.OpenRead(remotePackagePrevious);
            }
            

            using (zipPrevious)
            {
                foreach (ZipArchiveEntry entry in zipPrevious.Entries)
                {
                    //use full path name as key due to duplicate exes, xmls.
                    artifactFiles.Add(entry.FullName, entry.Name);
                }
            }
            
            try 
            {
                zipStable = ZipFile.OpenRead(nugetPackageStablePath);
            }
            catch(DirectoryNotFoundException)
            {
                zipStable = ZipFile.OpenRead(remotePackageStable);
            }
            using (zipStable)
            {
                foreach (ZipArchiveEntry entry in zipStable.Entries)
                {
                    //make sure new pkg has atleast all the old pkg files
                    if (artifactFiles.ContainsKey(entry.FullName))
                    {
                        artifactFiles.TryGetValue(entry.FullName, out string stableValue);
                        Assert.Equal(entry.Name, stableValue);
                    }
                    else
                    {
                        if (entry.FullName.Contains(tfm20))
                        {
                            string newKey = entry.FullName.Replace(tfm20, tfm31);
                            artifactFiles.TryGetValue(newKey, out string artifactsValue);
                            Assert.Equal(entry.Name, artifactsValue);
                        }
                    }
                }
            }
        }
    }
}

