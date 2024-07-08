// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.CodeDom.Compiler;

namespace Microsoft.DotNet.Scaffolding.TextTemplating;

/// <summary>
///     This is currently an internal API that supports scaffolding. Use with caution.
/// </summary>
internal interface ITextTransformation
{
    /// <summary>
    /// Session object that can be used to transmit information into a template.
    /// </summary>
    IDictionary<string, object> Session { get; set; }

    /// <summary>
    /// Errors received after performing the text transformation.
    /// </summary>
    CompilerErrorCollection Errors { get; }

    void Initialize();

    string TransformText();
}
