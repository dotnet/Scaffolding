// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;
using System.IO;
using System.Text;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Templating
{
    internal class TemplateRazorProjectItem : RazorProjectItem
    {
        private readonly byte[] _contentUTF8Bytes;
        private readonly FileInfo _path;

        public TemplateRazorProjectItem(string path, string content)
        {
            var preamble = Encoding.UTF8.GetPreamble();

            var contentBytes = Encoding.UTF8.GetBytes(content);

            this._contentUTF8Bytes = new byte[preamble.Length + contentBytes.Length];
            preamble.CopyTo(_contentUTF8Bytes, 0);
            contentBytes.CopyTo(_contentUTF8Bytes, preamble.Length);
            this._path = new FileInfo(path);
        }

        public override string BasePath => this._path.Directory.FullName;

        public override string FilePath => this._path.Name;

        public override string PhysicalPath => this._path.FullName;

        public override bool Exists => true;

        public override Stream Read() => new MemoryStream(_contentUTF8Bytes);
    }
}
