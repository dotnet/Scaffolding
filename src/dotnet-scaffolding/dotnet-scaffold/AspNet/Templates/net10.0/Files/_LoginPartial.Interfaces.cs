// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.DotNet.Scaffolding.TextTemplating;
using System.CodeDom.Compiler;

namespace Microsoft.DotNet.Tools.Scaffold.AspNet.Templates.net10.Files;

internal partial class _LoginPartial : ITextTransformation
{
    CompilerErrorCollection ITextTransformation.Errors => Errors;
}
