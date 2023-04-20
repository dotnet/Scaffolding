// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Scaffolding.Shared.CodeModifier.CodeChange;

namespace Microsoft.DotNet.Scaffolding.Shared.CodeModifier
{
    public class CodeModifierConfig
    {
        public string Identifier { get; set; }
        public CodeFile[] Files { get; set; }
    }
}
