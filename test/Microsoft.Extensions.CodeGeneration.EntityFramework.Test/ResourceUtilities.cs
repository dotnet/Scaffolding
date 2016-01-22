using System.IO;
using System.Reflection;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.Extensions.CodeGeneration.EntityFramework.Test
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
            "Microsoft.Extensions.CodeGeneration.EntityFramework.Test");
    }
}
