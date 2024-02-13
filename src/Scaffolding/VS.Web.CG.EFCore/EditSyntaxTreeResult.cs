// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore
{
    public class EditSyntaxTreeResult
    {
        public bool NoEditsNeeded { get; set; } = false;
        public bool Edited { get; set; }

        public SyntaxTree OldTree { get; set; }

        public SyntaxTree NewTree { get; set; }
    }
}
