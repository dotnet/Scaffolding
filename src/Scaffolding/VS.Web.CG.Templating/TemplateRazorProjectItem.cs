﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;
using System.IO;
using System.Text;

namespace Microsoft.VisualStudio.Web.CodeGeneration.Templating
{
    internal class TemplateRazorProjectItem : RazorProjectItem
    {
        private readonly byte[] _contentUTF8Bytes;

        public TemplateRazorProjectItem(string content)
        {
            var preamble = Encoding.UTF8.GetPreamble();

            var contentBytes = Encoding.UTF8.GetBytes(content);

            _contentUTF8Bytes = new byte[preamble.Length + contentBytes.Length];
            preamble.CopyTo(_contentUTF8Bytes, 0);
            contentBytes.CopyTo(_contentUTF8Bytes, preamble.Length);
        }

        public override string BasePath => "/";

        public override string FilePath => "Template";

        public override string PhysicalPath => "/Template";

        public override bool Exists => true;

        public override Stream Read() => new MemoryStream(_contentUTF8Bytes);
    }
}
