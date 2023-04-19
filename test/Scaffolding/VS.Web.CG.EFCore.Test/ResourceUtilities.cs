// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Reflection;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore.Test
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

        private static EmbeddedFileProvider Instance = new EmbeddedFileProvider(typeof(ResourceUtilities).GetTypeInfo().Assembly, typeof(ResourceUtilities).Namespace);
    }
}
