using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using Xunit;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Core.FunctionalTest
{
    public class CompareNugetPkg
    {
        string zipPath = "..\\..\\..\\..\\..\\Microsoft.VisualStudio.Web.CodeGeneration.Design.3.1.0.nupkg";
        // string zipOne = "";
        // string zipTwo = "";

        [Fact]
        public bool checkIt()
        {
            using (ZipArchive archive = ZipFile.OpenRead(zipPath))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    
                }
            }
                return true;
        }
    }
}
