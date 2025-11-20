// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.Scaffolding.TextTemplating;

/// <summary>
/// Represents the properties required for T4 text templating operations.
/// </summary>
public class TextTemplatingProperty
{
    /// <summary>
    /// Path to the .tt (T4) template on disk (likely to be packed in a dotnet tool).
    /// </summary>
    public required string TemplatePath { get; set; }
    /// <summary>
    /// The System.Type auto-generated object of the template (using TextTemplatingFilePreprocessor).
    /// </summary>
    public required Type TemplateType { get; set; }
    /// <summary>
    /// Output file path where the templated content will be written (should include the extension if one is wanted).
    /// </summary>
    public required string OutputPath { get; set; }
    /// <summary>
    /// The 'name' property of <#@ parameter #> in provided .tt template.
    /// </summary>
    public required string TemplateModelName { get; set; }
    /// <summary>
    /// The 'type' property of <#@ parameter #> in provided .tt template.
    /// </summary>
    public required object TemplateModel { get; set; }
}
