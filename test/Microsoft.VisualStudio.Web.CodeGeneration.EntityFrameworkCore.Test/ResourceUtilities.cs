// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
