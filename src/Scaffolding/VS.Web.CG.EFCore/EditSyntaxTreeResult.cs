// Copyright (c) .NET Foundation. All rights reserved.

using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore
{
    public class EditSyntaxTreeResult
    {
        public bool Edited { get; set; }

        public SyntaxTree OldTree { get; set; }

        public SyntaxTree NewTree { get; set; }
    }
}
