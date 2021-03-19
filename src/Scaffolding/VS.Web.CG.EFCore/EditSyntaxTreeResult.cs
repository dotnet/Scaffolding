// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
