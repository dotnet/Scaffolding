using Microsoft.DotNet.Scaffolding.Shared.ProjectModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.DotNet.Scaffolding.Shared.Tests
{
    public class ProjectModelHelperTests
    {
        [Theory]
        [InlineData(new object[] {
            new string[] { ".NETCoreApp,Version=v3.1", ".NETCoreApp,Version=v5.0", ".NETCoreApp,Version=v6.0", ".NETCoreApp, Version = v2.1" },
            new string[] { "netcoreapp3.1", "net5.0", "net6.0", "netcoreapp2.1" }
        })]
        public void GetShortTfmTests(string[] tfmMonikers, string[] shortTfms)
        {
            for (int i = 0; i < tfmMonikers.Length; i++)
            {
                string tfmMoniker = tfmMonikers[i];
                string shortTfm = ProjectModelHelper.GetShortTfm(tfmMoniker);
                Assert.Equal(shortTfm, shortTfms[i]);
            }
        }
    }
}
