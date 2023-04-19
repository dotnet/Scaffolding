// Copyright (c) .NET Foundation. All rights reserved.

using System;
using System.IO;
using System.Text;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.Extensions.ProjectModel
{
    internal class TemporaryFileProvider : PhysicalFileProvider
    {
        public TemporaryFileProvider()
            :base(Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "tmpfiles", Guid.NewGuid().ToString())).FullName)
        {
        }

        public void Add(string filename, string contents)
        {
            File.WriteAllText(Path.Combine(this.Root, filename), contents, Encoding.UTF8);
        }

        public new void Dispose()
        {
            base.Dispose();
            Directory.Delete(Root, recursive: true);
        }
    }
}
