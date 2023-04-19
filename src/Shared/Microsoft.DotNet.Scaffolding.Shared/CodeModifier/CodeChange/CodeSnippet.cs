// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.DotNet.Scaffolding.Shared.CodeModifier.CodeChange
{
    public class CodeSnippet
    {
        public string InsertAfter { get; set; }
        private string _block;
        public string Block
        {
            get
            {
                if (string.IsNullOrEmpty(_block))
                {
                    _block = string.Join(Environment.NewLine, MultiLineBlock);
                }

                return _block;
            }
            set { _block = value; }
        }

        public string CheckBlock { get; set; }
        public string Parent { get; set; }
        public bool Prepend { get; set; } = false;
        public bool Replace { get; set; } = false;
        public string[] InsertBefore { get; set; }
        public string[] Options { get; set; }
        public Formatting LeadingTrivia { get; set; } = new Formatting();
        public Formatting TrailingTrivia { get; set; } = new Formatting { Semicolon = true, Newline = true };
        public string Parameter { get; set; }
        public CodeChangeType CodeChangeType { get; set; } = CodeChangeType.Default;
        public string[] MultiLineBlock { get; set; }
        public string[] ReplaceSnippet { get; set; }
    }
}
