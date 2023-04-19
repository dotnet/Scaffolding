// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.DotNet.Scaffolding.Shared.CodeModifier.CodeChange
{
    public class CodeFile
    {
        public Dictionary<string, Method> Methods { get; set; }
        public CodeSnippet[] Replacements { get; set; }
        public string AddFilePath { get; set; }
        public string[] Usings { get; set; }
        public CodeBlock[] UsingsWithOptions { get; set; }
        public string FileName { get; set; }
        public string Extension => FileName.Substring(FileName.LastIndexOf('.') + 1);
        public CodeBlock[] ClassProperties { get; set; }
        public CodeBlock[] ClassAttributes { get; set; }
        public string[] GlobalVariables { get; set; }
        public string[] Options { get; set; }
    }

    public class CodeBlock
    {
        public string Block { get; set; }
        public string[] Options { get; set; }
    }

    public class Formatting
    {
        public bool Newline { get; set; }
        public int NumberOfSpaces { get; set; }
        public bool Semicolon { get; set; }
    }
}
