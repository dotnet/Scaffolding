using System.Collections.Generic;
using System.IO;

namespace Microsoft.DotNet.Scaffolding.Shared.ProjectModel
{
    internal static class ProjectModelHelper
    {
        public static string GetShortTfm(string tfmMoniker)
        {
            string shortTfm = string.Empty;
            if (!string.IsNullOrEmpty(tfmMoniker))
            {
                tfmMoniker = tfmMoniker.Replace(" ", "");
                //NuGet.Frameworks.NuGetFramework.GetShortFolderName() is giving invalid results on .NET tfms. Will use a dictionary for now
                ShortTfmDictionary.TryGetValue(tfmMoniker, out shortTfm);
            }
            return shortTfm;
        }

        public static string GetProjectAssetsFile(IProjectContext projectInformation)
        {
            string projectFolder = Path.GetDirectoryName(projectInformation.ProjectFullPath);
            if (!string.IsNullOrEmpty(projectFolder))
            {
                return Path.Combine(projectFolder, "obj", "project.assets.json");
            }
            return string.Empty;
        }

        private static Dictionary<string, string> ShortTfmDictionary = new Dictionary<string, string>()
        {
            { ".NETCoreApp,Version=v3.1", "netcoreapp3.1" },
            { ".NETCoreApp,Version=v5.0", "net5.0" },
            { ".NETCoreApp,Version=v6.0", "net6.0" },
            { ".NETCoreApp,Version=v2.1", "netcoreapp2.1" }
        };
    }
}
