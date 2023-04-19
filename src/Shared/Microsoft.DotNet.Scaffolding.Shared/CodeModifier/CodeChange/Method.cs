// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.Scaffolding.Shared.CodeModifier.CodeChange
{
    public class Method
    {
        public string[] Parameters { get; set; }
        public CodeBlock[] AddParameters { get; set; }
        public CodeBlock EditType { get; set; }
        public CodeSnippet[] CodeChanges { get; set; }
        public CodeBlock[] Attributes { get; set; }
    }
}
