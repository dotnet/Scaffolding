// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.VisualStudio.Web.CodeGeneration
{
    internal class TemporaryFileProvider : PhysicalFileProvider
    {
        public TemporaryFileProvider()
            : base(Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "tmpfiles", Guid.NewGuid().ToString())).FullName)
        {
        }

        public void Add(string filename, string contents)
        {
            File.WriteAllText(Path.Combine(this.Root, filename), contents, Encoding.UTF8);
        }

        public void Copy(string source, string destination)
        {
            File.Copy(source, Path.Combine(this.Root, destination));
        }

        public new void Dispose()
        {
            base.Dispose();
            Directory.Delete(Root, recursive: true);
        }
    }
}
