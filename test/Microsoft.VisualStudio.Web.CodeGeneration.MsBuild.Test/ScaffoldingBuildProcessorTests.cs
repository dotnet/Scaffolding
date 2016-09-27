using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VisualStudio.Web.CodeGeneration.MsBuild.Test
{
    public class ScaffoldingBuildProcessorTests
    {
        [Fact]
        public void TestBuildProcessor()
        {
            string filePath = @"C:\Users\prbhosal\Documents\Visual Studio 15\Projects\WebApplication2\WebApplication2\WebApplication2.csproj";
            ScaffoldingBuildProcessor processor = new ScaffoldingBuildProcessor();

            MsBuilder<ScaffoldingBuildProcessor> builder = new MsBuilder<ScaffoldingBuildProcessor>(filePath, processor);
            builder.RunMsBuild();
            Assert.NotNull(processor.Packages);
        }
    }
}
