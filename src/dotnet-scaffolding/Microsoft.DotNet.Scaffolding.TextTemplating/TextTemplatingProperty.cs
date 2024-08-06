// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Microsoft.DotNet.Scaffolding.TextTemplating;

public class TextTemplatingProperty
{
    //Path to .tt (T4) template on disk (likely to be packed in a dotnet tool)
    public required string TemplatePath { get; set; }
    //the System.Type auto-generated object of the template (using TextTemplatingFilePreprocessor)
    public required Type TemplateType { get; set; }
    //output file path where the templated content will be written (should include the extension if one is wanted)
    public required string OutputPath { get; set; }
    //the 'name' property of <#@ parameter #> in provided .tt template
    public required string TemplateModelName { get; set; }
    //the 'type' property of <#@ parameter #> in provided .tt template
    public required object TemplateModel { get; set; }
}
