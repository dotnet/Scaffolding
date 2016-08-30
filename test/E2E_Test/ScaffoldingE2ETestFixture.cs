using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace E2E_Test
{
    public class ScaffoldingE2ETestFixture : IDisposable
    {
        public ScaffoldingE2ETestFixture()
        {
            FilesToCleanUp = new List<string>();
            FoldersToCleanUp = new List<string>();
            BaseLineFilesDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Baseline");
            TestProjectDirectory = Path.GetFullPath(@"../TestApps/WebApplication1");
        }

        public List<string> FilesToCleanUp { get; private set; }
        public List<string> FoldersToCleanUp { get; private set; }
        public string BaseLineFilesDirectory { get; private set; }

        public string TestProjectDirectory { get; private set; }


        public void Dispose()
        {
            Console.WriteLine("Cleaning up generated files:");
            foreach(var file in FilesToCleanUp)
            {
                Console.WriteLine($"     {file}");
                File.Delete(Path.GetFullPath(file));
            }

            Console.WriteLine("Cleaning up generated folders");
            foreach(var folder in FoldersToCleanUp)
            {
                Console.WriteLine($"    {folder}");
                Directory.Delete(folder);
            }
        }
    }

    [CollectionDefinition("ScaffoldingE2ECollection")]
    public class ScaffoldingE2ECollection : ICollectionFixture<ScaffoldingE2ETestFixture>
    {

    }
}
