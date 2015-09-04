using System.IO;
using System.Reflection;
using Microsoft.AspNet.FileProviders;

namespace Microsoft.Framework.CodeGeneration.EntityFramework.Test
{
    internal static class ResourceUtilities
    {
        public static string GetEmbeddedResourceFileContent(string relativeResourcePath)
        {
            using (var stream = Instance.GetFileInfo(relativeResourcePath).CreateReadStream())
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        private static EmbeddedFileProvider Instance = new EmbeddedFileProvider(Assembly.GetExecutingAssembly(),
            "Microsoft.Framework.CodeGeneration.EntityFramework.Test");
    }
}
