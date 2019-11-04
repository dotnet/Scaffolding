// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO.Compression;
using System.Net;
using Xunit;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Core.FunctionalTest
{
    public class NupkgTest
    {
        [Fact]
        public void CheckFolderStructure()
        {
            string nuget300Package = "https://www.nuget.org/api/v2/package/Microsoft.VisualStudio.Web.CodeGeneration.Design/3.0.0";
            string fileName = "Microsoft.VisualStudio.Web.CodeGeneration.Design.3.0.0.nupkg";
            string buildNupkg = "../../../packages/Debug/Shipping/Debug/Microsoft.VisualStudio.Web.CodeGeneration.Design.3.1.0-dev.nupkg";
            using (WebClient myWebClient = new WebClient())
            {
                myWebClient.DownloadFile(nuget300Package, fileName);
            }

            ZipArchive zipOne, zipTwo;
            using(zipOne = ZipFile.OpenRead(fileName))
            {
                Assert.NotNull(zipOne);
            }

            using (zipTwo = ZipFile.OpenRead(buildNupkg))
            {
                Assert.NotNull(zipTwo);
            }
        }
    }
}
